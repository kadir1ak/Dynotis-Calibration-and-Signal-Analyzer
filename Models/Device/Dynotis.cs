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
using MathNet.Numerics;
using System.Windows.Threading;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Interface;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Serial;
using Dynotis_Calibration_and_Signal_Analyzer.Services;
using OxyPlot.Legends;
using System.Windows.Input;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Device
{
    public class Dynotis : BindableBase
    {
        public Dynotis()
        {
            Interface = new InterfaceData();
            Interface.PropertyChanged += Interface_PropertyChanged;
            thrust = new Thrust();
            torque = new Torque();
            current = new Current();
            voltage = new Voltage();
            serialPort = new SerialPort();
            InitializePlotModel();
            InitializeInterface();
        }

        #region Taslak

        #endregion

        #region Serial Port

        private string _sampleCount;
        public string SampleCount
        {
            get => _sampleCount;
            set => SetProperty(ref _sampleCount, value);
        }

        private double sampleCount = 0;

        private DateTime lastUpdate = DateTime.Now;

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

            try
            {
                if (serialPort == null || !serialPort.IsOpen) return;

                string indata = serialPort.ReadExisting();
                if (string.IsNullOrEmpty(indata)) return;

                string[] dataParts = indata.Split(',');
                if (dataParts.Length == 5 &&
                    double.TryParse(dataParts[0].Replace('.', ','), out double time) &&
                    double.TryParse(dataParts[1].Replace('.', ','), out double itki) &&
                    double.TryParse(dataParts[2].Replace('.', ','), out double tork) &&
                    double.TryParse(dataParts[3].Replace('.', ','), out double akım) &&
                    double.TryParse(dataParts[4].Replace('.', ','), out double voltaj))
                {

                    if (Application.Current?.Dispatcher.CheckAccess() == true)
                    {
                        // UI iş parçacığında isek doğrudan çalıştır
                        UpdateSensorData(indata, itki, tork, akım, voltaj);
                        CalculateSampleRate();
                    }
                    else
                    {
                        // UI iş parçacığında değilsek Dispatcher kullan
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            UpdateSensorData(indata, itki, tork, akım, voltaj);
                            CalculateSampleRate();
                        }));
                    }
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"I/O Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopSerialPort();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateSensorData(string indata, double itki, double tork, double akım, double voltaj)
        {
            portReadData = indata;
            portReadTime += 0.001;

            thrust.Raw.ADC = itki;
            torque.Raw.ADC = tork;
            current.Raw.ADC = akım;
            voltage.Raw.ADC = voltaj;

            UpdateRawBuffer(thrust.Raw.RawBuffer, itki);
            UpdateRawBuffer(torque.Raw.RawBuffer, tork);
            UpdateRawBuffer(current.Raw.RawBuffer, akım);
            UpdateRawBuffer(voltage.Raw.RawBuffer, voltaj);

            thrust.Raw.Noise = CalculateNoise(thrust.Raw.RawBuffer);
            torque.Raw.Noise = CalculateNoise(torque.Raw.RawBuffer);
            current.Raw.Noise = CalculateNoise(current.Raw.RawBuffer);
            voltage.Raw.Noise = CalculateNoise(voltage.Raw.RawBuffer);
        }

        private void CalculateSampleRate()
        {
            sampleCount++;
            var now = DateTime.Now;
            var elapsed = now - lastUpdate;

            if (elapsed.TotalSeconds >= 1) // Her saniyede bir hesapla
            {
                SampleCount = $"Saniyedeki veri örnekleme hızı: {sampleCount} Hz";
                sampleCount = 0;
                lastUpdate = now;
            }
        }

        public void StopSerialPort()
        {
            if (serialPort?.IsOpen == true)
            {
                serialPort.Close();
                serialPort.DataReceived -= SerialPort_DataReceived;
            }
        }

        #endregion

        #region Sensors
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
        #endregion

        #region Calculates Statistic
        private void UpdateRawBuffer(List<double> rawBuffer, double newValue)
        {
            // Yeni değeri ekle
            rawBuffer.Add(newValue);

            // Buffer kapasitesini kontrol et, aşarsa eski veriyi kaldır
            if (rawBuffer.Count > 100)
            {
                rawBuffer.RemoveAt(0);
            }
        }

        private double CalculateNoise(List<double> rawBuffer)
        {
            if (rawBuffer.Count == 0) return 0;

            double average = rawBuffer.Average();
            double sumSquaredDiff = rawBuffer.Select(x => Math.Pow(x - average, 2)).Sum();
            return Math.Round(Math.Sqrt(sumSquaredDiff / rawBuffer.Count), 3);
        }
        #endregion

        #region Add Point
        public async Task AddPointAsync()
        {
            try
            {
                switch (Interface.Mode)
                {
                    case Mode.Thrust:
                        await AddThrustPointAsync();
                        break;
                    case Mode.Torque:
                        await AddTorquePointAsync();
                        break;
                    case Mode.Current:
                        await AddCurrentPointAsync();
                        break;
                    case Mode.Voltage:
                        await AddVoltagePointAsync();
                        break;
                    case Mode.LoadCellTest:
                        await AddLoadCellTestPointAsync();
                        break;
                    default:
                        MessageBox.Show("Geçerli bir mod seçiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddThrustPointAsync()
        {
            double appliedValue = thrust.Calibration.Applied;
            await CollectDataForSensor(thrust.Calibration, thrust.Raw, torque.Raw, appliedValue);
            Interface.UpdateThrustData(thrust);
        }

        private async Task AddTorquePointAsync()
        {
            double appliedValue = torque.Calibration.Applied;
            await CollectDataForSensor(torque.Calibration, torque.Raw, thrust.Raw, appliedValue);
            Interface.UpdateTorqueData(torque);
        }

        private async Task AddCurrentPointAsync()
        {
            double appliedValue = current.Calibration.Applied;
            await CollectDataForSensor(current.Calibration, current.Raw, null, appliedValue);
            Interface.UpdateCurrentData(current);
        }

        private async Task AddVoltagePointAsync()
        {
            double appliedValue = voltage.Calibration.Applied;
            await CollectDataForSensor(voltage.Calibration, voltage.Raw, null, appliedValue);
            Interface.UpdateVoltageData(voltage);
        }

        private async Task AddLoadCellTestPointAsync()
        {
            await CollectLoadCellData(thrust, torque);
            Interface.UpdateLoadCellData(thrust, torque);
        }
        private async Task CollectDataForSensor(CalibrationMeasurements calibration, RawMeasurements raw, RawMeasurements? crossRaw, double appliedValue)
        {
            try
            {
                calibration.Applied = appliedValue; // Uygulanan değeri ata
                List<double> adcValues = new List<double>();
                List<double> crossADCValues = new List<double>();

                int duration = 5000; // 5 saniye (5000 ms)
                int interval = 50; // 50 ms aralıklarla veri topla
                int elapsed = 0;

                // Progress ayarları
                Interface.Progress = 0; // İlerlemeyi başlat

                while (elapsed < duration)
                {
                    await Task.Delay(interval); // Bekle
                    adcValues.Add(raw.ADC); // Mevcut ADC değerini topla
                    if (crossRaw != null)
                    {
                        crossADCValues.Add(crossRaw.ADC); // Çapraz etkiyi topla (itki-tork bağımlılığı)
                    }

                    elapsed += interval;

                    // Progress güncellemesi
                    Interface.Progress = (elapsed / (double)duration) * 100.0; // Yüzde olarak ilerleme
                }

                // Ortalama ADC değerlerini hesapla
                double averageADC = adcValues.Any() ? adcValues.Average() : 0.0;
                double averageCrossADC = crossADCValues.Any() ? crossADCValues.Average() : 0.0;

                // Çapraz etkiyi hesapla ve kalibrasyon eğrisine ekle
                if (adcValues.Any())
                {
                    calibration.PointRawBuffer.Add(averageADC);
                    calibration.PointAppliedBuffer.Add(appliedValue);
                    if (crossRaw != null)
                    {
                        double crossEffect = averageCrossADC;
                        calibration.PointErrorBuffer.Add(crossEffect); // Çapraz hatayı kaydet
                    }
                }
                else
                {
                    MessageBox.Show("ADC verisi toplanamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // İşlem tamamlandığında progress %0 yap
                Interface.Progress = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load Cell Test sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }         
        }

        private async Task CollectLoadCellData(Thrust thrust, Torque torque)
        {
            try
            {
                List<double> thrustADCValues = new List<double>();
                List<double> torqueADCValues = new List<double>();

                double appliedThrust = thrust.Calibration.Applied;
                double appliedTorque = torque.Calibration.Applied;

                int duration = 5000; // 5 saniye (5000 ms)
                int interval = 50; // 50 ms aralıklarla veri topla
                int elapsed = 0;

                // Progress ayarları
                Interface.Progress = 0; // İlerlemeyi başlat

                while (elapsed < duration)
                {
                    await Task.Delay(interval);
                    thrustADCValues.Add(thrust.Raw.ADC);
                    torqueADCValues.Add(torque.Raw.ADC);
                    elapsed += interval;

                    // Progress güncellemesi
                    Interface.Progress = (elapsed / (double)duration) * 100.0; // Yüzde olarak ilerleme
                }

                // Ortalama değerler
                double averageThrustADC = thrustADCValues.Any() ? thrustADCValues.Average() : 0.0;
                double averageTorqueADC = torqueADCValues.Any() ? torqueADCValues.Average() : 0.0;

                // Load Cell Data'ya ekle
                thrust.Calibration.PointRawBuffer.Add(averageThrustADC);
                thrust.Calibration.PointAppliedBuffer.Add(appliedThrust);

                torque.Calibration.PointRawBuffer.Add(averageTorqueADC);
                torque.Calibration.PointAppliedBuffer.Add(appliedTorque);

                // İşlem tamamlandığında progress %0 yap
                Interface.Progress = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load Cell Test sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Calibration
        public async Task PerformCalibrationAsync()
        {
            try
            {
                switch (Interface.Mode)
                {
                    case Mode.Thrust:
                        await CalibrateSensorAsync(thrust.Calibration, "Thrust", 3);
                        break;
                    case Mode.Torque:
                        await CalibrateSensorAsync(torque.Calibration, "Torque", 3);
                        break;
                    case Mode.Current:
                        await CalibrateSensorAsync(current.Calibration, "Current", 2);
                        break;
                    case Mode.Voltage:
                        await CalibrateSensorAsync(voltage.Calibration, "Voltage", 2);
                        break;
                    default:
                        MessageBox.Show("Please select a valid mode (Thrust, Torque, Current, or Voltage).",
                                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during calibration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CalibrateSensorAsync(CalibrationMeasurements calibration, string modeName, int degree)
        {
            if (calibration == null)
                throw new ArgumentNullException(nameof(calibration), "Calibration data is null.");

            if (calibration.PointRawBuffer == null || calibration.PointAppliedBuffer == null)
                throw new InvalidOperationException("Calibration buffers are not initialized.");

            if (calibration.PointRawBuffer.Count < degree + 1 || calibration.PointAppliedBuffer.Count < degree + 1)
            {
                MessageBox.Show($"Insufficient calibration points for {modeName}. At least {degree + 1} points are required.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Polinom katsayılarını hesapla
                var coefficients = Fit.Polynomial(calibration.PointRawBuffer.ToArray(), calibration.PointAppliedBuffer.ToArray(), degree);

                // Hata katsayılarını hesapla (opsiyonel)
                var errorCoefficients = calibration.PointErrorBuffer?.Count > 0
                    ? Fit.Polynomial(calibration.PointRawBuffer.ToArray(), calibration.PointErrorBuffer.ToArray(), degree)
                    : new double[degree + 1];

                // Katsayıları kalibrasyona ekle
                calibration.Coefficient = new Coefficients
                {
                    A = coefficients.Length > 3 ? coefficients[3] : 0,
                    B = coefficients.Length > 2 ? coefficients[2] : 0,
                    C = coefficients.Length > 1 ? coefficients[1] : 0,
                    D = coefficients.Length > 0 ? coefficients[0] : 0,
                    ErrorA = errorCoefficients.Length > 3 ? errorCoefficients[3] : 0,
                    ErrorB = errorCoefficients.Length > 2 ? errorCoefficients[2] : 0,
                    ErrorC = errorCoefficients.Length > 1 ? errorCoefficients[1] : 0,
                    ErrorD = errorCoefficients.Length > 0 ? errorCoefficients[0] : 0
                };

                // Denklemleri oluştur
                string equation = GeneratePolynomialEquation(coefficients, "x");
                string errorEquation = GeneratePolynomialEquation(errorCoefficients, "e");

                calibration.Coefficient.Equation = equation; 
                calibration.Coefficient.ErrorEquation = errorEquation;

                // Kullanıcıya sonuçları göster
                MessageBox.Show($"{modeName} calibration completed successfully.\n\n" +
                                $"Equation:\n{equation}\n\n" +
                                $"Error Equation:\n{errorEquation}",
                                "Calibration Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during {modeName} calibration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GeneratePolynomialEquation(double[] coefficients, string variable)
        {
            var terms = coefficients
                .Select((c, i) => c != 0 ? $"{c:F6}*{variable}^{i}" : null)
                .Where(t => t != null)
                .Reverse()
                .ToList();
            return string.Join(" + ", terms);
        }
        #endregion

        #region Plot

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
        private void InitializePlotModel()
        {
            PlotModel = new PlotModel {};
            AddSeries("Thrust", OxyColors.Orange);
            AddSeries("Torque", OxyColors.DarkSeaGreen);
            AddSeries("Current", OxyColor.Parse("#FF3D67B9"));
            AddSeries("Voltage", OxyColors.Purple);

            PlotModel.Legends.Add(new OxyPlot.Legends.Legend
            {
                LegendTitle = "Legend",
                LegendPosition = LegendPosition.LeftTop,
                LegendTextColor = OxyColors.Black
            });

            UpdatePlotVisibility(Interface.Mode);
        }

        private void AddSeries(string title, OxyColor color)
        {
            PlotModel.Series.Add(new LineSeries
            {
                Title = title,
                Color = color
            });
        }
        private void UpdatePlotVisibility(Mode mode)
        {
            if (PlotModel == null || PlotModel.Series.Count == 0) return;

            foreach (var series in PlotModel.Series.OfType<LineSeries>())
            {
                series.IsVisible = false; // Varsayılan olarak tüm serileri gizle
            }

            switch (mode)
            {
                case Mode.Thrust:
                    PlotModel.Series[0].IsVisible = true; // Sadece Thrust serisini görünür yap
                    break;
                case Mode.Torque:
                    PlotModel.Series[1].IsVisible = true; // Sadece Torque serisini görünür yap
                    break;
                case Mode.LoadCellTest:
                    PlotModel.Series[0].IsVisible = true; // Thrust serisini görünür yap
                    PlotModel.Series[1].IsVisible = true; // Torque serisini görünür yap
                    break;
                case Mode.Current:
                    PlotModel.Series[2].IsVisible = true; // Sadece Current serisini görünür yap
                    break;
                case Mode.Voltage:
                    PlotModel.Series[3].IsVisible = true; // Sadece Voltage serisini görünür yap
                    break;
                default:
                    break;
            }

            PlotModel.InvalidatePlot(true); // Grafiği yeniden çiz
        }
        #endregion

        #region Update Interface Data

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

        private CancellationTokenSource _updateInterfaceDataLoopCancellationTokenSource;

        private int UpdateTimeMillisecond = 100; // 10 Hz (100ms)

        private readonly object _InterfaceDataLock = new();
        private async Task UpdateInterfaceDataLoop(CancellationToken token)
        {
            try
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
                            Interface.PortReadData = $"Port Message: {portReadData}";
                            Interface.PortReadTime = portReadTime;

                            Interface.Thrust.Raw.ADC = thrust.Raw.ADC;
                            Interface.Torque.Raw.ADC = torque.Raw.ADC;
                            Interface.Current.Raw.ADC = current.Raw.ADC;
                            Interface.Voltage.Raw.ADC = voltage.Raw.ADC;

                            Interface.Thrust.Raw.Noise = thrust.Raw.Noise;
                            Interface.Torque.Raw.Noise = torque.Raw.Noise;
                            Interface.Current.Raw.Noise = current.Raw.Noise;
                            Interface.Voltage.Raw.Noise = voltage.Raw.Noise;

                            Interface.Thrust.Calibration.Coefficient.Equation = "İtki (gF) = " + thrust.Calibration.Coefficient.Equation;
                            Interface.Torque.Calibration.Coefficient.Equation = "Tork (Nmm) = " + torque.Calibration.Coefficient.Equation;
                            Interface.Current.Calibration.Coefficient.Equation = "Akım (mA) = " + current.Calibration.Coefficient.Equation;
                            Interface.Voltage.Calibration.Coefficient.Equation = "Voltaj (mV) = " + voltage.Calibration.Coefficient.Equation;
                            Interface.Thrust.Calibration.Coefficient.ErrorEquation = "İtki Hatası (ADC) = " + thrust.Calibration.Coefficient.ErrorEquation;
                            Interface.Torque.Calibration.Coefficient.ErrorEquation = "Tork Hatası (ADC) = " + torque.Calibration.Coefficient.ErrorEquation;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Interface update loop error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private void Interface_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Interface.Mode))
            {
                UpdatePlotVisibility(Interface.Mode);
            }
        }
        public void InitializeInterface()
        {
            thrust.Calibration.Coefficient.Equation = "a₁x³ + a₂x² + a₃x + c";
            torque.Calibration.Coefficient.Equation = "b₁x³ + b₂x² + b₃x + d";
            current.Calibration.Coefficient.Equation = "c₁x³ + c₂x² + c₃x + d";
            voltage.Calibration.Coefficient.Equation = "d₁x³ + d₂x² + d₃x + e";
            thrust.Calibration.Coefficient.ErrorEquation = "a₁x³ + a₂x² + a₃x + c";
            torque.Calibration.Coefficient.ErrorEquation = "b₁x³ + b₂x² + b₃x + d";

            Interface.Thrust.Calibration.Coefficient.Equation = "İtki (gF) = " + thrust.Calibration.Coefficient.Equation;
            Interface.Torque.Calibration.Coefficient.Equation = "Tork (Nmm) = " + torque.Calibration.Coefficient.Equation;
            Interface.Current.Calibration.Coefficient.Equation = "Akım (mA) = " + current.Calibration.Coefficient.Equation;
            Interface.Voltage.Calibration.Coefficient.Equation = "Voltaj (mV) = " + voltage.Calibration.Coefficient.Equation;
            Interface.Thrust.Calibration.Coefficient.ErrorEquation = "İtki Hatası (ADC) = " + thrust.Calibration.Coefficient.ErrorEquation;
            Interface.Torque.Calibration.Coefficient.ErrorEquation = "Tork Hatası (ADC) = " + torque.Calibration.Coefficient.ErrorEquation;

        }
        #endregion

        #region Update Plot Data
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
        #endregion

    }
}
