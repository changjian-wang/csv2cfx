using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace CSV2CFX.Extensions
{
    public class DynamicConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly string _filePath;
        private FileSystemWatcher _watcher;
        private readonly object _lock = new object();

        public DynamicConfigurationProvider(string filePath)
        {
            _filePath = filePath;
        }

        public override void Load()
        {
            lock (_lock)
            {
                Data.Clear();

                if (File.Exists(_filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_filePath);
                        if (!string.IsNullOrEmpty(json))
                        {
                            var jsonDocument = JsonDocument.Parse(json);
                            LoadFromJsonElement(jsonDocument.RootElement, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                    }
                }

                SetupFileWatcher();
            }
        }

        private void LoadFromJsonElement(JsonElement element, string prefix)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                        LoadFromJsonElement(property.Value, key);
                    }
                    break;

                case JsonValueKind.Array:
                    for (int i = 0; i < element.GetArrayLength(); i++)
                    {
                        LoadFromJsonElement(element[i], $"{prefix}:{i}");
                    }
                    break;

                default:
                    Data[prefix] = element.ToString();
                    break;
            }
        }

        private void SetupFileWatcher()
        {
            if (_watcher != null)
                return;

            var directory = Path.GetDirectoryName(_filePath);
            var fileName = Path.GetFileName(_filePath);

            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                _watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnFileChanged;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Task.Delay(500).ContinueWith(_ =>
            {
                lock (_lock)
                {
                    Load();
                    OnReload();
                }
            });
        }

        public void UpdateConfiguration(object configuration, string sectionName)
        {
            lock (_lock)
            {
                try
                {
                    var existingData = new Dictionary<string, object>();

                    if (File.Exists(_filePath))
                    {
                        var existingJson = File.ReadAllText(_filePath);
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

                    _watcher.EnableRaisingEvents = false;
                    File.WriteAllText(_filePath, json);
                    _watcher.EnableRaisingEvents = true;

                    Load();
                    OnReload();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating configuration: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}