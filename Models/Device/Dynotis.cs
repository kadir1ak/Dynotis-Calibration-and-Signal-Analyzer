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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.ObjectModel;

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
            loadCellTest = new LoadCellTest();
            current = new Current();
            voltage = new Voltage();

            serialPort = new SerialPort();

            InitializeInterface();
            InitializePlotModel();
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
                        UpdateSensorData(indata, time, itki, tork, akım, voltaj);
                        CalculateSampleRate();
                    }
                    else
                    {
                        // UI iş parçacığında değilsek Dispatcher kullan
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            UpdateSensorData(indata, time, itki, tork, akım, voltaj);
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
        private void UpdateSensorData(string indata, double time, double itki, double tork, double akım, double voltaj)
        {
            portReadData = indata;
            portReadTime = time;

            thrust.raw.Value = itki;
            torque.raw.Value = tork;
            current.raw.Value = akım;
            voltage.raw.Value = voltaj;

            UpdateRawBuffer(thrust.raw.Buffer, itki);
            UpdateRawBuffer(torque.raw.Buffer, tork);
            UpdateRawBuffer(current.raw.Buffer, akım);
            UpdateRawBuffer(voltage.raw.Buffer, voltaj);

            thrust.raw.NoiseValue = CalculateNoise(thrust.raw.Buffer);
            torque.raw.NoiseValue = CalculateNoise(torque.raw.Buffer);
            current.raw.NoiseValue = CalculateNoise(current.raw.Buffer);
            voltage.raw.NoiseValue = CalculateNoise(voltage.raw.Buffer);
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
        private LoadCellTest _loadCellTest;
        public LoadCellTest loadCellTest
        {
            get => _loadCellTest;
            set
            {
                if (_loadCellTest != value)
                {
                    _loadCellTest = value;
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

        #region Point Add
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
            double appliedValue = thrust.calibration.Applied;
            await CollectDataForThrust(thrust.calibration, thrust.raw, torque.raw, appliedValue);
            Interface.UpdateThrustDataGrid(thrust);
        }

        private async Task AddTorquePointAsync()
        {
            double appliedValue = torque.calibration.Applied;
            await CollectDataForTorque(torque.calibration, torque.raw, thrust.raw, appliedValue);
            Interface.UpdateTorqueDataGrid(torque);
        }

        private async Task AddCurrentPointAsync()
        {
            double appliedValue = current.calibration.Applied;
            await CollectDataForCurrent(current.calibration, current.raw, appliedValue);
            Interface.UpdateCurrentDataGrid(current);
        }

        private async Task AddVoltagePointAsync()
        {
            double appliedValue = voltage.calibration.Applied;
            await CollectDataForVoltage(voltage.calibration, voltage.raw, appliedValue);
            Interface.UpdateVoltageDataGrid(voltage);
        }

        private async Task AddLoadCellTestPointAsync()
        {
            await CollectLoadCellTestData(loadCellTest, thrust, torque);
            Interface.UpdateLoadCellTestDataGrid(loadCellTest);
        }

        private async Task CollectDataForThrust(Thrust.Calibration calibration, Thrust.Raw raw, Torque.Raw errorRaw, double appliedValue)
        {
            try
            {
                // Uygulanan değeri ata
                double AppliedThrust = appliedValue;
                if (calibration.AddingOn && calibration.PointAppliedBuffer.Count > 0)
                {
                    // Uygulanan değeri üzerine ekle
                    AppliedThrust += calibration.PointAppliedBuffer[calibration.PointAppliedBuffer.Count - 1];
                }

                List<double> Values = new List<double>();
                List<double> ErrorValues = new List<double>();

                int duration = 5000; // 5 saniye (5000 ms)
                int interval = 50; // 50 ms aralıklarla veri topla
                int elapsed = 0;

                // Progress ayarları
                Interface.Progress = 0; // İlerlemeyi başlat

                while (elapsed < duration)
                {
                    await Task.Delay(interval); // Bekle
                    Values.Add(raw.Value); // Mevcut ADC değerini topla
                    ErrorValues.Add(errorRaw.Value); // Çapraz etkiyi topla (itki-tork bağımlılığı)

                    elapsed += interval;

                    // Progress güncellemesi
                    Interface.Progress = (elapsed / (double)duration) * 100.0; // Yüzde olarak ilerleme
                }

                // Ortalama ADC değerlerini hesapla
                double AverageValue = Values.Any() ? Values.Average() : 0.0;
                double AverageErrorValue = ErrorValues.Any() ? ErrorValues.Average() : 0.0;

                // Değerleri kalibrasyon eğrisine ekle
                if (Values.Any())
                {
                    calibration.PointRawBuffer.Add(Math.Round(AverageValue, 3));
                    calibration.PointAppliedBuffer.Add(Math.Round(AppliedThrust, 3));
                    calibration.PointErrorBuffer.Add(Math.Round(AverageErrorValue, 3));
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
        private async Task CollectDataForTorque(Torque.Calibration calibration, Torque.Raw raw, Thrust.Raw errorRaw, double appliedValue)
        {
            try
            {
                // Uygulanan değeri ata
                double AppliedTorque = appliedValue;
                if (calibration.AddingOn && calibration.PointAppliedBuffer.Count > 0)
                {
                    // Uygulanan değeri üzerine ekle
                    AppliedTorque += calibration.PointAppliedBuffer[calibration.PointAppliedBuffer.Count - 1];
                }

                List<double> Values = new List<double>();
                List<double> ErrorValues = new List<double>();

                int duration = 5000; // 5 saniye (5000 ms)
                int interval = 50; // 50 ms aralıklarla veri topla
                int elapsed = 0;

                // Progress ayarları
                Interface.Progress = 0; // İlerlemeyi başlat

                while (elapsed < duration)
                {
                    await Task.Delay(interval); // Bekle
                    Values.Add(raw.Value); // Mevcut ADC değerini topla
                    ErrorValues.Add(errorRaw.Value); // Çapraz etkiyi topla (itki-tork bağımlılığı)

                    elapsed += interval;

                    // Progress güncellemesi
                    Interface.Progress = (elapsed / (double)duration) * 100.0; // Yüzde olarak ilerleme
                }

                // Ortalama ADC değerlerini hesapla
                double AverageValue = Values.Any() ? Values.Average() : 0.0;
                double AverageErrorValue = ErrorValues.Any() ? ErrorValues.Average() : 0.0;

                // Değerleri kalibrasyon eğrisine ekle
                if (Values.Any())
                {
                    calibration.PointRawBuffer.Add(Math.Round(AverageValue, 3));
                    calibration.PointAppliedBuffer.Add(Math.Round(AppliedTorque, 3));
                    calibration.PointErrorBuffer.Add(Math.Round(AverageErrorValue, 3));
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
        private async Task CollectDataForCurrent(Current.Calibration calibration, Current.Raw raw, double appliedValue)
        {
            try
            {
                calibration.Applied = appliedValue; // Uygulanan değeri ata
                List<double> Values = new List<double>();
                List<double> ErrorValues = new List<double>();

                int duration = 5000; // 5 saniye (5000 ms)
                int interval = 50; // 50 ms aralıklarla veri topla
                int elapsed = 0;

                // Progress ayarları
                Interface.Progress = 0; // İlerlemeyi başlat

                while (elapsed < duration)
                {
                    await Task.Delay(interval); // Bekle
                    Values.Add(raw.Value); // Mevcut ADC değerini topla
                    elapsed += interval;

                    // Progress güncellemesi
                    Interface.Progress = (elapsed / (double)duration) * 100.0; // Yüzde olarak ilerleme
                }

                // Ortalama ADC değerlerini hesapla
                double AverageValue = Values.Any() ? Values.Average() : 0.0;
                double AverageErrorValue = ErrorValues.Any() ? ErrorValues.Average() : 0.0;

                // Değerleri kalibrasyon eğrisine ekle
                if (Values.Any())
                {
                    calibration.PointRawBuffer.Add(Math.Round(AverageValue, 3));
                    calibration.PointAppliedBuffer.Add(Math.Round(calibration.Applied, 3));
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
        private async Task CollectDataForVoltage(Voltage.Calibration calibration, Voltage.Raw raw, double appliedValue)
        {
            try
            {
                calibration.Applied = appliedValue; // Uygulanan değeri ata
                List<double> Values = new List<double>();
                List<double> ErrorValues = new List<double>();

                int duration = 5000; // 5 saniye (5000 ms)
                int interval = 50; // 50 ms aralıklarla veri topla
                int elapsed = 0;

                // Progress ayarları
                Interface.Progress = 0; // İlerlemeyi başlat

                while (elapsed < duration)
                {
                    await Task.Delay(interval); // Bekle
                    Values.Add(raw.Value); // Mevcut ADC değerini topla
                    elapsed += interval;

                    // Progress güncellemesi
                    Interface.Progress = (elapsed / (double)duration) * 100.0; // Yüzde olarak ilerleme
                }

                // Ortalama ADC değerlerini hesapla
                double AverageValue = Values.Any() ? Values.Average() : 0.0;
                double AverageErrorValue = ErrorValues.Any() ? ErrorValues.Average() : 0.0;

                // Değerleri kalibrasyon eğrisine ekle
                if (Values.Any())
                {
                    calibration.PointRawBuffer.Add(Math.Round(AverageValue, 3));
                    calibration.PointAppliedBuffer.Add(Math.Round(calibration.Applied, 3));
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
        private async Task CollectLoadCellTestData(LoadCellTest loadCellTest, Thrust thrust, Torque torque)
        {
            try
            {
                List<double> calculatedThrustValues = new List<double>();
                List<double> calculatedTorqueValues = new List<double>();

                int duration = 5000; // 5 saniye (5000 ms)
                int interval = 50; // 50 ms aralıklarla veri topla
                int elapsed = 0;

                // Progress ayarları
                Interface.Progress = 0; // İlerlemeyi başlat

                while (elapsed < duration)
                {
                    await Task.Delay(interval);
                    calculatedThrustValues.Add(thrust.calculated.Value);
                    calculatedTorqueValues.Add(torque.calculated.Value);
                    elapsed += interval;

                    // Progress güncellemesi
                    Interface.Progress = (elapsed / (double)duration) * 100.0; // Yüzde olarak ilerleme
                }

                double AppliedThrust = thrust.calibration.Applied;
                double AppliedTorque = torque.calibration.Applied;

                double ThrustCapacity = thrust.calibration.Capacity;
                double TorqueCapacity = torque.calibration.Capacity;

                // Ortalama değerler
                double CalculatedThrustValue = calculatedThrustValues.Any() ? calculatedThrustValues.Average() : 0.0;
                double CalculatedTorqueValue = calculatedTorqueValues.Any() ? calculatedTorqueValues.Average() : 0.0;

                // Thrust için yüzde hata hesaplamaları
                double ThrustErrorValue = ((CalculatedThrustValue - AppliedThrust) / AppliedThrust) * 100;

                // Thrust için FS hata hesaplamaları
                double ThrustFSErrorValue = ((CalculatedThrustValue - AppliedThrust) / ThrustCapacity) * 100;

                // Torque için yüzde hata hesaplamaları
                double TorqueErrorValue = ((CalculatedTorqueValue - AppliedTorque) / AppliedTorque) * 100;

                // Torque için FS hata hesaplamaları
                double TorqueFSErrorValue = ((CalculatedTorqueValue - AppliedTorque) / TorqueCapacity) * 100;


                // Load Cell Data'ya ekle
                loadCellTest.Thrust.Buffer.Add(Math.Round(CalculatedThrustValue, 3));
                loadCellTest.Thrust.AppliedBuffer.Add(Math.Round(AppliedThrust, 3));
                loadCellTest.Thrust.ErrorBuffer.Add(Math.Round(ThrustErrorValue, 3));
                loadCellTest.Thrust.FSErrorBuffer.Add(Math.Round(ThrustFSErrorValue, 3));
                loadCellTest.Torque.Buffer.Add(Math.Round(CalculatedTorqueValue, 3));
                loadCellTest.Torque.AppliedBuffer.Add(Math.Round(AppliedTorque, 3));     
                loadCellTest.Torque.ErrorBuffer.Add(Math.Round(TorqueErrorValue, 3));
                loadCellTest.Torque.FSErrorBuffer.Add(Math.Round(TorqueFSErrorValue, 3));

                // İşlem tamamlandığında progress %0 yap
                Interface.Progress = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load Cell Test sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Point Delete
        public async Task DeletePointAsync()
        {
            try
            {
                switch (Interface.Mode)
                {
                    case Mode.Thrust:
                        DeleteThrustPoint();
                        break;

                    case Mode.Torque:
                        DeleteTorquePoint();
                        break;

                    case Mode.Current:
                        DeleteCurrentPoint();
                        break;

                    case Mode.Voltage:
                        DeleteVoltagePoint();
                        break;

                    case Mode.LoadCellTest:
                        DeleteLoadCellTestPoint();
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
        private void DeleteThrustPoint()
        {
            DeletePointFromThrustData(thrust.calibration);
            Interface.UpdateThrustDataGrid(thrust);
        }

        private void DeleteTorquePoint()
        {
            DeletePointFromTorqueData(torque.calibration);
            Interface.UpdateTorqueDataGrid(torque);
        }

        private void DeleteCurrentPoint()
        {
            DeletePointFromCurrentData(current.calibration);
            Interface.UpdateCurrentDataGrid(current);
        }

        private void DeleteVoltagePoint()
        {
            DeletePointFromVoltageData(voltage.calibration);
            Interface.UpdateVoltageDataGrid(voltage);
        }
        private void DeleteLoadCellTestPoint()
        {
            DeletePointFromLoadCellTestData(loadCellTest);
            Interface.UpdateVoltageDataGrid(voltage);
        }
        private void DeletePointFromThrustData(Thrust.Calibration calibration)
        {
            if (calibration.PointAppliedBuffer.Count > 0)
            {
                calibration.PointAppliedBuffer.RemoveAt(calibration.PointAppliedBuffer.Count - 1);
            }

            if (calibration.PointRawBuffer.Count > 0)
            {
                calibration.PointRawBuffer.RemoveAt(calibration.PointRawBuffer.Count - 1);
            }

            if (calibration.PointErrorBuffer != null && calibration.PointErrorBuffer.Count > 0)
            {
                calibration.PointErrorBuffer.RemoveAt(calibration.PointErrorBuffer.Count - 1);
            }
        }
        private void DeletePointFromTorqueData(Torque.Calibration calibration)
        {
            if (calibration.PointAppliedBuffer.Count > 0)
            {
                calibration.PointAppliedBuffer.RemoveAt(calibration.PointAppliedBuffer.Count - 1);
            }

            if (calibration.PointRawBuffer.Count > 0)
            {
                calibration.PointRawBuffer.RemoveAt(calibration.PointRawBuffer.Count - 1);
            }

            if (calibration.PointErrorBuffer != null && calibration.PointErrorBuffer.Count > 0)
            {
                calibration.PointErrorBuffer.RemoveAt(calibration.PointErrorBuffer.Count - 1);
            }
        }
        private void DeletePointFromCurrentData(Current.Calibration calibration)
        {
            if (calibration.PointAppliedBuffer.Count > 0)
            {
                calibration.PointAppliedBuffer.RemoveAt(calibration.PointAppliedBuffer.Count - 1);
            }

            if (calibration.PointRawBuffer.Count > 0)
            {
                calibration.PointRawBuffer.RemoveAt(calibration.PointRawBuffer.Count - 1);
            }
        }
        private void DeletePointFromVoltageData(Voltage.Calibration calibration)
        {
            if (calibration.PointAppliedBuffer.Count > 0)
            {
                calibration.PointAppliedBuffer.RemoveAt(calibration.PointAppliedBuffer.Count - 1);
            }

            if (calibration.PointRawBuffer.Count > 0)
            {
                calibration.PointRawBuffer.RemoveAt(calibration.PointRawBuffer.Count - 1);
            }
        }
        private void DeletePointFromLoadCellTestData(LoadCellTest loadCellTest)
        {
            if (loadCellTest.Thrust.AppliedBuffer.Count > 0)
            {
                loadCellTest.Thrust.Buffer.RemoveAt(loadCellTest.Thrust.Buffer.Count - 1);
                loadCellTest.Thrust.AppliedBuffer.RemoveAt(loadCellTest.Thrust.AppliedBuffer.Count - 1);
                loadCellTest.Thrust.ErrorBuffer.RemoveAt(loadCellTest.Thrust.ErrorBuffer.Count - 1);
                loadCellTest.Thrust.FSErrorBuffer.RemoveAt(loadCellTest.Thrust.FSErrorBuffer.Count - 1);
            }

            if (loadCellTest.Torque.AppliedBuffer.Count > 0)
            {
                loadCellTest.Torque.Buffer.RemoveAt(loadCellTest.Torque.Buffer.Count - 1);
                loadCellTest.Torque.AppliedBuffer.RemoveAt(loadCellTest.Torque.AppliedBuffer.Count - 1);
                loadCellTest.Torque.ErrorBuffer.RemoveAt(loadCellTest.Torque.ErrorBuffer.Count - 1);
                loadCellTest.Torque.FSErrorBuffer.RemoveAt(loadCellTest.Torque.FSErrorBuffer.Count - 1);
            }
        }
        #endregion

        #region All Points Delete
        public async Task DeleteAllPointsAsync()
        {
            try
            {
                switch (Interface.Mode)
                {
                    case Mode.Thrust:
                        DeleteAllPointsFromThrustData(thrust.calibration);
                        Interface.UpdateThrustDataGrid(thrust);
                        break;

                    case Mode.Torque:
                        DeleteAllPointsFromTorqueData(torque.calibration);
                        Interface.UpdateTorqueDataGrid(torque);
                        break;

                    case Mode.Current:
                        DeleteAllPointsFromCurrentData(current.calibration);
                        Interface.UpdateCurrentDataGrid(current);
                        break;

                    case Mode.Voltage:
                        DeleteAllPointsFromVoltageData(voltage.calibration);
                        Interface.UpdateVoltageDataGrid(voltage);
                        break;

                    case Mode.LoadCellTest:
                        DeleteAllPointsFromLoadCellTestData(loadCellTest);
                        Interface.UpdateVoltageDataGrid(voltage);
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

        public async Task DeleteAllPointsInAllModesAsync()
        {
            try
            {
                DeleteAllPointsFromThrustData(thrust.calibration);
                Interface.UpdateThrustDataGrid(thrust);

                DeleteAllPointsFromTorqueData(torque.calibration);
                Interface.UpdateTorqueDataGrid(torque);

                DeleteAllPointsFromCurrentData(current.calibration);
                Interface.UpdateCurrentDataGrid(current);

                DeleteAllPointsFromVoltageData(voltage.calibration);
                Interface.UpdateVoltageDataGrid(voltage);

                DeleteAllPointsFromLoadCellTestData(loadCellTest);
                Interface.UpdateVoltageDataGrid(voltage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAllPointsFromThrustData(Thrust.Calibration calibration)
        {
            calibration.PointAppliedBuffer.Clear();
            calibration.PointRawBuffer.Clear();
            calibration.PointErrorBuffer?.Clear();
        }

        private void DeleteAllPointsFromTorqueData(Torque.Calibration calibration)
        {
            calibration.PointAppliedBuffer.Clear();
            calibration.PointRawBuffer.Clear();
            calibration.PointErrorBuffer?.Clear();
        }

        private void DeleteAllPointsFromCurrentData(Current.Calibration calibration)
        {
            calibration.PointAppliedBuffer.Clear();
            calibration.PointRawBuffer.Clear();
        }

        private void DeleteAllPointsFromVoltageData(Voltage.Calibration calibration)
        {
            calibration.PointAppliedBuffer.Clear();
            calibration.PointRawBuffer.Clear();
        }

        private void DeleteAllPointsFromLoadCellTestData(LoadCellTest loadCellTest)
        {
            loadCellTest.Thrust.Buffer.Clear();
            loadCellTest.Thrust.AppliedBuffer.Clear();
            loadCellTest.Thrust.ErrorBuffer.Clear();
            loadCellTest.Thrust.FSErrorBuffer.Clear();

            loadCellTest.Torque.Buffer.Clear();
            loadCellTest.Torque.AppliedBuffer.Clear();
            loadCellTest.Torque.ErrorBuffer.Clear();
            loadCellTest.Torque.FSErrorBuffer.Clear();
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
                        await CalibrateThrustAsync(thrust.calibration, torque.calibration, "Thrust", 3);
                        break;
                    case Mode.Torque:
                        await CalibrateTorqueAsync(torque.calibration, thrust.calibration, "Torque", 3);
                        break;
                    case Mode.Current:
                        await CalibrateCurrentAsync(current.calibration, "Current", 2);
                        break;
                    case Mode.Voltage:
                        await CalibrateVoltageAsync(voltage.calibration, "Voltage", 2);
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
        private async Task CalibrateThrustAsync(Thrust.Calibration calibration, Torque.Calibration error, string modeName, int degree)
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


                // Hata katsayılarını hesapla
                var errorCoefficients = Fit.Polynomial(calibration.PointRawBuffer.ToArray(), calibration.PointErrorBuffer.ToArray(), degree);

                // Katsayıları kalibrasyona ekle
                calibration.Coefficient.A = coefficients.Length > 3 ? coefficients[3] : 0;
                calibration.Coefficient.B = coefficients.Length > 2 ? coefficients[2] : 0;
                calibration.Coefficient.C = coefficients.Length > 1 ? coefficients[1] : 0;
                calibration.Coefficient.D = coefficients.Length > 0 ? coefficients[0] : 0;

                error.Coefficient.ErrorA = errorCoefficients.Length > 3 ? errorCoefficients[3] : 0;
                error.Coefficient.ErrorB = errorCoefficients.Length > 2 ? errorCoefficients[2] : 0;
                error.Coefficient.ErrorC = errorCoefficients.Length > 1 ? errorCoefficients[1] : 0;
                error.Coefficient.ErrorD = errorCoefficients.Length > 0 ? errorCoefficients[0] : 0;

                // Denklemleri oluştur
                string equation = GeneratePolynomialEquation(coefficients, "x");
                string errorEquation = GeneratePolynomialEquation(errorCoefficients, "e");

                calibration.Coefficient.Equation = equation;
                error.Coefficient.ErrorEquation = errorEquation;

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
        private async Task CalibrateTorqueAsync(Torque.Calibration calibration, Thrust.Calibration error, string modeName, int degree)
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
                var errorCoefficients = Fit.Polynomial(calibration.PointRawBuffer.ToArray(), calibration.PointErrorBuffer.ToArray(), degree); ;

                // Katsayıları kalibrasyona ekle
                calibration.Coefficient.A = coefficients.Length > 3 ? coefficients[3] : 0;
                calibration.Coefficient.B = coefficients.Length > 2 ? coefficients[2] : 0;
                calibration.Coefficient.C = coefficients.Length > 1 ? coefficients[1] : 0;
                calibration.Coefficient.D = coefficients.Length > 0 ? coefficients[0] : 0;

                error.Coefficient.ErrorA = errorCoefficients.Length > 3 ? errorCoefficients[3] : 0;
                error.Coefficient.ErrorB = errorCoefficients.Length > 2 ? errorCoefficients[2] : 0;
                error.Coefficient.ErrorC = errorCoefficients.Length > 1 ? errorCoefficients[1] : 0;
                error.Coefficient.ErrorD = errorCoefficients.Length > 0 ? errorCoefficients[0] : 0;

                // Denklemleri oluştur
                string equation = GeneratePolynomialEquation(coefficients, "x");
                string errorEquation = GeneratePolynomialEquation(errorCoefficients, "e");

                calibration.Coefficient.Equation = equation;
                error.Coefficient.ErrorEquation = errorEquation;

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
        private async Task CalibrateCurrentAsync(Current.Calibration calibration, string modeName, int degree)
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

                // Katsayıları kalibrasyona ekle
                calibration.Coefficient = new Coefficients
                {
                    A = coefficients.Length > 3 ? coefficients[3] : 0,
                    B = coefficients.Length > 2 ? coefficients[2] : 0,
                    C = coefficients.Length > 1 ? coefficients[1] : 0,
                    D = coefficients.Length > 0 ? coefficients[0] : 0
                };

                // Denklemleri oluştur
                string equation = GeneratePolynomialEquation(coefficients, "x");

                calibration.Coefficient.Equation = equation;

                // Kullanıcıya sonuçları göster
                MessageBox.Show($"{modeName} calibration completed successfully.\n\n" +
                                $"Equation:\n{equation}\n\n",
                                "Calibration Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during {modeName} calibration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task CalibrateVoltageAsync(Voltage.Calibration calibration, string modeName, int degree)
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

                // Katsayıları kalibrasyona ekle
                calibration.Coefficient = new Coefficients
                {
                    A = coefficients.Length > 3 ? coefficients[3] : 0,
                    B = coefficients.Length > 2 ? coefficients[2] : 0,
                    C = coefficients.Length > 1 ? coefficients[1] : 0,
                    D = coefficients.Length > 0 ? coefficients[0] : 0
                };

                // Denklemleri oluştur
                string equation = GeneratePolynomialEquation(coefficients, "x");

                calibration.Coefficient.Equation = equation;

                // Kullanıcıya sonuçları göster
                MessageBox.Show($"{modeName} calibration completed successfully.\n\n" +
                                $"Equation:\n{equation}\n\n",
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

        #region Calculation
        public async Task CalculateThrustValueAsync(Thrust thrust,Torque torque)
        {
            
            try
            {
                if (thrust.calibration?.Coefficient == null)
                {
                    throw new InvalidOperationException("Hata katsayıları eksik. Hesaplama devam edemiyor.");
                }

                // Katsayıları al
                var coeff = thrust.calibration.Coefficient;

                // Error Raw Value = A * x³ + B * x² + C * x + D
                double calculatedErrorRawValue = coeff.ErrorA * Math.Pow(torque.raw.Value, 3) +
                                              coeff.ErrorB * Math.Pow(torque.raw.Value, 2) +
                                              coeff.ErrorC * torque.raw.Value +
                                              coeff.ErrorD;

                double rawValue = thrust.raw.Value - calculatedErrorRawValue;

                // Error Value = A * x³ + B * x² + C * x + D
                double calculatedErrorValue = coeff.A * Math.Pow(calculatedErrorRawValue, 3) +
                                         coeff.B * Math.Pow(calculatedErrorRawValue, 2) +
                                         coeff.C * calculatedErrorRawValue +
                                         coeff.D;

                // Value = A * x³ + B * x² + C * x + D
                double calculatedValue = coeff.A * Math.Pow(rawValue, 3) +
                                         coeff.B * Math.Pow(rawValue, 2) +
                                         coeff.C * rawValue +
                                         coeff.D;

                // Noise Value = A * x³ + B * x² + C * x + D
                double calculatedNoiseValue = coeff.A * Math.Pow(thrust.raw.NoiseValue, 3) +
                                              coeff.B * Math.Pow(thrust.raw.NoiseValue, 2) +
                                              coeff.C * thrust.raw.NoiseValue +
                                              coeff.D;

                await Task.Delay(10); // Simülasyon için kısa bir gecikme

                // Hesaplanan değerleri güncelle
                thrust.calculated.Value = Math.Round(calculatedValue, 3); 
                thrust.calculated.RawValue = Math.Round(rawValue, 3); 
                thrust.calculated.ErrorValue = Math.Round(calculatedErrorValue, 3); 
                thrust.raw.ErrorValue = Math.Round(calculatedErrorRawValue, 3); 
                thrust.calculated.NoiseValue = Math.Round(calculatedNoiseValue, 3); 
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                MessageBox.Show($"Error in calculation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async Task CalculateTorqueValueAsync(Torque torque,Thrust thrust)
        {

            try
            {
                if (torque.calibration?.Coefficient == null)
                {
                    throw new InvalidOperationException("Hata katsayıları eksik. Hesaplama devam edemiyor.");
                }

                // Katsayıları al
                var coeff = torque.calibration.Coefficient;

                // Error Raw Value = A * x³ + B * x² + C * x + D
                double calculatedErrorRawValue = coeff.ErrorA * Math.Pow(thrust.raw.Value, 3) +
                                              coeff.ErrorB * Math.Pow(thrust.raw.Value, 2) +
                                              coeff.ErrorC * thrust.raw.Value +
                                              coeff.ErrorD;

                double rawValue = torque.raw.Value - calculatedErrorRawValue;

                // Error Value = A * x³ + B * x² + C * x + D
                double calculatedErrorValue = coeff.A * Math.Pow(calculatedErrorRawValue, 3) +
                                         coeff.B * Math.Pow(calculatedErrorRawValue, 2) +
                                         coeff.C * calculatedErrorRawValue +
                                         coeff.D;

                // Value = A * x³ + B * x² + C * x + D
                double calculatedValue = coeff.A * Math.Pow(rawValue, 3) +
                                         coeff.B * Math.Pow(rawValue, 2) +
                                         coeff.C * rawValue +
                                         coeff.D;

                // Noise Value = A * x³ + B * x² + C * x + D
                double calculatedNoiseValue = coeff.A * Math.Pow(torque.raw.NoiseValue, 3) +
                                              coeff.B * Math.Pow(torque.raw.NoiseValue, 2) +
                                              coeff.C * torque.raw.NoiseValue +
                                              coeff.D;

                await Task.Delay(10); // Simülasyon için kısa bir gecikme

                // Hesaplanan değerleri güncelle
                torque.calculated.Value = Math.Round(calculatedValue, 3);
                torque.calculated.RawValue = Math.Round(rawValue, 3);
                torque.calculated.ErrorValue = Math.Round(calculatedErrorValue, 3);
                torque.raw.ErrorValue = Math.Round(calculatedErrorRawValue, 3);
                torque.calculated.NoiseValue = Math.Round(calculatedNoiseValue, 3);
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                MessageBox.Show($"Error in calculation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async Task CalculateCurrentValueAsync(Current current)
        {

            try
            {
                if (current.calibration?.Coefficient == null)
                {
                    throw new InvalidOperationException("Hata katsayıları eksik. Hesaplama devam edemiyor.");
                }

                // Katsayıları al
                var coeff = current.calibration.Coefficient;

                // Value = A * x³ + B * x² + C * x + D
                double calculatedValue = coeff.A * Math.Pow(current.raw.Value, 3) +
                                         coeff.B * Math.Pow(current.raw.Value, 2) +
                                         coeff.C * current.raw.Value +
                                         coeff.D;

                // Noise Value = A * x³ + B * x² + C * x + D
                double calculatedNoiseValue = coeff.A * Math.Pow(current.raw.NoiseValue, 3) +
                                              coeff.B * Math.Pow(current.raw.NoiseValue, 2) +
                                              coeff.C * current.raw.NoiseValue +
                                              coeff.D;

                await Task.Delay(10); // Simülasyon için kısa bir gecikme

                // Hesaplanan değerleri güncelle
                current.calculated.Value = Math.Round(calculatedValue, 3);
                current.calculated.RawValue = Math.Round(current.raw.Value, 3);
                current.calculated.NoiseValue = Math.Round(calculatedNoiseValue, 3);
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                MessageBox.Show($"Error in calculation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async Task CalculateVoltageValueAsync(Voltage voltage)
        {

            try
            {
                if (voltage.calibration?.Coefficient == null)
                {
                    throw new InvalidOperationException("Hata katsayıları eksik. Hesaplama devam edemiyor.");
                }

                // Katsayıları al
                var coeff = voltage.calibration.Coefficient;

                // Value = A * x³ + B * x² + C * x + D
                double calculatedValue = coeff.A * Math.Pow(voltage.raw.Value, 3) +
                                         coeff.B * Math.Pow(voltage.raw.Value, 2) +
                                         coeff.C * voltage.raw.Value +
                                         coeff.D;

                // Noise Value = A * x³ + B * x² + C * x + D
                double calculatedNoiseValue = coeff.A * Math.Pow(voltage.raw.NoiseValue, 3) +
                                              coeff.B * Math.Pow(voltage.raw.NoiseValue, 2) +
                                              coeff.C * voltage.raw.NoiseValue +
                                              coeff.D;

                await Task.Delay(10); // Simülasyon için kısa bir gecikme

                // Hesaplanan değerleri güncelle
                voltage.calculated.Value = Math.Round(calculatedValue, 3);
                voltage.calculated.RawValue = Math.Round(voltage.raw.Value, 3);
                voltage.calculated.NoiseValue = Math.Round(calculatedNoiseValue, 3);
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                MessageBox.Show($"Error in calculation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Excel Export
        public async Task ExcelExportAsync()
        {
            try
            {
                // Kullanıcıdan dosya yolu seçmesini isteyin
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                    Title = "Excel Dosyasını Kaydet",
                    FileName = "Veriler.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // Excel dosyasını oluştur
                    using (var package = new OfficeOpenXml.ExcelPackage())
                    {
                        // İtki Verileri
                        if (Interface.ThrustData != null && Interface.ThrustData.Any())
                        {
                            var thrustSheet = package.Workbook.Worksheets.Add("İtki Verileri");
                            FillThrustWorksheet(thrustSheet, Interface);
                        }

                        // Tork Verileri
                        if (Interface.TorqueData != null && Interface.TorqueData.Any())
                        {
                            var torqueSheet = package.Workbook.Worksheets.Add("Tork Verileri");
                            FillTorqueWorksheet(torqueSheet, Interface);
                        }

                        // Load Cell Test Verileri
                        if (Interface.LoadCellTestData != null && Interface.LoadCellTestData.Any())
                        {
                            var loadCellSheet = package.Workbook.Worksheets.Add("Load Cell Test Verileri");
                            FillLoadCellTestWorksheet(loadCellSheet, Interface);
                        }

                        // Akım Verileri
                        if (Interface.CurrentData != null && Interface.CurrentData.Any())
                        {
                            var currentSheet = package.Workbook.Worksheets.Add("Akım Verileri");
                            FillCurrentWorksheet(currentSheet, Interface);
                        }

                        // Voltaj Verileri
                        if (Interface.VoltageData != null && Interface.VoltageData.Any())
                        {
                            var voltageSheet = package.Workbook.Worksheets.Add("Voltaj Verileri");
                            FillVoltageWorksheet(voltageSheet, Interface);
                        }

                        // Dosyayı kaydet
                        await package.SaveAsAsync(new FileInfo(filePath));
                        MessageBox.Show("Veriler başarıyla Excel'e aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel'e aktarma sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillThrustWorksheet(OfficeOpenXml.ExcelWorksheet worksheet, InterfaceData DataGrid)
        {
            // Başlıkları ekle
            string[] headers = new[] { "No", "Uygulanan İtki (gr)", "Okunan İtki (ADC)", "Okunan Tork (ADC)" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Verileri ekle
            int row = 2; // Veriler 2. satırdan itibaren başlıyor
            foreach (var thrustDataGrid in DataGrid.ThrustData)
            {
                worksheet.Cells[row, 1].Value = thrustDataGrid.No;                  // "No" sütunu
                worksheet.Cells[row, 2].Value = thrustDataGrid.Applied_Thrust;      // "Uygulanan İtki (gr)" sütunu
                worksheet.Cells[row, 3].Value = thrustDataGrid.ADC_Thrust;          // "Okunan İtki (ADC)" sütunu
                worksheet.Cells[row, 4].Value = thrustDataGrid.ADC_Torque;          // "Okunan Tork (ADC)" sütunu
                row++; // Bir sonraki satıra geç
            }

            // Otomatik sütun genişliği
            worksheet.Cells.AutoFitColumns();
        }
        private void FillTorqueWorksheet(OfficeOpenXml.ExcelWorksheet worksheet, InterfaceData DataGrid)
        {
            // Başlıkları ekle
            string[] headers = new[] { "No", "Uygulanan Tork (Nmm)", "Okunan Tork (ADC)", "Okunan İtki (ADC)" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Verileri ekle
            int row = 2; // Veriler 2. satırdan itibaren başlıyor
            foreach (var torqueDataGrid in DataGrid.TorqueData)
            {
                worksheet.Cells[row, 1].Value = torqueDataGrid.No;                  // "No" sütunu
                worksheet.Cells[row, 2].Value = torqueDataGrid.Applied_Torque;      // "Uygulanan Tork (Nmm)" sütunu
                worksheet.Cells[row, 3].Value = torqueDataGrid.ADC_Torque;          // "Okunan Tork (ADC)" sütunu
                worksheet.Cells[row, 4].Value = torqueDataGrid.ADC_Thrust;          // "Okunan İtki (ADC)" sütunu
                row++; // Bir sonraki satıra geç
            }

            // Otomatik sütun genişliği
            worksheet.Cells.AutoFitColumns();
        }
        private void FillCurrentWorksheet(OfficeOpenXml.ExcelWorksheet worksheet, InterfaceData DataGrid)
        {
            // Başlıkları ekle
            string[] headers = new[] { "No", "Uygulanan Akım (mA)", "Okunan Akım (ADC)" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Verileri ekle
            int row = 2; // Veriler 2. satırdan itibaren başlıyor
            foreach (var currentDataGrid in DataGrid.CurrentData)
            {
                worksheet.Cells[row, 1].Value = currentDataGrid.No;                  // "No" sütunu
                worksheet.Cells[row, 2].Value = currentDataGrid.Applied_Current;      // "Uygulanan Akım (mA)" sütunu
                worksheet.Cells[row, 3].Value = currentDataGrid.ADC_Current;          // "Okunan Akım (ADC)" sütunu
                row++; // Bir sonraki satıra geç
            }

            // Otomatik sütun genişliği
            worksheet.Cells.AutoFitColumns();
        }
        private void FillVoltageWorksheet(OfficeOpenXml.ExcelWorksheet worksheet, InterfaceData DataGrid)
        {
            // Başlıkları ekle
            string[] headers = new[] { "No", "Uygulanan Voltaj (mV)", "Okunan Voltaj (ADC)" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Verileri ekle
            int row = 2; // Veriler 2. satırdan itibaren başlıyor
            foreach (var voltageDataGrid in DataGrid.VoltageData)
            {
                worksheet.Cells[row, 1].Value = voltageDataGrid.No;                  // "No" sütunu
                worksheet.Cells[row, 2].Value = voltageDataGrid.Applied_Voltage;      // "Uygulanan Voltaj (mV)" sütunu
                worksheet.Cells[row, 3].Value = voltageDataGrid.ADC_Voltage;          // "Okunan Voltaj (ADC)" sütunu
                row++; // Bir sonraki satıra geç
            }

            // Otomatik sütun genişliği
            worksheet.Cells.AutoFitColumns();
        }
        private void FillLoadCellTestWorksheet(OfficeOpenXml.ExcelWorksheet worksheet, InterfaceData DataGrid)
        {
            // İlk tablo için başlangıç satırı
            int startRow1 = 1;

            // İlk tablo başlıklarını ekle
            string[] headers1 = new[] { "No", "Uygulanan İtki (gr)", "Hesaplanan İtki (gr)", "Hata (%)", "FS Hata (%)" };
            for (int i = 0; i < headers1.Length; i++)
            {
                worksheet.Cells[startRow1, i + 1].Value = headers1[i];
                worksheet.Cells[startRow1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[startRow1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // İlk tablo verilerini ekle
            int row1 = startRow1 + 1; // Veriler başlıktan sonraki satırdan başlıyor
            foreach (var thrustDataGrid in DataGrid.LoadCellTestData)
            {
                worksheet.Cells[row1, 1].Value = thrustDataGrid.No;                     // "No" sütunu
                worksheet.Cells[row1, 2].Value = thrustDataGrid.Applied_Thrust;         // "Uygulanan İtki (gr)" sütunu
                worksheet.Cells[row1, 3].Value = thrustDataGrid.Calculated_Thrust;      // "Hesaplanan İtki (gr)" sütunu
                worksheet.Cells[row1, 4].Value = thrustDataGrid.Error_Thrust;           // "Hata (%)" sütunu
                worksheet.Cells[row1, 5].Value = thrustDataGrid.FSError_Thrust;         // "FS Hata (%)" sütunu
                row1++; // Bir sonraki satıra geç
            }

            // İkinci tablo için başlangıç satırı (ilk tablodan sonra boş bir satır bırakabilirsiniz)
            int startRow2 = row1 + 2;

            // İkinci tablo başlıklarını ekle
            string[] headers2 = new[] { "No", "Uygulanan Tork (Nmm)", "Hesaplanan Tork (Nmm)", "Hata (%)", "FS Hata (%)" };
            for (int i = 0; i < headers2.Length; i++)
            {
                worksheet.Cells[startRow2, i + 1].Value = headers2[i];
                worksheet.Cells[startRow2, i + 1].Style.Font.Bold = true;
                worksheet.Cells[startRow2, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // İkinci tablo verilerini ekle
            int row2 = startRow2 + 1; // Veriler başlıktan sonraki satırdan başlıyor
            foreach (var torqueDataGrid in DataGrid.LoadCellTestData)
            {
                worksheet.Cells[row2, 1].Value = torqueDataGrid.No;                     // "No" sütunu
                worksheet.Cells[row2, 2].Value = torqueDataGrid.Applied_Torque;         // "Uygulanan Tork (Nmm)" sütunu
                worksheet.Cells[row2, 3].Value = torqueDataGrid.Calculated_Torque;      // "Hesaplanan Tork (Nmm)" sütunu
                worksheet.Cells[row2, 4].Value = torqueDataGrid.Error_Torque;           // "Hata (%)" sütunu
                worksheet.Cells[row2, 5].Value = torqueDataGrid.FSError_Torque;         // "FS Hata (%)" sütunu
                row2++; // Bir sonraki satıra geç
            }

            // Otomatik sütun genişliği
            worksheet.Cells.AutoFitColumns();
        }
        #endregion

        #region Excel Import
        public async Task ExcelImportAsync()
        {
            try
            {
                // Kullanıcıdan dosya seçmesini isteyin
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                    Title = "Excel Dosyasını Aç"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;

                    // Excel dosyasını aç
                    using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
                    {
                        // İtki Verileri
                        var thrustSheet = package.Workbook.Worksheets["İtki Verileri"];
                        if (thrustSheet != null)
                        {
                            ObservableCollection<ThrustDataGrid> temp = ReadThrustWorksheet(thrustSheet);
                            Interface.ThrustData.Clear(); 
                            foreach (var item in temp)
                            {
                                Interface.ThrustData.Add(item); 
                            }
                            Interface.UpdateThrustFromData(thrust);
                        }

                        // Tork Verileri
                        var torqueSheet = package.Workbook.Worksheets["Tork Verileri"];
                        if (torqueSheet != null)
                        {
                            ObservableCollection<TorqueDataGrid> temp = ReadTorqueWorksheet(torqueSheet);
                            Interface.TorqueData.Clear();
                            foreach (var item in temp)
                            {
                                Interface.TorqueData.Add(item);
                            }
                            Interface.UpdateTorqueFromData(torque);
                        }

                        // Load Cell Test Verileri
                        var loadCellSheet = package.Workbook.Worksheets["Load Cell Test Verileri"];
                        if (loadCellSheet != null)
                        {
                            ObservableCollection<LoadCellTestDataGrid> temp = ReadLoadCellTestWorksheet(loadCellSheet);
                            Interface.LoadCellTestData.Clear();
                            foreach (var item in temp)
                            {
                                Interface.LoadCellTestData.Add(item);
                            }
                            Interface.UpdateLoadCellTestFromData(loadCellTest);
                        }

                        // Akım Verileri
                        var currentSheet = package.Workbook.Worksheets["Akım Verileri"];
                        if (currentSheet != null)
                        {
                            ObservableCollection<CurrentDataGrid> temp = ReadCurrentWorksheet(currentSheet);
                            Interface.CurrentData.Clear();
                            foreach (var item in temp)
                            {
                                Interface.CurrentData.Add(item);
                            }
                            Interface.UpdateCurrentFromData(current);
                        }

                        // Voltaj Verileri
                        var voltageSheet = package.Workbook.Worksheets["Voltaj Verileri"];
                        if (voltageSheet != null)
                        {
                            ObservableCollection<VoltageDataGrid> temp = ReadVoltageWorksheet(voltageSheet);
                            Interface.VoltageData.Clear(); 
                            foreach (var item in temp)
                            {
                                Interface.VoltageData.Add(item);
                            }
                            Interface.UpdateVoltageFromData(voltage);
                        }

                        MessageBox.Show("Excel dosyası başarıyla içeri aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel'den içeri aktarma sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private ObservableCollection<ThrustDataGrid> ReadThrustWorksheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            var thrustData = new ObservableCollection<ThrustDataGrid>();
            int row = 2; // Veriler 2. satırdan başlıyor

            while (worksheet.Cells[row, 1].Value != null)
            {
                // Eğer hücre değeri "No" ise, bu satırı atla
                if (worksheet.Cells[row, 1].Value.ToString() == "No")
                {
                    row++;
                    continue;
                }

                var data = new ThrustDataGrid
                {
                    No = SafeConvertToInt(worksheet.Cells[row, 1].Value),
                    Applied_Thrust = SafeConvertToDouble(worksheet.Cells[row, 2].Value),
                    ADC_Thrust = SafeConvertToDouble(worksheet.Cells[row, 3].Value),
                    ADC_Torque = SafeConvertToDouble(worksheet.Cells[row, 4].Value)
                };
                thrustData.Add(data);
                row++;
            }

            return thrustData;
        }

        private ObservableCollection<TorqueDataGrid> ReadTorqueWorksheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            var torqueData = new ObservableCollection<TorqueDataGrid>();
            int row = 2; // Veriler 2. satırdan başlıyor

            while (worksheet.Cells[row, 1].Value != null)
            {
                // Eğer hücre değeri "No" ise, bu satırı atla
                if (worksheet.Cells[row, 1].Value.ToString() == "No")
                {
                    row++;
                    continue;
                }

                var data = new TorqueDataGrid
                {
                    No = SafeConvertToInt(worksheet.Cells[row, 1].Value),
                    Applied_Torque = SafeConvertToDouble(worksheet.Cells[row, 2].Value),
                    ADC_Torque = SafeConvertToDouble(worksheet.Cells[row, 3].Value),
                    ADC_Thrust = SafeConvertToDouble(worksheet.Cells[row, 4].Value)
                };
                torqueData.Add(data);
                row++;
            }

            return torqueData;
        } 

        private ObservableCollection<CurrentDataGrid> ReadCurrentWorksheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            var currentData = new ObservableCollection<CurrentDataGrid>();
            int row = 2; // Veriler 2. satırdan başlıyor

            while (worksheet.Cells[row, 1].Value != null)
            {
                // Eğer hücre değeri "No" ise, bu satırı atla
                if (worksheet.Cells[row, 1].Value.ToString() == "No")
                {
                    row++;
                    continue;
                }

                var data = new CurrentDataGrid
                {
                    No = SafeConvertToInt(worksheet.Cells[row, 1].Value),
                    Applied_Current = SafeConvertToDouble(worksheet.Cells[row, 2].Value),
                    ADC_Current = SafeConvertToDouble(worksheet.Cells[row, 3].Value)
                };
                currentData.Add(data);
                row++;
            }

            return currentData;
        }
        private ObservableCollection<VoltageDataGrid> ReadVoltageWorksheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            var voltageData = new ObservableCollection<VoltageDataGrid>();
            int row = 2; // Veriler 2. satırdan başlıyor

            while (worksheet.Cells[row, 1].Value != null)
            {
                // Eğer hücre değeri "No" ise, bu satırı atla
                if (worksheet.Cells[row, 1].Value.ToString() == "No")
                {
                    row++;
                    continue;
                }

                var data = new VoltageDataGrid
                {
                    No = SafeConvertToInt(worksheet.Cells[row, 1].Value),
                    Applied_Voltage = SafeConvertToDouble(worksheet.Cells[row, 2].Value),
                    ADC_Voltage = SafeConvertToDouble(worksheet.Cells[row, 3].Value)
                };
                voltageData.Add(data);
                row++;
            }

            return voltageData;
        }
        private ObservableCollection<LoadCellTestDataGrid> ReadLoadCellTestWorksheet(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            var loadCellData = new ObservableCollection<LoadCellTestDataGrid>();

            // İlk tabloyu oku
            int startRow1 = 2; // İlk tablo verileri 2. satırdan başlıyor
            while (worksheet.Cells[startRow1, 1].Value != null)
            {
                // Eğer hücre değeri "No" ise, bu satırı atla
                if (worksheet.Cells[startRow1, 1].Value.ToString() == "No")
                {
                    startRow1++;
                    continue;
                }

                var data = new LoadCellTestDataGrid
                {
                    No = SafeConvertToInt(worksheet.Cells[startRow1, 1].Value),                     // "No" sütunu
                    Applied_Thrust = SafeConvertToDouble(worksheet.Cells[startRow1, 2].Value),        // "Uygulanan İtki (gr)" sütunu
                    Calculated_Thrust = SafeConvertToDouble(worksheet.Cells[startRow1, 3].Value),     // "Hesaplanan İtki (gr)" sütunu
                    Error_Thrust = SafeConvertToDouble(worksheet.Cells[startRow1, 4].Value),          // "Hata (%)" sütunu
                    FSError_Thrust = SafeConvertToDouble(worksheet.Cells[startRow1, 5].Value)         // "FS Hata (%)" sütunu
                };
                loadCellData.Add(data);
                startRow1++; // Bir sonraki satıra geç
            }

            // İkinci tabloyu oku
            int startRow2 = startRow1 + 2; // İkinci tablo, ilk tablodan sonra 2 satır boşluk bırakılarak başlıyor
            while (worksheet.Cells[startRow2, 1].Value != null)
            {
                // Eğer hücre değeri "No" ise, bu satırı atla
                if (worksheet.Cells[startRow2, 1].Value.ToString() == "No")
                {
                    startRow2++;
                    continue;
                }

                var data = new LoadCellTestDataGrid
                {
                    No = SafeConvertToInt(worksheet.Cells[startRow2, 1].Value),                     // "No" sütunu
                    Applied_Torque = SafeConvertToDouble(worksheet.Cells[startRow2, 2].Value),        // "Uygulanan Tork (Nmm)" sütunu
                    Calculated_Torque = SafeConvertToDouble(worksheet.Cells[startRow2, 3].Value),     // "Hesaplanan Tork (Nmm)" sütunu
                    Error_Torque = SafeConvertToDouble(worksheet.Cells[startRow2, 4].Value),          // "Hata (%)" sütunu
                    FSError_Torque = SafeConvertToDouble(worksheet.Cells[startRow2, 5].Value)         // "FS Hata (%)" sütunu
                };
                loadCellData.Add(data);
                startRow2++; // Bir sonraki satıra geç
            }

            return loadCellData;
        }
        private int SafeConvertToInt(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0; // Varsayılan değer döndür
            if (int.TryParse(value.ToString(), out int result))
                return result;
            throw new FormatException($"Geçersiz tamsayı formatı: {value}");
        }

        private double SafeConvertToDouble(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0.0; // Varsayılan değer döndür
            if (double.TryParse(value.ToString(), out double result))
                return result;
            throw new FormatException($"Geçersiz double formatı: {value}");
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
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            // İtki ve Tork hesaplama işlemlerini başlat
                            await CalculateThrustValueAsync(thrust, torque);
                            await CalculateTorqueValueAsync(torque, thrust);
                            await CalculateCurrentValueAsync(current);
                            await CalculateVoltageValueAsync(voltage);

                            Interface.PortReadData = $"Port Message: {portReadData}";
                            Interface.PortReadTime = portReadTime;

                            thrust.calibration.AddingOn = Interface.Thrust.calibration.AddingOn;
                            torque.calibration.AddingOn = Interface.Torque.calibration.AddingOn;

                            Interface.Current.raw.Value = current.raw.Value;
                            Interface.Current.raw.NoiseValue = current.raw.NoiseValue;
                            Interface.Current.calculated.Value = current.calculated.Value;
                            Interface.Current.calculated.NoiseValue = current.calculated.NoiseValue;
                            Interface.Current.calculated.RawValue = current.calculated.RawValue;


                            Interface.Voltage.raw.Value = voltage.raw.Value;
                            Interface.Voltage.raw.NoiseValue = voltage.raw.NoiseValue;
                            Interface.Voltage.calculated.Value = voltage.calculated.Value;
                            Interface.Voltage.calculated.NoiseValue = voltage.calculated.NoiseValue;
                            Interface.Voltage.calculated.RawValue = voltage.calculated.RawValue;

                            Interface.Thrust.raw.Value = thrust.raw.Value;
                            Interface.Thrust.raw.NoiseValue = thrust.raw.NoiseValue;
                            Interface.Thrust.raw.ErrorValue = thrust.raw.ErrorValue;
                            Interface.Thrust.calculated.RawValue = thrust.calculated.RawValue;

                            Interface.Thrust.calculated.NoiseValue = thrust.calculated.NoiseValue;
                            Interface.Thrust.calculated.Value = thrust.calculated.Value;
                            Interface.Thrust.calculated.NetValue = thrust.calculated.NetValue;

                            Interface.Torque.raw.Value = torque.raw.Value;
                            Interface.Torque.raw.NoiseValue = torque.raw.NoiseValue;
                            Interface.Torque.raw.ErrorValue = torque.raw.ErrorValue;
                            Interface.Torque.calculated.RawValue = torque.calculated.RawValue;

                            Interface.Torque.calculated.NoiseValue = torque.calculated.NoiseValue;
                            Interface.Torque.calculated.Value = torque.calculated.Value;
                            Interface.Torque.calculated.NetValue = torque.calculated.NetValue;


                            Interface.Thrust.calibration.Coefficient.Equation = "İtki (gF) = " + thrust.calibration.Coefficient.Equation;
                            Interface.Torque.calibration.Coefficient.Equation = "Tork (Nmm) = " + torque.calibration.Coefficient.Equation;
                            Interface.Current.calibration.Coefficient.Equation = "Akım (mA) = " + current.calibration.Coefficient.Equation;
                            Interface.Voltage.calibration.Coefficient.Equation = "Voltaj (mV) = " + voltage.calibration.Coefficient.Equation;
                            Interface.Thrust.calibration.Coefficient.ErrorEquation = "İtki Hatası (ADC) = " + thrust.calibration.Coefficient.ErrorEquation;
                            Interface.Torque.calibration.Coefficient.ErrorEquation = "Tork Hatası (ADC) = " + torque.calibration.Coefficient.ErrorEquation;
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
            Interface.Dividing = 100;

            thrust.calibration.AddingOn = true;
            torque.calibration.AddingOn = true;

            Interface.Thrust.calibration.AddingOn = thrust.calibration.AddingOn;
            Interface.Torque.calibration.AddingOn = torque.calibration.AddingOn;

            thrust.calibration.Coefficient.Equation = "a₁x³ + a₂x² + a₃x + c";
            torque.calibration.Coefficient.Equation = "b₁x³ + b₂x² + b₃x + d";
            current.calibration.Coefficient.Equation = "c₁x³ + c₂x² + c₃x + d";
            voltage.calibration.Coefficient.Equation = "d₁x³ + d₂x² + d₃x + e";
            thrust.calibration.Coefficient.ErrorEquation = "a₁x³ + a₂x² + a₃x + c";
            torque.calibration.Coefficient.ErrorEquation = "b₁x³ + b₂x² + b₃x + d";

            Interface.Thrust.calibration.Coefficient.Equation = "İtki (gF) = " + thrust.calibration.Coefficient.Equation;
            Interface.Torque.calibration.Coefficient.Equation = "Tork (Nmm) = " + torque.calibration.Coefficient.Equation;
            Interface.Current.calibration.Coefficient.Equation = "Akım (mA) = " + current.calibration.Coefficient.Equation;
            Interface.Voltage.calibration.Coefficient.Equation = "Voltaj (mV) = " + voltage.calibration.Coefficient.Equation;
            Interface.Thrust.calibration.Coefficient.ErrorEquation = "İtki Hatası (ADC) = " + thrust.calibration.Coefficient.ErrorEquation;
            Interface.Torque.calibration.Coefficient.ErrorEquation = "Tork Hatası (ADC) = " + torque.calibration.Coefficient.ErrorEquation;

        }
        #endregion

        #region Update Plot Data
        private CancellationTokenSource _updatePlotDataLoopCancellationTokenSource;

        private int PlotUpdateTimeMillisecond = 10; // 100 Hz (10ms)

        private readonly object _PlotDataLock = new();
        private async Task UpdatePlotDataLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(PlotUpdateTimeMillisecond, token);

                    // Son verileri al
                    double latestTime, thrustValue, torqueValue, currentValue, voltageValue;

                    lock (_PlotDataLock)
                    {
                        latestTime = portReadTime;
                        thrustValue = thrust.raw.Value;
                        torqueValue = torque.raw.Value;
                        currentValue = current.raw.Value;
                        voltageValue = voltage.raw.Value;
                    }

                    // Grafiği güncelle
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (PlotModel.Series[0] is LineSeries thrustSeries)
                        {
                            try
                            {
                                thrustSeries.Points.Add(new DataPoint(latestTime, thrustValue));
                                while (thrustSeries.Points.Count > Interface.Dividing) thrustSeries.Points.RemoveAt(0);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Plot update loop error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }  
                        }

                        if (PlotModel.Series[1] is LineSeries torqueSeries)
                        {
                            try
                            {
                                torqueSeries.Points.Add(new DataPoint(latestTime, torqueValue));
                                while (torqueSeries.Points.Count > Interface.Dividing) torqueSeries.Points.RemoveAt(0);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Plot update loop error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                        if (PlotModel.Series[2] is LineSeries currentSeries)
                        {
                            try
                            {
                                currentSeries.Points.Add(new DataPoint(latestTime, currentValue));
                                while (currentSeries.Points.Count > Interface.Dividing) currentSeries.Points.RemoveAt(0);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Plot update loop error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                        if (PlotModel.Series[3] is LineSeries voltageSeries)
                        {
                            try
                            {
                                voltageSeries.Points.Add(new DataPoint(latestTime, voltageValue));
                                while (voltageSeries.Points.Count > Interface.Dividing) voltageSeries.Points.RemoveAt(0);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Plot update loop error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                        PlotModel.InvalidatePlot(true); // Grafiği yeniden çiz
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Plot update loop error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
