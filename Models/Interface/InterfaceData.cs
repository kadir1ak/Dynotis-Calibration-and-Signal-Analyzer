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
        public ObservableCollection<DataGridRowModel> ThrustData { get; set; }
        public ObservableCollection<DataGridRowModel> TorqueData { get; set; }
        public ObservableCollection<DataGridRowModel> LoadCellTestData { get; set; }
        public ObservableCollection<DataGridRowModel> CurrentData { get; set; }
        public ObservableCollection<DataGridRowModel> VoltageData { get; set; }
        public InterfaceData()
        {
            Thrust = new Thrust();
            Torque = new Torque();
            LoadCellTest = new LoadCellTest();
            Current = new Current();
            Voltage = new Voltage();

            ThrustData = new ObservableCollection<DataGridRowModel>();
            TorqueData = new ObservableCollection<DataGridRowModel>();
            LoadCellTestData = new ObservableCollection<DataGridRowModel>();
            CurrentData = new ObservableCollection<DataGridRowModel>();
            VoltageData = new ObservableCollection<DataGridRowModel>();
        }
        public void UpdateThrustData(Thrust thrust)
        {
            ThrustData.Clear();
            for (int i = 0; i < thrust.calibration.PointRawBuffer.Count; i++)
            {
                ThrustData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Thrust = thrust.calibration.PointRawBuffer[i],
                    Applied_Thrust = thrust.calibration.PointAppliedBuffer[i],
                    ADC_Torque = thrust.calibration.PointErrorBuffer[i]
                });
            }
        }

        public void UpdateTorqueData(Torque torque)
        {
            TorqueData.Clear();
            for (int i = 0; i < torque.calibration.PointRawBuffer.Count; i++)
            {
                TorqueData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Torque = torque.calibration.PointRawBuffer[i],
                    Applied_Torque = torque.calibration.PointAppliedBuffer[i],
                    ADC_Thrust = torque.calibration.PointErrorBuffer[i]
                });
            }
        }

        public void UpdateLoadCellTestData(LoadCellTest loadCellTest, Thrust thrust, Torque torque)
        {
            LoadCellTestData.Clear();
            for (int i = 0; i < thrust.calibration.PointRawBuffer.Count; i++)
            {
                LoadCellTestData.Add(new DataGridRowModel
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

        public void UpdateCurrentData(Current current)
        {
            CurrentData.Clear();
            for (int i = 0; i < current.calibration.PointRawBuffer.Count; i++)
            {
                CurrentData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Current = current.calibration.PointRawBuffer[i],
                    Applied_Current = current.calibration.PointAppliedBuffer[i]
                });
            }
        }

        public void UpdateVoltageData(Voltage voltage)
        {
            VoltageData.Clear();
            for (int i = 0; i < voltage.calibration.PointRawBuffer.Count; i++)
            {
                VoltageData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Voltage = voltage.calibration.PointRawBuffer[i],
                    Applied_Voltage = voltage.calibration.PointAppliedBuffer[i]
                });
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
    public class DataGridRowModel
    {
        public int No { get; set; }
        public double ADC_Thrust { get; set; }
        public double Applied_Thrust { get; set; }
        public double Calculated_Thrust { get; set; }
        public double Error_Thrust { get; set; }
        public double FSError_Thrust { get; set; }
        public double ADC_Torque { get; set; }
        public double Applied_Torque { get; set; }
        public double Calculated_Torque { get; set; }
        public double Error_Torque { get; set; }
        public double FSError_Torque { get; set; }
        public double ADC_Current { get; set; }
        public double Applied_Current { get; set; }
        public double Calculated_Current { get; set; }
        public double Error_Current { get; set; }
        public double ADC_Voltage { get; set; }
        public double Applied_Voltage { get; set; }
        public double Calculated_Voltage { get; set; }
        public double Error_Voltage { get; set; }
    }

}
