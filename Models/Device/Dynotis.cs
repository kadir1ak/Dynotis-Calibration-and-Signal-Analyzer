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

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Device
{
    public class Dynotis : BindableBase
    {
        public Dynotis()
        {
            Interface = new InterfaceData();
            thrust = new Thrust();
            torque = new Torque();
            current = new Current();
            voltage = new Voltage();
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
        private Voltage _voltage;
        public Voltage voltage
        {
            get => _voltage;
            set
            {
                if (_voltage != value)
                {
                    _voltage = value;
                    OnPropertyChanged();
                }
            }
        }

        private Thrust _thrust;
        public Thrust thrust
        {
            get => _thrust;
            set
            {
                if (_thrust != value)
                {
                    _thrust = value;
                    OnPropertyChanged();
                }
            }
        }

        private Torque _torque;
        public Torque torque
        {
            get => _torque;
            set
            {
                if (_torque != value)
                {
                    _torque = value;
                    OnPropertyChanged();
                }
            }
        }

        private Current _current;
        public Current current
        {
            get => _current;
            set
            {
                if (_current != value)
                {
                    _current = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _portReadData;
        public string portReadData
        {
            get => _portReadData;
            set => SetProperty(ref _portReadData, value);
        }
        private double _portReadTime;
        public double portReadTime
        {
            get => _portReadTime;
            set => SetProperty(ref _portReadTime, value);
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
                // Seri port başarıyla açıldıysa ara yüz verilerini güncelleme döngüsünü başlat
                StartUpdateInterfaceDataLoop();
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
                    portReadData = indata;
                    portReadTime = time;

                    thrust.Raw.ADC = itki;
                    torque.Raw.ADC = tork;
                    current.Raw.ADC = akım;
                    voltage.Raw.ADC = voltaj;
                });
            }
        }

        private CancellationTokenSource _updateLoopCancellationTokenSource;

        private int UpdateTimeMillisecond = 100; // 10 Hz (100ms)

        private readonly object _dataLock = new();
        private async Task UpdateInterfaceDataLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(UpdateTimeMillisecond, token);

                string latestData;
                lock (_dataLock)
                {
                    latestData = portReadData;
                }

                if (latestData != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Interface.PortReadData = portReadData;
                        Interface.PortReadTime = portReadTime;

                        Interface.Thrust.Raw.ADC = thrust.Raw.ADC;
                        Interface.Torque.Raw.ADC = torque.Raw.ADC;
                        Interface.Current.Raw.ADC = current.Raw.ADC;
                        Interface.Voltage.Raw.ADC = voltage.Raw.ADC;
                    });
                }
            }
        }
        public void StartUpdateInterfaceDataLoop()
        {
            StopUpdateInterfaceDataLoop(); // Eski döngüyü durdur
            _updateLoopCancellationTokenSource = new CancellationTokenSource();
            var token = _updateLoopCancellationTokenSource.Token;
            _ = UpdateInterfaceDataLoop(token);
        }
        public void StopUpdateInterfaceDataLoop()
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
