using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class CsvFilePathSetting
    {
        public string? ProductionInformationFilePath { get; set; }

        public string? MachineStatusInformationFilePath { get; set; }

        public string? ProcessDataFilesFilePath { get; set; }
    }
}
