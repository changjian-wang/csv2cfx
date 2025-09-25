using CSV2CFX.AppSettings;
using CSV2CFX.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Services
{
    public class MessagingServiceFactory : IMessagingServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<ProtocolSetting> _protocolOptions;

        public MessagingServiceFactory(IServiceProvider serviceProvider, IOptionsMonitor<ProtocolSetting> protocolOptions)
        {
            _serviceProvider = serviceProvider;
            _protocolOptions = protocolOptions;
        }

        public IMessagingService CreateMessagingService()
        {
            var protocolSetting = _protocolOptions.CurrentValue;
            
            return protocolSetting.Type switch
            {
                ProtocolType.MQTT => _serviceProvider.GetRequiredService<MqttService>(),
                ProtocolType.AMQP => _serviceProvider.GetRequiredService<RabbitMQService>(),
                _ => _serviceProvider.GetRequiredService<RabbitMQService>() // Default to AMQP for backward compatibility
            };
        }
    }
}