using CSV2CFX.AppSettings;
using CSV2CFX.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace CSV2CFX.Views
{
    public partial class ConfigurationPage : Page, IDisposable
    {
        private ConfigurationViewModel _viewModel;
        private readonly IOptionsMonitor<MachineInfoSetting> _machineInfoOptions;
        private readonly IOptionsMonitor<ApiSetting> _apiOptions;
        private readonly IConfiguration _configuration;

        public ConfigurationViewModel ViewModel
        {
            get => _viewModel;
            private set
            {
                _viewModel = value;
                DataContext = _viewModel;
            }
        }

        public ConfigurationPage(
            IOptionsMonitor<MachineInfoSetting> machineInfoOptions,
            IOptionsMonitor<ApiSetting> apiOptions,
            IConfiguration configuration)
        {
            _machineInfoOptions = machineInfoOptions;
            _apiOptions = apiOptions;
            _configuration = configuration;

            InitializeComponent();

            ViewModel = new ConfigurationViewModel(
                _machineInfoOptions,
                _apiOptions,
                _configuration
            );
        }

        // 设计时构造函数
        public ConfigurationPage() : this(null, null, null) { }

        private void BrowseProductionFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "选择生产信息文件",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                ViewModel.ProductionInformationFilePath = dialog.FileName;
            }
        }

        private void BrowseMachineStatusFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "选择机器状态信息文件",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                ViewModel.MachineStatusInformationFilePath = dialog.FileName;
            }
        }

        private void BrowseProcessDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var folderDialog = new FolderBrowserDialog
                {
                    Description = "选择过程数据文件夹",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };

                if (!string.IsNullOrEmpty(ViewModel.ProcessDataFilesFilePath) &&
                    Directory.Exists(ViewModel.ProcessDataFilesFilePath))
                {
                    folderDialog.SelectedPath = ViewModel.ProcessDataFilesFilePath;
                }

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.ProcessDataFilesFilePath = folderDialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"选择文件夹时发生错误: {ex.Message}");
            }
        }

        private async void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = CreateConfigurationObject();
                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = "appsettings.json",
                    Title = "保存配置文件"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllTextAsync(dialog.FileName, json, System.Text.Encoding.UTF8);
                    ShowSuccessMessage($"配置已保存到 {dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"保存配置时发生错误: {ex.Message}");
            }
        }

        private async void LoadConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "选择配置文件",
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = await File.ReadAllTextAsync(dialog.FileName, System.Text.Encoding.UTF8);
                    await LoadConfigurationFromJsonAsync(json);
                    ShowSuccessMessage($"配置已从 {dialog.FileName} 加载");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"加载配置时发生错误: {ex.Message}");
            }
        }

        private async void ResetConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "确定要重置所有配置到默认值吗？\n\n警告：此操作将清除所有当前配置！",
                "确认重置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ViewModel?.Dispose();
                    ViewModel = new ConfigurationViewModel(_machineInfoOptions, _apiOptions, _configuration);
                    ShowSuccessMessage("配置已重置为默认值");
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"重置配置时发生错误: {ex.Message}");
                }
            }
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Wpf.Ui.Controls.Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "🔄 测试中...";
                }

                ShowInfoMessage("正在测试连接，请稍候...");

                var testResults = await TestConnectionsAsync();

                if (testResults.All(r => r.Success))
                {
                    ShowSuccessMessage($"所有连接测试成功！\n\n{string.Join("\n", testResults.Select(r => $"✓ {r.Name}"))}");
                }
                else
                {
                    var failedTests = testResults.Where(r => !r.Success).ToList();
                    var successTests = testResults.Where(r => r.Success).ToList();
                    var message = "连接测试完成，部分测试失败：\n\n";
                    message += "✓ 成功：\n" + string.Join("\n", successTests.Select(r => $"  • {r.Name}"));
                    message += "\n\n✗ 失败：\n" + string.Join("\n", failedTests.Select(r => $"  • {r.Name}: {r.Error}"));
                    ShowErrorMessage(message);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"连接测试失败: {ex.Message}");
            }
            finally
            {
                var button = sender as Wpf.Ui.Controls.Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "🔗 Test Connection";
                }
            }
        }

        private async Task<List<TestResult>> TestConnectionsAsync()
        {
            var results = new List<TestResult>();

            try
            {
                await Task.Delay(500);
                
                // Test the selected protocol
                if (ViewModel.ProtocolType == CSV2CFX.AppSettings.ProtocolType.AMQP)
                {
                    if (string.IsNullOrEmpty(ViewModel.HostName))
                    {
                        results.Add(new TestResult("RabbitMQ", false, "主机名不能为空"));
                    }
                    else if (ViewModel.Port <= 0 || ViewModel.Port > 65535)
                    {
                        results.Add(new TestResult("RabbitMQ", false, "端口号无效"));
                    }
                    else
                    {
                        results.Add(new TestResult("RabbitMQ", true, ""));
                    }
                }
                else // MQTT
                {
                    if (string.IsNullOrEmpty(ViewModel.MqttBrokerHost))
                    {
                        results.Add(new TestResult("MQTT", false, "代理主机不能为空"));
                    }
                    else if (ViewModel.MqttBrokerPort <= 0 || ViewModel.MqttBrokerPort > 65535)
                    {
                        results.Add(new TestResult("MQTT", false, "代理端口号无效"));
                    }
                    else
                    {
                        results.Add(new TestResult("MQTT", true, ""));
                    }
                }
            }
            catch (Exception ex)
            {
                var protocolName = ViewModel.ProtocolType == CSV2CFX.AppSettings.ProtocolType.AMQP ? "RabbitMQ" : "MQTT";
                results.Add(new TestResult(protocolName, false, ex.Message));
            }

            try
            {
                await Task.Delay(500);
                if (string.IsNullOrEmpty(ViewModel.Endpoint))
                {
                    results.Add(new TestResult("API 端点", false, "API 端点不能为空"));
                }
                else if (!Uri.TryCreate(ViewModel.Endpoint, UriKind.Absolute, out _))
                {
                    results.Add(new TestResult("API 端点", false, "API 端点格式无效"));
                }
                else
                {
                    results.Add(new TestResult("API 端点", true, ""));
                }
            }
            catch (Exception ex)
            {
                results.Add(new TestResult("API 端点", false, ex.Message));
            }

            try
            {
                if (!string.IsNullOrEmpty(ViewModel.ProductionInformationFilePath))
                {
                    if (File.Exists(ViewModel.ProductionInformationFilePath))
                    {
                        results.Add(new TestResult("生产信息文件", true, ""));
                    }
                    else
                    {
                        results.Add(new TestResult("生产信息文件", false, "文件不存在"));
                    }
                }

                if (!string.IsNullOrEmpty(ViewModel.ProcessDataFilesFilePath))
                {
                    if (Directory.Exists(ViewModel.ProcessDataFilesFilePath))
                    {
                        results.Add(new TestResult("过程数据文件夹", true, ""));
                    }
                    else
                    {
                        results.Add(new TestResult("过程数据文件夹", false, "文件夹不存在"));
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(new TestResult("文件路径检查", false, ex.Message));
            }

            return results;
        }

        private object CreateConfigurationObject()
        {
            return new
            {
                Logging = new
                {
                    LogLevel = new
                    {
                        Default = ViewModel.DefaultLogLevel ?? "Information",
                        Lifetime = ViewModel.MicrosoftHostingLifetimeLogLevel ?? "Information"
                    }
                },
                Protocol = new
                {
                    Type = (int)ViewModel.ProtocolType
                },
                RabbitMQ = new
                {
                    HostName = ViewModel.HostName ?? "",
                    UserName = ViewModel.UserName ?? "",
                    Password = ViewModel.Password ?? "",
                    Port = ViewModel.Port,
                    VirtualHost = ViewModel.VirtualHost ?? "/",
                    AutomaticRecoveryEnabled = ViewModel.AutomaticRecoveryEnabled,
                    NetworkRecoveryIntervalSeconds = ViewModel.NetworkRecoveryIntervalSeconds
                },
                RabbitMQPublisherSettings = new
                {
                    Prefix = ViewModel.Prefix ?? ""
                },
                MQTT = new
                {
                    BrokerHost = ViewModel.MqttBrokerHost ?? "",
                    BrokerPort = ViewModel.MqttBrokerPort,
                    Username = ViewModel.MqttUsername ?? "",
                    Password = ViewModel.MqttPassword ?? "",
                    ClientId = ViewModel.MqttClientId ?? "",
                    UseTls = ViewModel.MqttUseTls,
                    KeepAlivePeriod = ViewModel.MqttKeepAlivePeriod,
                    CleanSession = ViewModel.MqttCleanSession,
                    ConnectionTimeout = ViewModel.MqttConnectionTimeout,
                    TopicPrefix = ViewModel.MqttTopicPrefix ?? ""
                },
                Api = new
                {
                    Endpoint = ViewModel.Endpoint ?? "",
                    LoginUri = ViewModel.LoginUri ?? ""
                },
                BackgroundTask = new
                {
                    MaxConcurrency = ViewModel.MaxConcurrency,
                    DelayBetweenBatchesMs = ViewModel.DelayBetweenBatchesMs
                },
                CsvFilePath = new
                {
                    ProductionInformationFilePath = ViewModel.ProductionInformationFilePath ?? "",
                    MachineStatusInformationFilePath = ViewModel.MachineStatusInformationFilePath ?? "",
                    ProcessDataFilesFilePath = ViewModel.ProcessDataFilesFilePath ?? ""
                },
                MachineInfo = new
                {
                    Heartbeat = "CFX.Heartbeat",
                    WorkStarted = "CFX.Production.WorkStarted",
                    WorkCompleted = "CFX.Production.WorkCompleted",
                    UnitsProcessed = "CFX.Production.Processing.UnitsProcessed",
                    StationStateChanged = "CFX.ResourcePerformance.StationStateChanged",
                    FaultOccurred = "CFX.ResourcePerformance.FaultOccurred",
                    FaultCleared = "CFX.ResourcePerformance.FaultCleared",
                    UniqueId = ViewModel.UniqueId ?? "",
                    Version = ViewModel.Version ?? "",
                    HeartbeatFrequency = ViewModel.HeartbeatFrequency
                },
                MachineMetadata = new
                {
                    Building = ViewModel.Building ?? "",
                    Device = ViewModel.Device ?? "",
                    AreaName = ViewModel.AreaName ?? "",
                    Organization = ViewModel.Organization ?? "",
                    LineName = ViewModel.LineName ?? "",
                    SiteName = ViewModel.SiteName ?? "",
                    StationName = ViewModel.StationName ?? "",
                    ProcessType = ViewModel.ProcessType ?? "",
                    MachineName = ViewModel.MachineName ?? "",
                    CreatedBy = ViewModel.CreatedBy ?? ""
                }
            };
        }

        private async Task LoadConfigurationFromJsonAsync(string json)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(json);
                var root = document.RootElement;

                // 加载配置到 ViewModel
                if (root.TryGetProperty("Logging", out var logging) && logging.TryGetProperty("LogLevel", out var logLevel))
                {
                    if (logLevel.TryGetProperty("Default", out var defaultLevel))
                    {
                        ViewModel.DefaultLogLevel = defaultLevel.GetString() ?? "Information";
                    }
                    if (logLevel.TryGetProperty("Lifetime", out var lifetimeLevel) ||
                        logLevel.TryGetProperty("Microsoft.Hosting.Lifetime", out lifetimeLevel))
                    {
                        ViewModel.MicrosoftHostingLifetimeLogLevel = lifetimeLevel.GetString() ?? "Information";
                    }
                }

                // Load protocol settings
                if (root.TryGetProperty("Protocol", out var protocol))
                {
                    if (protocol.TryGetProperty("Type", out var protocolType))
                    {
                        if (Enum.TryParse<CSV2CFX.AppSettings.ProtocolType>(protocolType.GetInt32().ToString(), out var pt))
                            ViewModel.ProtocolType = pt;
                    }
                }

                if (root.TryGetProperty("RabbitMQ", out var rabbitMQ))
                {
                    if (rabbitMQ.TryGetProperty("HostName", out var hostName))
                        ViewModel.HostName = hostName.GetString() ?? "";
                    if (rabbitMQ.TryGetProperty("UserName", out var userName))
                        ViewModel.UserName = userName.GetString() ?? "";
                    if (rabbitMQ.TryGetProperty("Password", out var password))
                        ViewModel.Password = password.GetString() ?? "";
                    if (rabbitMQ.TryGetProperty("Port", out var port))
                        ViewModel.Port = port.GetInt32();
                    if (rabbitMQ.TryGetProperty("VirtualHost", out var virtualHost))
                        ViewModel.VirtualHost = virtualHost.GetString() ?? "/";
                    if (rabbitMQ.TryGetProperty("AutomaticRecoveryEnabled", out var autoRecovery))
                        ViewModel.AutomaticRecoveryEnabled = autoRecovery.GetBoolean();
                    if (rabbitMQ.TryGetProperty("NetworkRecoveryIntervalSeconds", out var recoveryInterval))
                        ViewModel.NetworkRecoveryIntervalSeconds = recoveryInterval.GetInt32();
                }

                if (root.TryGetProperty("RabbitMQPublisherSettings", out var publisherSettings))
                {
                    if (publisherSettings.TryGetProperty("Prefix", out var prefix))
                        ViewModel.Prefix = prefix.GetString() ?? "";
                }

                // Load MQTT settings
                if (root.TryGetProperty("MQTT", out var mqtt))
                {
                    if (mqtt.TryGetProperty("BrokerHost", out var brokerHost))
                        ViewModel.MqttBrokerHost = brokerHost.GetString() ?? "";
                    if (mqtt.TryGetProperty("BrokerPort", out var brokerPort))
                        ViewModel.MqttBrokerPort = brokerPort.GetInt32();
                    if (mqtt.TryGetProperty("Username", out var mqttUsername))
                        ViewModel.MqttUsername = mqttUsername.GetString() ?? "";
                    if (mqtt.TryGetProperty("Password", out var mqttPassword))
                        ViewModel.MqttPassword = mqttPassword.GetString() ?? "";
                    if (mqtt.TryGetProperty("ClientId", out var clientId))
                        ViewModel.MqttClientId = clientId.GetString() ?? "";
                    if (mqtt.TryGetProperty("UseTls", out var useTls))
                        ViewModel.MqttUseTls = useTls.GetBoolean();
                    if (mqtt.TryGetProperty("KeepAlivePeriod", out var keepAlive))
                        ViewModel.MqttKeepAlivePeriod = keepAlive.GetInt32();
                    if (mqtt.TryGetProperty("CleanSession", out var cleanSession))
                        ViewModel.MqttCleanSession = cleanSession.GetBoolean();
                    if (mqtt.TryGetProperty("ConnectionTimeout", out var connectionTimeout))
                        ViewModel.MqttConnectionTimeout = connectionTimeout.GetInt32();
                    if (mqtt.TryGetProperty("TopicPrefix", out var topicPrefix))
                        ViewModel.MqttTopicPrefix = topicPrefix.GetString() ?? "";
                }

                if (root.TryGetProperty("Api", out var api))
                {
                    if (api.TryGetProperty("Endpoint", out var endpoint))
                        ViewModel.Endpoint = endpoint.GetString() ?? "";
                    if (api.TryGetProperty("LoginUri", out var loginUri))
                        ViewModel.LoginUri = loginUri.GetString() ?? "";
                }

                if (root.TryGetProperty("BackgroundTask", out var backgroundTask))
                {
                    if (backgroundTask.TryGetProperty("MaxConcurrency", out var maxConcurrency))
                        ViewModel.MaxConcurrency = maxConcurrency.GetInt32();
                    if (backgroundTask.TryGetProperty("DelayBetweenBatchesMs", out var delayMs))
                        ViewModel.DelayBetweenBatchesMs = delayMs.GetInt32();
                }

                if (root.TryGetProperty("CsvFilePath", out var csvFilePath))
                {
                    if (csvFilePath.TryGetProperty("ProductionInformationFilePath", out var prodPath))
                        ViewModel.ProductionInformationFilePath = prodPath.GetString() ?? "";
                    if (csvFilePath.TryGetProperty("MachineStatusInformationFilePath", out var statusPath))
                        ViewModel.MachineStatusInformationFilePath = statusPath.GetString() ?? "";
                    if (csvFilePath.TryGetProperty("ProcessDataFilesFilePath", out var processPath))
                        ViewModel.ProcessDataFilesFilePath = processPath.GetString() ?? "";
                }

                if (root.TryGetProperty("MachineInfo", out var machineInfo))
                {
                    if (machineInfo.TryGetProperty("UniqueId", out var uniqueId))
                        ViewModel.UniqueId = uniqueId.GetString() ?? "";
                    if (machineInfo.TryGetProperty("Version", out var version))
                        ViewModel.Version = version.GetString() ?? "";
                    if (machineInfo.TryGetProperty("HeartbeatFrequency", out var heartbeatFreq))
                        ViewModel.HeartbeatFrequency = heartbeatFreq.GetInt32();
                }

                if (root.TryGetProperty("MachineMetadata", out var machineMetadata))
                {
                    if (machineMetadata.TryGetProperty("Building", out var building))
                        ViewModel.Building = building.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("Device", out var device))
                        ViewModel.Device = device.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("AreaName", out var areaName))
                        ViewModel.AreaName = areaName.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("Organization", out var organization))
                        ViewModel.Organization = organization.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("LineName", out var lineName))
                        ViewModel.LineName = lineName.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("SiteName", out var siteName))
                        ViewModel.SiteName = siteName.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("StationName", out var stationName))
                        ViewModel.StationName = stationName.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("ProcessType", out var processType))
                        ViewModel.ProcessType = processType.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("MachineName", out var machineName))
                        ViewModel.MachineName = machineName.GetString() ?? "";
                    if (machineMetadata.TryGetProperty("CreatedBy", out var createdBy))
                        ViewModel.CreatedBy = createdBy.GetString() ?? "";
                }

                // 强制刷新 UI - 这是关键步骤！
                await Dispatcher.InvokeAsync(() =>
                {
                    // 触发所有属性的 PropertyChanged 事件
                    RefreshAllBindings();
                });
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"加载配置时发生错误: {ex.Message}");
            }
        }

        // 新增方法：刷新所有绑定
        private void RefreshAllBindings()
        {
            ViewModel.NotifyAllPropertiesChanged();
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfoMessage(string message)
        {
            MessageBox.Show(message, "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Dispose()
        {
            ViewModel?.Dispose();
        }

        private class TestResult
        {
            public string Name { get; }
            public bool Success { get; }
            public string Error { get; }

            public TestResult(string name, bool success, string error)
            {
                Name = name;
                Success = success;
                Error = error;
            }
        }
    }
}