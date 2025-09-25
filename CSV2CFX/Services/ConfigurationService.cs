using CSV2CFX.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CSV2CFX.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IOptionsMonitor<AppSettings.AppSettings> _options;
        private readonly string _configFilePath;

        public ConfigurationService(IOptionsMonitor<AppSettings.AppSettings> options)
        {
            _options = options;
            _configFilePath = "appsettings.json";
        }

        public AppSettings.AppSettings GetConfiguration()
        {
            // 创建一个新的实例，避免直接修改原始配置
            var config = _options.CurrentValue;
            return new AppSettings.AppSettings
            {
                Logging = config.Logging,
                RabbitMQ = config.RabbitMQ,
                RabbitMQPublisherSettings = config.RabbitMQPublisherSettings,
                Api = config.Api,
                BackgroundTask = config.BackgroundTask,
                CsvFilePath = config.CsvFilePath,
                MachineInfo = config.MachineInfo,
                MachineMetadata = config.MachineMetadata
            };
        }

        public async Task SaveConfiguration(AppSettings.AppSettings config)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(new
            {
                Logging = config.Logging,
                RabbitMQ = config.RabbitMQ,
                RabbitMQPublisherSettings = config.RabbitMQPublisherSettings,
                Api = config.Api,
                BackgroundTask = config.BackgroundTask,
                CsvFilePath = config.CsvFilePath,
                MachineInfo = config.MachineInfo,
                MachineMetadata = config.MachineMetadata
            }, options);

            await File.WriteAllTextAsync(_configFilePath, json);
        }
    }
}
