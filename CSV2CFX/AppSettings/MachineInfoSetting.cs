using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class MachineInfoSetting
    {
        public string Heartbeat { get; set; } = "";
        public string WorkStarted { get; set; } = "";
        public string WorkCompleted { get; set; } = "";
        public string UnitsProcessed { get; set; } = "";
        public string StationStateChanged { get; set; } = "";
        public string FaultOccurred { get; set; } = "";
        public string FaultCleared { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public string Version { get; set; } = "";
        public int HeartbeatFrequency { get; set; } = 5;
    }
}
