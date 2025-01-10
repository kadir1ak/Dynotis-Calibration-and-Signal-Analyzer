using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Interface;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Serial;
using Dynotis_Calibration_and_Signal_Analyzer.Services;
using static Dynotis_Calibration_and_Signal_Analyzer.Models.Device.DynotisData;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Device
{
    public class Dynotis : BindableBase
    {
        public Dynotis()
        {
            Interface = new InterfaceData();
            data = new DynotisData();
            serialPort = new SerialPort();          
        }

        private InterfaceData _interface;
        public InterfaceData Interface
        {
            get => _interface;
            set
            {
                if (_interface != value)
                {
                    _interface = value;
                    OnPropertyChanged();
                }
            }
        }
        private DynotisData _data;
        public DynotisData data
        {
            get => _data;
            set
            {
                if (_data != value)
                {
                    _data = value;
                    OnPropertyChanged();
                }
            }
        }

        private SerialPort _serialPort;
        public SerialPort serialPort
        {
            get => _serialPort;
            set
            {
                if (_serialPort != value)
                {
                    _serialPort = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task SerialPortConnect(string portName)
        {
            try
            {
                // Mevcut seri port açıksa kapat
                if (serialPort?.IsOpen == true)
                {
                    serialPort.DataReceived -= SerialPort_DataReceived;
                    serialPort.Close();
                }

                // Yeni SerialPort nesnesi oluştur ve ayarlarını yap
                serialPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = 921600,         // Baud hızı
                    DataBits = 8,              // Veri bitleri
                    Parity = Parity.None,      // Parite
                    StopBits = StopBits.One,   // Stop bit
                    Handshake = Handshake.None, // Donanım kontrolü yok
                    ReadTimeout = 1000,        // Okuma zaman aşımı (ms)
                    WriteTimeout = 1000        // Yazma zaman aşımı (ms)
                };

                // Veri alımı için olay işleyicisini bağla
                serialPort.DataReceived += SerialPort_DataReceived;
                // Seri portu aç
                serialPort.Open();
                // Seri port başarıyla açıldıysa güncelleme döngüsünü başlat
                StartUpdateDataLoop();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Access denied to the port: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Port error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to port: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string indata = serialPort.ReadExisting();

            if (string.IsNullOrEmpty(indata)) return;

            string[] dataParts = indata.Split(',');
            if (dataParts.Length == 5 &&
                double.TryParse(dataParts[0], out double time) &&
                int.TryParse(dataParts[1], out int itki) &&
                int.TryParse(dataParts[2], out int tork) &&
                int.TryParse(dataParts[3], out int akım) &&
                int.TryParse(dataParts[4], out int voltaj))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    data.PortRead.Data = indata;
                    data.PortRead.Time = time;

                    data.Thrust.Raw.ADC = itki;
                    data.Torque.Raw.ADC = tork;
                    data.Current.Raw.ADC = akım;
                    data.Voltage.Raw.ADC = voltaj;
                });
            }
        }

        private CancellationTokenSource _updateLoopCancellationTokenSource;

        private int UpdateTimeMillisecond = 10; // 100 Hz (10ms)

        private readonly object _dataLock = new();
        private async Task UpdateDataLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(UpdateTimeMillisecond, token);

                DynotisData latestData;
                lock (_dataLock)
                {
                    latestData = data;
                }

                if (latestData != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Interface.data.PortRead.Data = data.PortRead.Data;
                        Interface.data.PortRead.Time = data.PortRead.Time;

                        Interface.data.Thrust.Raw.ADC = data.Thrust.Raw.ADC;
                        Interface.data.Torque.Raw.ADC = data.Torque.Raw.ADC;
                        Interface.data.Current.Raw.ADC = data.Current.Raw.ADC;
                        Interface.data.Voltage.Raw.ADC = data.Voltage.Raw.ADC;
                    });
                }
            }
        }
        public void StartUpdateDataLoop()
        {
            StopUpdateDataLoop(); // Eski döngüyü durdur
            _updateLoopCancellationTokenSource = new CancellationTokenSource();
            var token = _updateLoopCancellationTokenSource.Token;
            _ = UpdateDataLoop(token);
        }
        public void StopUpdateDataLoop()
        {
            if (_updateLoopCancellationTokenSource != null && !_updateLoopCancellationTokenSource.IsCancellationRequested)
            {
                _updateLoopCancellationTokenSource.Cancel();
                _updateLoopCancellationTokenSource.Dispose();
                _updateLoopCancellationTokenSource = null;
            }
        }
    }
}
