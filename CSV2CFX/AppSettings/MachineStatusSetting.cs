using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class MachineStatusSetting
    {
        public string? Heartbeat { get; set; }

        public string? WorkStarted { get; set; }

        public string? WorkCompleted { get; set; }

        public string? UnitsProcessed { get; set; }

        public string? StationStateChanged { get; set; }

        public string? FaultOccurred { get; set; }

        public string? FaultCleared { get; set; }

        public string? UniqueId { get; set; }

        public string? Version { get; set; }

        public int HeartbeatFrequency { get; set; }

        public string? Device { get; set; }

        public string? Building { get; set; }

        public string? SiteName { get; set; }

        public string? StationName { get; set; }

        public string? CreatedBy { get; set; }

        public string? ProcessType { get; set; }
    }
}
