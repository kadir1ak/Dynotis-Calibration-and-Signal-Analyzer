using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        private bool _buttonIsEnabled;
        public bool ButtonIsEnabled
        {
            get => _buttonIsEnabled;
            set => SetProperty(ref _buttonIsEnabled, value);
        }

        private bool _textboxIsEnabled;
        public bool TextboxIsEnabled
        {
            get => _textboxIsEnabled;
            set => SetProperty(ref _textboxIsEnabled, value);
        }

        private bool _checkboxIsEnabled;
        public bool CheckboxIsEnabled
        {
            get => _checkboxIsEnabled;
            set => SetProperty(ref _checkboxIsEnabled, value);
        }     

        private bool _thrustUnitsIsEnabled;
        public bool ThrustUnitsIsEnabled
        {
            get => _thrustUnitsIsEnabled;
            set => SetProperty(ref _thrustUnitsIsEnabled, value);
        }      
        
        private bool _torqueUnitsIsEnabled;
        public bool TorqueUnitsIsEnabled
        {
            get => _torqueUnitsIsEnabled;
            set => SetProperty(ref _torqueUnitsIsEnabled, value);
        }

        private bool _currentUnitsIsEnabled;
        public bool CurrentUnitsIsEnabled
        {
            get => _currentUnitsIsEnabled;
            set => SetProperty(ref _currentUnitsIsEnabled, value);
        }

        private bool _voltageUnitsIsEnabled;
        public bool VoltageUnitsIsEnabled
        {
            get => _voltageUnitsIsEnabled;
            set => SetProperty(ref _voltageUnitsIsEnabled, value);
        }

        private Visibility _appliedDistanceVisibility;
        public Visibility AppliedDistanceVisibility
        {
            get => _appliedDistanceVisibility;
            set => SetProperty(ref _appliedDistanceVisibility, value);
        }

        private string _applied_ThrustColumn;
        public string Applied_ThrustColumn
        {
            get => _applied_ThrustColumn;
            set => SetProperty(ref _applied_ThrustColumn, value);
        }

        private string _applied_TorqueColumn;
        public string Applied_TorqueColumn
        {
            get => _applied_TorqueColumn;
            set => SetProperty(ref _applied_TorqueColumn, value);
        }

        private string _calculated_ThrustColumn;
        public string Calculated_ThrustColumn
        {
            get => _calculated_ThrustColumn;
            set => SetProperty(ref _calculated_ThrustColumn, value);
        }

        private string _calculated_TorqueColumn;
        public string Calculated_TorqueColumn
        {
            get => _calculated_TorqueColumn;
            set => SetProperty(ref _calculated_TorqueColumn, value);
        }

        private string _applied_CurrentColumn;
        public string Applied_CurrentColumn
        {
            get => _applied_CurrentColumn;
            set => SetProperty(ref _applied_CurrentColumn, value);
        }

        private string _applied_VoltageColumn;
        public string Applied_VoltageColumn
        {
            get => _applied_VoltageColumn;
            set => SetProperty(ref _applied_VoltageColumn, value);
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
        private Thrust _thrust;
        public Thrust Thrust
        {
            get => _thrust;
            set => SetProperty(ref _thrust, value);
        }

        private Torque _torque;
        public Torque Torque
        {
            get => _torque;
            set => SetProperty(ref _torque, value);
        }

        private LoadCellTest _loadCellTest;
        public LoadCellTest LoadCellTest
        {
            get => _loadCellTest;
            set => SetProperty(ref _loadCellTest, value);
        }

        private Current _current;
        public Current Current
        {
            get => _current;
            set => SetProperty(ref _current, value);
        }

        private Voltage _voltage;
        public Voltage Voltage
        {
            get => _voltage;
            set => SetProperty(ref _voltage, value);
        }

        private ObservableCollection<ThrustDataGrid> _thrustData;
        public ObservableCollection<ThrustDataGrid> ThrustData
        {
            get => _thrustData;
            set => SetProperty(ref _thrustData, value);
        }

        private ObservableCollection<TorqueDataGrid> _torqueData;
        public ObservableCollection<TorqueDataGrid> TorqueData
        {
            get => _torqueData;
            set => SetProperty(ref _torqueData, value);
        }

        private ObservableCollection<LoadCellTestDataGrid> _loadCellTestData;
        public ObservableCollection<LoadCellTestDataGrid> LoadCellTestData
        {
            get => _loadCellTestData;
            set => SetProperty(ref _loadCellTestData, value);
        }

        private ObservableCollection<CurrentDataGrid> _currentData;
        public ObservableCollection<CurrentDataGrid> CurrentData
        {
            get => _currentData;
            set => SetProperty(ref _currentData, value);
        }

        private ObservableCollection<VoltageDataGrid> _voltageData;
        public ObservableCollection<VoltageDataGrid> VoltageData
        {
            get => _voltageData;
            set => SetProperty(ref _voltageData, value);
        }

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
                    AppliedDirection_Thrust = thrust.calibration.PointDirectionBuffer[i],
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
                    AppliedDirection_Torque = torque.calibration.PointDirectionBuffer[i],
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
    public class ThrustDataGrid : BindableBase
    {
        private int _no;
        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        private string _appliedDirection_Thrust;
        public string AppliedDirection_Thrust
        {
            get => _appliedDirection_Thrust;
            set => SetProperty(ref _appliedDirection_Thrust, value);
        }

        private double _applied_Thrust;
        public double Applied_Thrust
        {
            get => _applied_Thrust;
            set => SetProperty(ref _applied_Thrust, value);
        }

        private double _adc_Thrust;
        public double ADC_Thrust
        {
            get => _adc_Thrust;
            set => SetProperty(ref _adc_Thrust, value);
        }

        private double _adc_Torque;
        public double ADC_Torque
        {
            get => _adc_Torque;
            set => SetProperty(ref _adc_Torque, value);
        }
    }

    public class TorqueDataGrid : BindableBase
    {
        private int _no;
        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        private string _appliedDirection_Torque;
        public string AppliedDirection_Torque
        {
            get => _appliedDirection_Torque;
            set => SetProperty(ref _appliedDirection_Torque, value);
        }

        private double _applied_Torque;
        public double Applied_Torque
        {
            get => _applied_Torque;
            set => SetProperty(ref _applied_Torque, value);
        }

        private double _adc_Torque;
        public double ADC_Torque
        {
            get => _adc_Torque;
            set => SetProperty(ref _adc_Torque, value);
        }

        private double _adc_Thrust;
        public double ADC_Thrust
        {
            get => _adc_Thrust;
            set => SetProperty(ref _adc_Thrust, value);
        }
    }

    public class LoadCellTestDataGrid : BindableBase
    {
        private int _no;
        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        private string _appliedDirection_Thrust;
        public string AppliedDirection_Thrust
        {
            get => _appliedDirection_Thrust;
            set => SetProperty(ref _appliedDirection_Thrust, value);
        }

        private double _applied_Thrust;
        public double Applied_Thrust
        {
            get => _applied_Thrust;
            set => SetProperty(ref _applied_Thrust, value);
        }

        private double _calculated_Thrust;
        public double Calculated_Thrust
        {
            get => _calculated_Thrust;
            set => SetProperty(ref _calculated_Thrust, value);
        }

        private double _error_Thrust;
        public double Error_Thrust
        {
            get => _error_Thrust;
            set => SetProperty(ref _error_Thrust, value);
        }

        private double _fsError_Thrust;
        public double FSError_Thrust
        {
            get => _fsError_Thrust;
            set => SetProperty(ref _fsError_Thrust, value);
        }

        private string _appliedDirection_Torque;
        public string AppliedDirection_Torque
        {
            get => _appliedDirection_Torque;
            set => SetProperty(ref _appliedDirection_Torque, value);
        }

        private double _applied_Torque;
        public double Applied_Torque
        {
            get => _applied_Torque;
            set => SetProperty(ref _applied_Torque, value);
        }

        private double _calculated_Torque;
        public double Calculated_Torque
        {
            get => _calculated_Torque;
            set => SetProperty(ref _calculated_Torque, value);
        }

        private double _error_Torque;
        public double Error_Torque
        {
            get => _error_Torque;
            set => SetProperty(ref _error_Torque, value);
        }

        private double _fsError_Torque;
        public double FSError_Torque
        {
            get => _fsError_Torque;
            set => SetProperty(ref _fsError_Torque, value);
        }
    }
    public class CurrentDataGrid : BindableBase
    {
        private int _no;
        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        private double _applied_Current;
        public double Applied_Current
        {
            get => _applied_Current;
            set => SetProperty(ref _applied_Current, value);
        }

        private double _adc_Current;
        public double ADC_Current
        {
            get => _adc_Current;
            set => SetProperty(ref _adc_Current, value);
        }
    }

    public class VoltageDataGrid : BindableBase
    {
        private int _no;
        public int No
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }

        private double _applied_Voltage;
        public double Applied_Voltage
        {
            get => _applied_Voltage;
            set => SetProperty(ref _applied_Voltage, value);
        }

        private double _adc_Voltage;
        public double ADC_Voltage
        {
            get => _adc_Voltage;
            set => SetProperty(ref _adc_Voltage, value);
        }
    }




}
