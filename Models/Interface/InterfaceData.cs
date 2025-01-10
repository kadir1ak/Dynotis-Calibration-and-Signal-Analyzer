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
        private DynotisData _data;
        public DynotisData data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        public InterfaceData()
        {
            data = new DynotisData();
        }
    }
}
