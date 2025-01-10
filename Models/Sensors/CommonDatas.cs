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
        private List<int> _rawBuffer = new();
        private List<int> _errorRawBuffer = new();
        private int _adc;
        private int _errorADC;
        private int _noise;
        private int _errorNoise;
        private int _count;

        public List<int> RawBuffer
        {
            get => _rawBuffer;
            set => SetProperty(ref _rawBuffer, value);
        }
        public List<int> ErrorRawBuffer
        {
            get => _errorRawBuffer;
            set => SetProperty(ref _errorRawBuffer, value);
        }

        public int ADC
        {
            get => _adc;
            set => SetProperty(ref _adc, value);
        }
        public int ErrorADC
        {
            get => _errorADC;
            set => SetProperty(ref _errorADC, value);
        }
        public int Noise
        {
            get => _noise;
            set => SetProperty(ref _noise, value);
        }
        public int ErrorNoise
        {
            get => _errorNoise;
            set => SetProperty(ref _errorNoise, value);
        }
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }

    // Kalibrasyon ölçümleri
    public class CalibrationMeasurements : BindableBase
    {
        private List<int> _pointRawBuffer = new();
        private List<int> _errorPointRawBuffer = new();
        private double _applied = 0;
        private Coefficients _coefficients;
        private Coefficients _errorCoefficients;
        private int _count;

        public List<int> PointRawBuffer
        {
            get => _pointRawBuffer;
            set => SetProperty(ref _pointRawBuffer, value);
        }
        public List<int> ErrorPointRawBuffer
        {
            get => _errorPointRawBuffer;
            set => SetProperty(ref _errorPointRawBuffer, value);
        }
        public double Applied
        {
            get => _applied;
            set => SetProperty(ref _applied, value);
        }
        public Coefficients Coefficient
        {
            get => _coefficients;
            set => SetProperty(ref _coefficients, value);
        }
        public Coefficients ErrorCoefficients
        {
            get => _errorCoefficients;
            set => SetProperty(ref _errorCoefficients, value);
        }
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }

    // Katsayılar
    public class Coefficients : BindableBase
    {
        private double _a;
        private double _b;
        private double _c;
        private double _errorA;
        private double _errorB;
        private double _errorC;

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
    }
}
