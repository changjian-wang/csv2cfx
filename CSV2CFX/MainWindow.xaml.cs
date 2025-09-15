using AutoUpdaterDotNET;
using CSV2CFX.AppSettings;
using CSV2CFX.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace CSV2CFX
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isServiceRunning = false;
        private readonly ILogger<MainWindow> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isManualCheck = false;
        private readonly IMachineService _machineService;
        private readonly IRabbitMQService _rabbitMQService;
        private Task? _backgroundTask;
        private readonly IOptions<MachineStatusSetting> _options;

        public MainWindow(
            ILogger<MainWindow> logger,
            IMachineService machineService,
            IRabbitMQService rabbitMQService,
            IOptions<MachineStatusSetting> options)
        {
            _logger = logger;
            _options = options;
            _machineService = machineService;
            _rabbitMQService = rabbitMQService;

            InitializeComponent();

            // 显示版本信息
            UpdateVersionDisplay();

            // 配置AutoUpdater.NET
            ConfigureAutoUpdater();

            // 创建统一的CancellationTokenSource
            _cancellationTokenSource = new CancellationTokenSource();

            // 启动每日更新检查
            StartDailyUpdateCheck();

            // 启动后延迟3秒进行静默更新检查
            Task.Delay(3000).ContinueWith(_ =>
            {
                if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() => CheckForUpdatesAsync(false));
                }
            });

            // 启动后台处理任务
            // StartBackgroundProcessing();
        }

        private void StartBackgroundProcessing()
        {
            _backgroundTask = Task.Run(async () =>
            {
                if (!string.IsNullOrWhiteSpace(_options.Value.UniqueId))
                {
                    await _machineService.CreateRabbitmqAsync(uniqueId: _options.Value.UniqueId).ConfigureAwait(false);
                }

                while (!_cancellationTokenSource!.Token.IsCancellationRequested)
                {
                    try
                    {
                        await RunAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // 任务被取消，安全退出
                        _logger?.LogInformation("Background processing task was cancelled.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 记录异常日志
                        _logger?.LogError(ex, "An error occurred during background processing.");
                        await Task.Delay(1000).ConfigureAwait(false); // 等待一段时间后重试
                    }
                }
            }, _cancellationTokenSource!.Token);
        }

        private void StopBackgroundProcessing()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();

                // 等待任务完成（可选超时）
                try
                {
                    _backgroundTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException ex)
                {
                    // 处理可能的异常
                }
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var uniqueId = _options.Value.UniqueId;

            if (!string.IsNullOrWhiteSpace(uniqueId))
            {
                var tasks = new Func<string, Task>[] 
                {
                    _machineService.PublishHeartbeatAsync,
                    _machineService.PublishWorkProcessAsync,
                    _machineService.PublishMachineStateAsync,
                };

                // 并行任务
                await Parallel.ForEachAsync(tasks, new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = tasks.Length
                }, async (task, ct) =>
                {
                    await task(uniqueId).ConfigureAwait(false);
                });

                // 添加适当的延迟，避免CPU过载
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        private void UpdateVersionDisplay()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;

                System.Diagnostics.Debug.WriteLine($"当前版本: {version}");
                System.Diagnostics.Debug.WriteLine($"程序集位置: {assembly.Location}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取版本信息失败: {ex.Message}");
            }
        }

        private void ConfigureAutoUpdater()
        {
            AutoUpdater.UpdateMode = Mode.Normal;
            AutoUpdater.Mandatory = false;
            AutoUpdater.UpdateFormSize = new System.Drawing.Size(800, 600);
            AutoUpdater.AppTitle = "CSV2CFX Service";
            AutoUpdater.AppCastURL = "https://raw.githubusercontent.com/changjian-wang/lc-materials/refs/heads/main/updates.xml";
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ReportErrors = false;

            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
        }

        private void StartDailyUpdateCheck()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        var nextMidnight = now.Date.AddDays(1);
                        var delay = nextMidnight - now;

                        System.Diagnostics.Debug.WriteLine($"当前UTC时间: {now:yyyy-MM-dd HH:mm:ss}");
                        System.Diagnostics.Debug.WriteLine($"下次更新检查: {nextMidnight:yyyy-MM-dd HH:mm:ss} UTC");

                        await Task.Delay(delay, _cancellationTokenSource.Token).ConfigureAwait(false);

                        if (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                System.Diagnostics.Debug.WriteLine($"执行每日自动更新检查: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                                CheckForUpdatesAsync(false);
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"每日更新检查异常: {ex.Message}");
                        try
                        {
                            await Task.Delay(TimeSpan.FromHours(1), _cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args?.Error != null)
            {
                if (_isManualCheck)
                {
                    MessageBox.Show($"检查更新时发生错误:\n{args.Error.Message}",
                                  "更新检查错误",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
                System.Diagnostics.Debug.WriteLine($"更新检查错误: {args.Error.Message}");
                _isManualCheck = false;
                return;
            }

            if (args?.IsUpdateAvailable == true)
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                var message = $"发现新版本可用!\n\n" +
                            $"当前版本: {currentVersion}\n" +
                            $"最新版本: {args.CurrentVersion}\n\n" +
                            $"是否立即下载并安装更新？";

                var result = MessageBox.Show(message,
                                            "发现更新",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            Application.Current.Shutdown();
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show($"下载更新时发生错误:\n{exception.Message}",
                                      "更新错误",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Error);
                    }
                }
            }
            else if (args != null)
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                if (_isManualCheck)
                {
                    MessageBox.Show($"当前版本 {currentVersion} 已是最新版本，无需更新。",
                                  "检查更新",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            else
            {
                if (_isManualCheck)
                {
                    MessageBox.Show("无法连接到更新服务器，请检查网络连接后重试。",
                                  "更新检查失败",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }

            _isManualCheck = false;
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            Application.Current.Shutdown();
        }

        private void CheckForUpdatesAsync(bool isManualCheck)
        {
            _isManualCheck = isManualCheck;
            AutoUpdater.ReportErrors = isManualCheck;

            UpdateVersionDisplay();

            AutoUpdater.Start();
        }

        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdatesAsync(true);
        }

        private void ManualUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("手动更新功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isServiceRunning)
            {
                // 启动服务
                StartBackgroundProcessing();
                StartServiceButton.Content = "停止服务";
                ServiceStatusText.Text = "服务状态：运行中";
                ServiceStatusText.Foreground = System.Windows.Media.Brushes.Green;
                isServiceRunning = true;
            }
            else
            {
                // 停止服务
                StopBackgroundProcessing();
                StartServiceButton.Content = "启动服务";
                ServiceStatusText.Text = "服务状态：已停止";
                ServiceStatusText.Foreground = System.Windows.Media.Brushes.Red;
                isServiceRunning = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.ApplicationExitEvent -= AutoUpdater_ApplicationExitEvent;

            base.OnClosed(e);
        }
    }
}