using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class BackgroundTaskSetting
    {
        public int MaxConcurrency { get; set; }

        public int DelayBetweenBatchesMs { get; set; }
    }
}
