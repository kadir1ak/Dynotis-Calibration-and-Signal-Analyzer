using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dynotis_Calibration_and_Signal_Analyzer.Services;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors
{
    public class Current : BindableBase
    {
        private Raw _raw;
        private Calibration _calibration;
        private Calculated _calculated;

        public Raw raw
        {
            get => _raw;
            set => SetProperty(ref _raw, value);
        }

        public Calibration calibration
        {
            get => _calibration;
            set => SetProperty(ref _calibration, value);
        }

        public Calculated calculated
        {
            get => _calculated;
            set => SetProperty(ref _calculated, value);
        }
        public Current()
        {
            raw = new Raw();
            calibration = new Calibration();
            calculated = new Calculated();
        }
        public class Calculated : BindableBase
        {
            private double _value;
            private string _unitName;
            private string _unitSymbol;
            private double _noiseValue;
            private double _rawValue;

            public double Value
            {
                get => _value;
                set => SetProperty(ref _value, value);
            }

            public string UnitName
            {
                get => _unitName;
                set => SetProperty(ref _unitName, value);
            }

            public string UnitSymbol
            {
                get => _unitSymbol;
                set => SetProperty(ref _unitSymbol, value);
            }

            public double NoiseValue
            {
                get => _noiseValue;
                set => SetProperty(ref _noiseValue, value);
            }
            public double RawValue
            {
                get => _rawValue;
                set => SetProperty(ref _rawValue, value);
            }
        }
        public class Calibration : BindableBase
        {
            private List<double> _pointAppliedBuffer = new List<double>();
            private List<double> _pointRawBuffer = new List<double>();
            private string _appliedUnit;
            private double _applied;
            private bool _addingOn;
            private Coefficients _coefficients = new Coefficients();

            public List<double> PointRawBuffer
            {
                get => _pointRawBuffer;
                set => SetProperty(ref _pointRawBuffer, value);
            }
            public List<double> PointAppliedBuffer
            {
                get => _pointAppliedBuffer;
                set => SetProperty(ref _pointAppliedBuffer, value);
            }
            public double Applied
            {
                get => _applied;
                set => SetProperty(ref _applied, value);
            }
            public string AppliedUnit
            {
                get => _appliedUnit;
                set => SetProperty(ref _appliedUnit, value);
            }
            public bool AddingOn
            {
                get => _addingOn;
                set => SetProperty(ref _addingOn, value);
            }
            public Coefficients Coefficient
            {
                get => _coefficients;
                set => SetProperty(ref _coefficients, value);
            }
        }
        public class Raw : BindableBase
        {
            private double _value;
            private double _noiseValue;

            private List<double> _buffer = new();
            public double Value
            {
                get => _value;
                set => SetProperty(ref _value, value);
            }
            public double NoiseValue
            {
                get => _noiseValue;
                set => SetProperty(ref _noiseValue, value);
            }
            public List<double> Buffer
            {
                get => _buffer;
                set => SetProperty(ref _buffer, value);
            }
        }
    }
}
