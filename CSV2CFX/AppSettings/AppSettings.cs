using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class AppSettings
    {
        public LoggingSetting Logging { get; set; } = new LoggingSetting();
     
        public RabbitmqSetting RabbitMQ { get; set; } = new RabbitmqSetting();
        
        public RabbitMQPublisherSettings RabbitMQPublisherSettings { get; set; } = new RabbitMQPublisherSettings();
        
        public ApiSetting Api { get; set; } = new ApiSetting();
        
        public BackgroundTaskSetting BackgroundTask { get; set; } = new BackgroundTaskSetting();
        
        public CsvFilePathSetting CsvFilePath { get; set; } = new CsvFilePathSetting();
        
        public MachineInfoSetting MachineInfo { get; set; } = new MachineInfoSetting();
        
        public MachineMetadataSetting MachineMetadata { get; set; } = new MachineMetadataSetting();
    }
}
