using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Models
{
    public class ProductionInfo
    {
        public string? ProductModel { get; set; }

        public string? SN { get; set; }

        public string? PartNum { get; set; }

        public string? CT { get; set; }

        public string? Result { get; set; }

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }
    }
}
