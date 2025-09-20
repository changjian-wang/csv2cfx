using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class LoggingSetting
    {
        public LogLevelSetting LogLevel { get; set; } = new LogLevelSetting();
    }

    public class LogLevelSetting
    {
        public string Default { get; set; } = "Information";
        public string MicrosoftHostingLifetime { get; set; } = "Information";
    }
}
