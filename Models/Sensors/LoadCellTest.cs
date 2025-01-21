using Dynotis_Calibration_and_Signal_Analyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors
{
    public class LoadCellTest : BindableBase
    {
        private Calculated _thrust;
        public Calculated Thrust
        {
            get => _thrust;
            set => SetProperty(ref _thrust, value);
        }

        private Calculated _torque;
        public Calculated Torque
        {
            get => _torque;
            set => SetProperty(ref _torque, value);
        }

        public LoadCellTest()
        {
            Thrust = new Calculated();
            Torque = new Calculated();
        }

        public class Calculated : BindableBase
        {
            private List<double> _buffer = new List<double>();
            private List<string> _appliedDirectionBuffer = new List<string>();
            private List<double> _appliedBuffer = new List<double>();
            private List<double> _errorBuffer = new List<double>();
            private List<double> _fsErrorBuffer = new List<double>();

            public List<double> Buffer
            {
                get => _buffer;
                set => SetProperty(ref _buffer, value);
            }
            public List<string> AppliedDirectionBuffer
            {
                get => _appliedDirectionBuffer;
                set => SetProperty(ref _appliedDirectionBuffer, value);
            }
            public List<double> AppliedBuffer
            {
                get => _appliedBuffer;
                set => SetProperty(ref _appliedBuffer, value);
            }
            public List<double> ErrorBuffer
            {
                get => _errorBuffer;
                set => SetProperty(ref _errorBuffer, value);
            }
            public List<double> FSErrorBuffer
            {
                get => _fsErrorBuffer;
                set => SetProperty(ref _fsErrorBuffer, value);
            }
        }
    }
}
