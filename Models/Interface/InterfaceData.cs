using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Device;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Services;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Interface
{
    public class InterfaceData : BindableBase
    {
        private string _portReadData;
        public string PortReadData
        {
            get => _portReadData;
            set => SetProperty(ref _portReadData, value);
        }
        private double _portReadTime;
        public double PortReadTime
        {
            get => _portReadTime;
            set => SetProperty(ref _portReadTime, value);
        }
        private double _progress = 0;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private double _dividing = 0;
        public double Dividing
        {
            get => _dividing;
            set => SetProperty(ref _dividing, value);
        }

        private Mode _mode;
        public Mode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnPropertyChanged();
                }
            }
        }
        public Thrust Thrust { get; set; }
        public Torque Torque { get; set; }
        public LoadCellTest LoadCellTest { get; set; }
        public Current Current { get; set; }
        public Voltage Voltage { get; set; }
        public ObservableCollection<ThrustDataGrid> ThrustData { get; set; }
        public ObservableCollection<TorqueDataGrid> TorqueData { get; set; }
        public ObservableCollection<LoadCellTestDataGrid> LoadCellTestData { get; set; }
        public ObservableCollection<CurrentDataGrid> CurrentData { get; set; }
        public ObservableCollection<VoltageDataGrid> VoltageData { get; set; }
        public InterfaceData()
        {
            Thrust = new Thrust();
            Torque = new Torque();
            LoadCellTest = new LoadCellTest();
            Current = new Current();
            Voltage = new Voltage();

            ThrustData = new ObservableCollection<ThrustDataGrid>();
            TorqueData = new ObservableCollection<TorqueDataGrid>();
            LoadCellTestData = new ObservableCollection<LoadCellTestDataGrid>();
            CurrentData = new ObservableCollection<CurrentDataGrid>();
            VoltageData = new ObservableCollection<VoltageDataGrid>();
        }
        public void UpdateThrustDataGrid(Thrust thrust)
        {
            ThrustData.Clear();
            for (int i = 0; i < thrust.calibration.PointRawBuffer.Count; i++)
            {
                ThrustData.Add(new ThrustDataGrid
                {
                    No = i + 1,
                    ADC_Thrust = thrust.calibration.PointRawBuffer[i],
                    Applied_Thrust = thrust.calibration.PointAppliedBuffer[i],
                    ADC_Torque = thrust.calibration.PointErrorBuffer[i]
                });
            }
        }
        public void UpdateThrustFromData(Thrust thrust)
        {
            // Önce mevcut buffer'ları temizliyoruz
            thrust.calibration.PointRawBuffer.Clear();
            thrust.calibration.PointAppliedBuffer.Clear();
            thrust.calibration.PointErrorBuffer.Clear();

            // ThrustData koleksiyonundaki verileri ilgili buffer'lara ekliyoruz
            foreach (var data in ThrustData)
            {
                thrust.calibration.PointRawBuffer.Add(data.ADC_Thrust);
                thrust.calibration.PointAppliedBuffer.Add(data.Applied_Thrust);
                thrust.calibration.PointErrorBuffer.Add(data.ADC_Torque);
            }
        }
        public void UpdateTorqueDataGrid(Torque torque)
        {
            TorqueData.Clear();
            for (int i = 0; i < torque.calibration.PointRawBuffer.Count; i++)
            {
                TorqueData.Add(new TorqueDataGrid
                {
                    No = i + 1,
                    ADC_Torque = torque.calibration.PointRawBuffer[i],
                    Applied_Torque = torque.calibration.PointAppliedBuffer[i],
                    ADC_Thrust = torque.calibration.PointErrorBuffer[i]
                });
            }
        }
        public void UpdateTorqueFromData(Torque torque)
        {
            // Mevcut buffer'ları temizliyoruz
            torque.calibration.PointRawBuffer.Clear();
            torque.calibration.PointAppliedBuffer.Clear();
            torque.calibration.PointErrorBuffer.Clear();

            // TorqueData koleksiyonundaki verileri ilgili buffer'lara ekliyoruz
            foreach (var data in TorqueData)
            {
                torque.calibration.PointRawBuffer.Add(data.ADC_Torque);
                torque.calibration.PointAppliedBuffer.Add(data.Applied_Torque);
                torque.calibration.PointErrorBuffer.Add(data.ADC_Thrust);
            }
        }

        public void UpdateLoadCellTestDataGrid(LoadCellTest loadCellTest)
        {
            LoadCellTestData.Clear();
            for (int i = 0; i < loadCellTest.Thrust.Buffer.Count; i++)
            {
                LoadCellTestData.Add(new LoadCellTestDataGrid
                {
                    No = i + 1,

                    Applied_Thrust = loadCellTest.Thrust.AppliedBuffer[i],
                    Calculated_Thrust = loadCellTest.Thrust.Buffer[i],
                    Error_Thrust = loadCellTest.Thrust.ErrorBuffer[i],
                    FSError_Thrust = loadCellTest.Thrust.FSErrorBuffer[i],

                    Applied_Torque = loadCellTest.Torque.AppliedBuffer[i],
                    Calculated_Torque = loadCellTest.Torque.Buffer[i],
                    Error_Torque = loadCellTest.Torque.ErrorBuffer[i],
                    FSError_Torque = loadCellTest.Torque.FSErrorBuffer[i]
                });
            }
        }
        public void UpdateLoadCellTestFromData(LoadCellTest loadCellTest)
        {
            // Mevcut buffer'ları temizliyoruz
            loadCellTest.Thrust.Buffer.Clear();
            loadCellTest.Thrust.AppliedBuffer.Clear();
            loadCellTest.Thrust.ErrorBuffer.Clear();
            loadCellTest.Thrust.FSErrorBuffer.Clear();

            loadCellTest.Torque.Buffer.Clear();
            loadCellTest.Torque.AppliedBuffer.Clear();
            loadCellTest.Torque.ErrorBuffer.Clear();
            loadCellTest.Torque.FSErrorBuffer.Clear();

            // LoadCellTestData koleksiyonundaki verileri ilgili buffer'lara ekliyoruz
            foreach (var data in LoadCellTestData)
            {
                loadCellTest.Thrust.AppliedBuffer.Add(data.Applied_Thrust);
                loadCellTest.Thrust.Buffer.Add(data.Calculated_Thrust);
                loadCellTest.Thrust.ErrorBuffer.Add(data.Error_Thrust);
                loadCellTest.Thrust.FSErrorBuffer.Add(data.FSError_Thrust);

                loadCellTest.Torque.AppliedBuffer.Add(data.Applied_Torque);
                loadCellTest.Torque.Buffer.Add(data.Calculated_Torque);
                loadCellTest.Torque.ErrorBuffer.Add(data.Error_Torque);
                loadCellTest.Torque.FSErrorBuffer.Add(data.FSError_Torque);
            }
        }

        public void UpdateCurrentDataGrid(Current current)
        {
            CurrentData.Clear();
            for (int i = 0; i < current.calibration.PointRawBuffer.Count; i++)
            {
                CurrentData.Add(new CurrentDataGrid
                {
                    No = i + 1,
                    ADC_Current = current.calibration.PointRawBuffer[i],
                    Applied_Current = current.calibration.PointAppliedBuffer[i]
                });
            }
        }
        public void UpdateCurrentFromData(Current current)
        {
            // Mevcut buffer'ları temizliyoruz
            current.calibration.PointRawBuffer.Clear();
            current.calibration.PointAppliedBuffer.Clear();

            // CurrentData koleksiyonundaki verileri ilgili buffer'lara ekliyoruz
            foreach (var data in CurrentData)
            {
                current.calibration.PointRawBuffer.Add(data.ADC_Current);
                current.calibration.PointAppliedBuffer.Add(data.Applied_Current);
            }
        }

        public void UpdateVoltageDataGrid(Voltage voltage)
        {
            VoltageData.Clear();
            for (int i = 0; i < voltage.calibration.PointRawBuffer.Count; i++)
            {
                VoltageData.Add(new VoltageDataGrid
                {
                    No = i + 1,
                    ADC_Voltage = voltage.calibration.PointRawBuffer[i],
                    Applied_Voltage = voltage.calibration.PointAppliedBuffer[i]
                });
            }
        }
        public void UpdateVoltageFromData(Voltage voltage)
        {
            // Mevcut buffer'ları temizliyoruz
            voltage.calibration.PointRawBuffer.Clear();
            voltage.calibration.PointAppliedBuffer.Clear();

            // VoltageData koleksiyonundaki verileri ilgili buffer'lara ekliyoruz
            foreach (var data in VoltageData)
            {
                voltage.calibration.PointRawBuffer.Add(data.ADC_Voltage);
                voltage.calibration.PointAppliedBuffer.Add(data.Applied_Voltage);
            }
        }


    }
    public enum Mode
    {
        Thrust,
        Torque,
        LoadCellTest,
        Current,
        Voltage
    }
    public class ThrustDataGrid 
    {
        public int No { get; set; } // Verinin sıra numarası
        public double Applied_Thrust { get; set; } // Uygulanan itki (gr)
        public double ADC_Thrust { get; set; } // Okunan itki (ADC)
        public double ADC_Torque { get; set; } // Okunan tork (ADC)
    }
    public class TorqueDataGrid 
    {
        public int No { get; set; } // Verinin sıra numarası
        public double Applied_Torque { get; set; } // Uygulanan tork (Nmm)
        public double ADC_Torque { get; set; } // Okunan tork (ADC)
        public double ADC_Thrust { get; set; } // Okunan itki (ADC)
    }
    public class LoadCellTestDataGrid 
    {
        public int No { get; set; } // Verinin sıra numarası
        public double Applied_Thrust { get; set; } // Uygulanan itki (gr)
        public double Calculated_Thrust { get; set; } // Hesaplanan itki (gr)
        public double Error_Thrust { get; set; } // Hata (%)
        public double FSError_Thrust { get; set; } // FS Hata (%)
        public double Applied_Torque { get; set; } // Uygulanan tork (Nmm)
        public double Calculated_Torque { get; set; } // Hesaplanan tork (Nmm)
        public double Error_Torque { get; set; } // Hata (%)
        public double FSError_Torque { get; set; } // FS Hata (%)
    }
    public class CurrentDataGrid 
    {
        public int No { get; set; } // Verinin sıra numarası
        public double Applied_Current { get; set; } // Uygulanan akım (mA)
        public double ADC_Current { get; set; } // Okunan akım (ADC)
    }
    public class VoltageDataGrid
    {
        public int No { get; set; } // Verinin sıra numarası
        public double Applied_Voltage { get; set; } // Uygulanan voltaj (V)
        public double ADC_Voltage { get; set; } // Okunan voltaj (ADC)
    }



}
