using Dynotis_Calibration_and_Signal_Analyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors
{
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
