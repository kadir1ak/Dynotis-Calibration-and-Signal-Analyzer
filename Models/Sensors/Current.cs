using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dynotis_Calibration_and_Signal_Analyzer.Services;

namespace Dynotis_Calibration_and_Signal_Analyzer.Models.Sensors
{
    public class Current : BindableBase
    {
        private RawMeasurements _raw;
        private CalibrationMeasurements _calibration;
        private CalculatedMeasurements _calculated;

        public RawMeasurements Raw
        {
            get => _raw;
            set => SetProperty(ref _raw, value);
        }

        public CalibrationMeasurements Calibration
        {
            get => _calibration;
            set => SetProperty(ref _calibration, value);
        }

        public CalculatedMeasurements Calculated
        {
            get => _calculated;
            set => SetProperty(ref _calculated, value);
        }

        public Current()
        {
            Raw = new RawMeasurements();
            Calibration = new CalibrationMeasurements();
            Calculated = new CalculatedMeasurements();
        }
    }
}
