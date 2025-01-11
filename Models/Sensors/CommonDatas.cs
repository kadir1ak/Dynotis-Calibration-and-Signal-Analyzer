using Dynotis_Calibration_and_Signal_Analyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors
{
    public class CommonDatas : BindableBase
    {

    }
    // Hesaplanmış ölçümler
    public class CalculatedMeasurements : BindableBase
    {
        private double _value;
        private string _unitName;
        private string _unitSymbol;
        private double _noise;
        private double _dara;

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

        public double Noise
        {
            get => _noise;
            set => SetProperty(ref _noise, value);
        }
        public double Dara
        {
            get => _dara;
            set => SetProperty(ref _dara, value);
        }
        public CalculatedMeasurements() { }
        public CalculatedMeasurements(double value, string unitName, string unitSymbol)
        {
            this.Value = value;
            this.UnitName = unitName;
            this.UnitSymbol = unitSymbol;
        }
    }

    // Ham ölçümler
    public class RawMeasurements : BindableBase
    {
        private List<double> _rawBuffer = new();
        private List<double> _errorRawBuffer = new();
        private double _adc;
        private double _errorADC;
        private double _noise;
        private double _errorNoise;

        public List<double> RawBuffer
        {
            get => _rawBuffer;
            set => SetProperty(ref _rawBuffer, value);
        }
        public List<double> ErrorRawBuffer
        {
            get => _errorRawBuffer;
            set => SetProperty(ref _errorRawBuffer, value);
        }

        public double ADC
        {
            get => _adc;
            set => SetProperty(ref _adc, value);
        }
        public double ErrorADC
        {
            get => _errorADC;
            set => SetProperty(ref _errorADC, value);
        }
        public double Noise
        {
            get => _noise;
            set => SetProperty(ref _noise, value);
        }
        public double ErrorNoise
        {
            get => _errorNoise;
            set => SetProperty(ref _errorNoise, value);
        }
    }

    // Kalibrasyon ölçümleri
    public class CalibrationMeasurements : BindableBase
    {
        private List<double> _pointRawBuffer = new List<double>();
        private List<double> _pointAppliedBuffer = new List<double>();
        private List<double> _pointErrorBuffer = new List<double>();
        private double _applied = 0;
        private double _appliedDistance = 0;
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
        public List<double> PointErrorBuffer
        {
            get => _pointErrorBuffer;
            set => SetProperty(ref _pointErrorBuffer, value);
        }
        public double Applied
        {
            get => _applied;
            set => SetProperty(ref _applied, value);
        }    
        public double AppliedDistance
        {
            get => _appliedDistance;
            set => SetProperty(ref _appliedDistance, value);
        }
        public Coefficients Coefficient
        {
            get => _coefficients;
            set => SetProperty(ref _coefficients, value);
        }
    }

    // Katsayılar
    public class Coefficients : BindableBase
    {
        private string _equation;
        private string _errorEquation;
        private double _a;
        private double _b;
        private double _c;
        private double _d;
        private double _errorA;
        private double _errorB;
        private double _errorC;
        private double _errorD;

        public string Equation
        {
            get => _equation;
            set => SetProperty(ref _equation, value);
        }
        public string ErrorEquation
        {
            get => _errorEquation;
            set => SetProperty(ref _errorEquation, value);
        }
        public double A
        {
            get => _a;
            set => SetProperty(ref _a, value);
        }

        public double B
        {
            get => _b;
            set => SetProperty(ref _b, value);
        }

        public double C
        {
            get => _c;
            set => SetProperty(ref _c, value);
        }
        public double D
        {
            get => _d;
            set => SetProperty(ref _d, value);
        }
        public double ErrorA
        {
            get => _errorA;
            set => SetProperty(ref _errorA, value);
        }

        public double ErrorB
        {
            get => _errorB;
            set => SetProperty(ref _errorB, value);
        }

        public double ErrorC
        {
            get => _errorC;
            set => SetProperty(ref _errorC, value);
        }
        public double ErrorD
        {
            get => _errorD;
            set => SetProperty(ref _errorD, value);
        }
    }
}
