using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.AppSettings
{
    public class MqttSetting
    {
        public string? BrokerHost { get; set; } = "localhost";
        public int BrokerPort { get; set; } = 1883;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public bool UseTls { get; set; } = false;
        public int KeepAlivePeriod { get; set; } = 60;
        public bool CleanSession { get; set; } = true;
        public int ConnectionTimeout { get; set; } = 30;
        public string? TopicPrefix { get; set; } = "cfx";
    }
}