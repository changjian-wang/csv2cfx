using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Models
{
    public class MachineStatusInfo
    {
        public string? OPTime { get; set; }

        public int? Status { get; set; }

        public int? ErrorID { get; set; }

        public string? ErrorMsg { get; set; }
    }
}
