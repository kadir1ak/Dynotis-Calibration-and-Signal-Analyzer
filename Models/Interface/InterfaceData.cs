using Dynotis_Calibration_and_Signal_Analyzer.Models.Device;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Services;
using System;
using System.Collections.Generic;
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

        public Thrust Thrust { get; set; }
        public Torque Torque { get; set; }
        public Current Current { get; set; }
        public Voltage Voltage { get; set; }

        public InterfaceData()
        {
            Thrust = new Thrust();
            Torque = new Torque();
            Current = new Current();
            Voltage = new Voltage();
        }
    }
}
