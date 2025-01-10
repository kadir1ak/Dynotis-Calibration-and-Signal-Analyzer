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
using OxyPlot;
using OxyPlot.Series;
using System.Windows.Threading;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Interface;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Serial;
using Dynotis_Calibration_and_Signal_Analyzer.Services;
using OxyPlot.Legends;

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
            InitializePlotModel();
        }
        private void InitializePlotModel()
        {
            PlotModel = new PlotModel { Title = "Sensor Data Visualization" };

            PlotModel.Legends.Add(new OxyPlot.Legends.Legend()
            {
                LegendTitle = "Legend",
                LegendPosition = LegendPosition.LeftTop,
                LegendTextColor = OxyColors.Black
            });

            PlotModel.Series.Add(new LineSeries
            {
                Title = "Thrust",
                Color = OxyColors.Orange
            });
            PlotModel.Series.Add(new LineSeries
            {
                Title = "Torque",
                Color = OxyColors.DarkSeaGreen
            });
            PlotModel.Series.Add(new LineSeries
            {
                Title = "Current",
                Color = OxyColor.Parse("#FF3D67B9")
            });
            PlotModel.Series.Add(new LineSeries
            {
                Title = "Voltage",
                Color = OxyColors.Purple
            });
        }

        private PlotModel _plotModel;
        public PlotModel PlotModel
        {
            get => _plotModel;
            set
            {
                if (_plotModel != value)
                {
                    _plotModel = value;
                    OnPropertyChanged();
                }
            }
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
        private double _portReadTime = 0;
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
                // Grafik güncelleme döngüsünü başlat
                StartUpdatePlotDataLoop();
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
                    portReadTime += 0.001;

                    thrust.Raw.ADC = itki;
                    torque.Raw.ADC = tork;
                    current.Raw.ADC = akım;
                    voltage.Raw.ADC = voltaj;
                });
            }
        }

        private CancellationTokenSource _updateInterfaceDataLoopCancellationTokenSource;

        private int UpdateTimeMillisecond = 100; // 10 Hz (100ms)

        private readonly object _InterfaceDataLock = new();
        private async Task UpdateInterfaceDataLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(UpdateTimeMillisecond, token);

                string latestData;
                lock (_InterfaceDataLock)
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
            _updateInterfaceDataLoopCancellationTokenSource = new CancellationTokenSource();
            var token = _updateInterfaceDataLoopCancellationTokenSource.Token;
            _ = UpdateInterfaceDataLoop(token);
        }
        public void StopUpdateInterfaceDataLoop()
        {
            if (_updateInterfaceDataLoopCancellationTokenSource != null && !_updateInterfaceDataLoopCancellationTokenSource.IsCancellationRequested)
            {
                _updateInterfaceDataLoopCancellationTokenSource.Cancel();
                _updateInterfaceDataLoopCancellationTokenSource.Dispose();
                _updateInterfaceDataLoopCancellationTokenSource = null;
            }
        }

        private CancellationTokenSource _updatePlotDataLoopCancellationTokenSource;

        private int PlotUpdateTimeMillisecond = 10; // 100 Hz (10ms)

        private readonly object _PlotDataLock = new();
        private async Task UpdatePlotDataLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(PlotUpdateTimeMillisecond, token);

                // Son verileri al
                double latestTime, thrustValue, torqueValue, currentValue, voltageValue;

                lock (_PlotDataLock)
                {
                    latestTime = portReadTime;
                    thrustValue = thrust.Raw.ADC;
                    torqueValue = torque.Raw.ADC;
                    currentValue = current.Raw.ADC;
                    voltageValue = voltage.Raw.ADC;
                }

                // Grafiği güncelle
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (PlotModel.Series[0] is LineSeries thrustSeries)
                    {
                        thrustSeries.Points.Add(new DataPoint(latestTime, thrustValue));
                        if (thrustSeries.Points.Count > 100) thrustSeries.Points.RemoveAt(0); // Maksimum 100 nokta tut
                    }

                    if (PlotModel.Series[1] is LineSeries torqueSeries)
                    {
                        torqueSeries.Points.Add(new DataPoint(latestTime, torqueValue));
                        if (torqueSeries.Points.Count > 100) torqueSeries.Points.RemoveAt(0);
                    }

                    if (PlotModel.Series[2] is LineSeries currentSeries)
                    {
                        currentSeries.Points.Add(new DataPoint(latestTime, currentValue));
                        if (currentSeries.Points.Count > 100) currentSeries.Points.RemoveAt(0);
                    }

                    if (PlotModel.Series[3] is LineSeries voltageSeries)
                    {
                        voltageSeries.Points.Add(new DataPoint(latestTime, voltageValue));
                        if (voltageSeries.Points.Count > 100) voltageSeries.Points.RemoveAt(0);
                    }

                    PlotModel.InvalidatePlot(true); // Grafiği yeniden çiz
                });
            }
        }

        public void StartUpdatePlotDataLoop()
        {
            StopUpdatePlotDataLoop(); // Eski döngüyü durdur
            _updatePlotDataLoopCancellationTokenSource = new CancellationTokenSource();
            var token = _updatePlotDataLoopCancellationTokenSource.Token;
            _ = UpdatePlotDataLoop(token);
        }

        public void StopUpdatePlotDataLoop()
        {
            if (_updatePlotDataLoopCancellationTokenSource != null && !_updatePlotDataLoopCancellationTokenSource.IsCancellationRequested)
            {
                _updatePlotDataLoopCancellationTokenSource.Cancel();
                _updatePlotDataLoopCancellationTokenSource.Dispose();
                _updatePlotDataLoopCancellationTokenSource = null;
            }
        }

    }
}
