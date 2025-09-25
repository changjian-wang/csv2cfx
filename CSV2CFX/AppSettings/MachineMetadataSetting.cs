using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class MachineMetadataSetting
    {
        public string Building { get; set; } = "";

        public string Device { get; set; } = "";

        public string AreaName { get; set; } = "";

        public string Organization { get; set; } = "";

        public string LineName { get; set; } = "";

        public string SiteName { get; set; } = "";

        public string StationName { get; set; } = "";

        public string ProcessType { get; set; } = "";

        public string MachineName { get; set; } = "";

        public string CreatedBy { get; set; } = "";
    }
}
