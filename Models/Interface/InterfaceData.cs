using Dynotis_Calibration_and_Signal_Analyzer.Models.Device;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Current Current { get; set; }
        public Voltage Voltage { get; set; }
        public ObservableCollection<DataGridRowModel> ThrustData { get; set; }
        public ObservableCollection<DataGridRowModel> TorqueData { get; set; }
        public ObservableCollection<DataGridRowModel> LoadCellData { get; set; }
        public ObservableCollection<DataGridRowModel> CurrentData { get; set; }
        public ObservableCollection<DataGridRowModel> VoltageData { get; set; }
        public InterfaceData()
        {
            Thrust = new Thrust();
            Torque = new Torque();
            Current = new Current();
            Voltage = new Voltage();

            ThrustData = new ObservableCollection<DataGridRowModel>();
            TorqueData = new ObservableCollection<DataGridRowModel>();
            LoadCellData = new ObservableCollection<DataGridRowModel>();
            CurrentData = new ObservableCollection<DataGridRowModel>();
            VoltageData = new ObservableCollection<DataGridRowModel>();
        }
        public void UpdateThrustData(Thrust thrust)
        {
            ThrustData.Clear();
            for (int i = 0; i < thrust.Calibration.PointRawBuffer.Count; i++)
            {
                ThrustData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Thrust = thrust.Calibration.PointRawBuffer[i],
                    Applied_Thrust = thrust.Calibration.PointAppliedBuffer[i],
                    ADC_Torque = thrust.Calibration.PointErrorBuffer[i]
                });
            }
        }

        public void UpdateTorqueData(Torque torque)
        {
            TorqueData.Clear();
            for (int i = 0; i < torque.Calibration.PointRawBuffer.Count; i++)
            {
                TorqueData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Torque = torque.Calibration.PointRawBuffer[i],
                    Applied_Torque = torque.Calibration.PointAppliedBuffer[i],
                    ADC_Thrust = torque.Calibration.PointErrorBuffer[i]
                });
            }
        }

        public void UpdateLoadCellData(Thrust thrust, Torque torque)
        {
            LoadCellData.Clear();
            for (int i = 0; i < thrust.Calibration.PointRawBuffer.Count; i++)
            {
                LoadCellData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Thrust = thrust.Calibration.PointRawBuffer[i],
                    Applied_Thrust = thrust.Calibration.PointAppliedBuffer[i],
                    ADC_Torque = torque.Calibration.PointRawBuffer[i],
                    Applied_Torque = torque.Calibration.PointAppliedBuffer[i]
                });
            }
        }

        public void UpdateCurrentData(Current current)
        {
            CurrentData.Clear();
            for (int i = 0; i < current.Calibration.PointRawBuffer.Count; i++)
            {
                CurrentData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Current = current.Calibration.PointRawBuffer[i],
                    Applied_Current = current.Calibration.PointAppliedBuffer[i]
                });
            }
        }

        public void UpdateVoltageData(Voltage voltage)
        {
            VoltageData.Clear();
            for (int i = 0; i < voltage.Calibration.PointRawBuffer.Count; i++)
            {
                VoltageData.Add(new DataGridRowModel
                {
                    No = i + 1,
                    ADC_Voltage = voltage.Calibration.PointRawBuffer[i],
                    Applied_Voltage = voltage.Calibration.PointAppliedBuffer[i]
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
