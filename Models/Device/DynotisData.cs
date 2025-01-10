using Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors;
using Dynotis_Calibration_and_Signal_Analyzer.Models.Serial;
using Dynotis_Calibration_and_Signal_Analyzer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Device
{
    public class DynotisData : BindableBase
    {
        private DataReadPort _portRead;
        public DataReadPort PortRead
        {
            get => _portRead;
            set => SetProperty(ref _portRead, value);
        }

        public Thrust Thrust { get; set; }
        public Torque Torque { get; set; }
        public Current Current { get; set; }
        public Voltage Voltage { get; set; }

        public DynotisData()
        {
            PortRead = new DataReadPort();
            Thrust = new Thrust();
            Torque = new Torque();
            Current = new Current();
            Voltage = new Voltage();
        }

        public class DataReadPort : BindableBase
        {
            private string _data;
            private double _time;
            public string Data
            {
                get => _data;
                set => SetProperty(ref _data, value);
            }
            public double Time
            {
                get => _time;
                set => SetProperty(ref _time, value);
            }
        }
    }
}
