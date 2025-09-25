using CSV2CFX.Extensions;
using CSV2CFX.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace CSV2CFX.Services
{
    public class DynamicConfigurationService : IDynamicConfigurationService
    {
        private readonly IConfiguration _configuration;
        private DynamicConfigurationProvider _dynamicProvider;

        public DynamicConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;

            if (configuration is IConfigurationRoot configRoot)
            {
                _dynamicProvider = configRoot.Providers
                    .OfType<DynamicConfigurationProvider>()
                    .FirstOrDefault();
            }
        }

        public async Task UpdateConfigurationAsync<T>(T configuration, string sectionName = null)
        {
            if (_dynamicProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("Dynamic configuration provider not found");
                return;
            }

            sectionName ??= GetSectionName<T>();

            await Task.Run(() =>
            {
                _dynamicProvider.UpdateConfiguration(configuration, sectionName);
            });
        }

        public async Task SaveConfigurationToFileAsync<T>(T configuration, string filePath, string sectionName = null)
        {
            sectionName ??= GetSectionName<T>();

            var existingData = new Dictionary<string, object>();

            if (File.Exists(filePath))
            {
                var existingJson = await File.ReadAllTextAsync(filePath);
                if (!string.IsNullOrEmpty(existingJson))
                {
                    existingData = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson)
                                  ?? new Dictionary<string, object>();
                }
            }

            var jsonElement = JsonSerializer.SerializeToElement(configuration);
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
            existingData[sectionName] = configDict;

            var json = JsonSerializer.Serialize(existingData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task LoadConfigurationFromFileAsync(string filePath)
        {
            if (_dynamicProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("Dynamic configuration provider not found");
                return;
            }

            await Task.Run(() =>
            {
                _dynamicProvider.Load();
            });
        }

        private string GetSectionName<T>()
        {
            var typeName = typeof(T).Name;
            if (typeName.EndsWith("Setting"))
            {
                typeName = typeName.Substring(0, typeName.Length - 7);
            }
            if (typeName.EndsWith("Settings"))
            {
                typeName = typeName.Substring(0, typeName.Length - 8);
            }
            return typeName;
        }
    }
}