using CSV2CFX.AppSettings;
using CSV2CFX.Interfaces;
using CSV2CFX.Models;
using CSV2CFX.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CSV2CFX
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private ILogger<App>? _logger;
        public IServiceProvider? ServiceProvider => _host?.Services;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 自动更新
            AutoUpdate(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // 配置应用程序设置
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // 绑定配置到IOptions<T>
                    services.Configure<RabbitmqSetting>(context.Configuration.GetSection("RabbitMQ"));
                    services.Configure<BackgroundTaskSetting>(context.Configuration.GetSection("BackgroundTask"));
                    services.Configure<MachineStatusSetting>(context.Configuration.GetSection("MachineInfo"));
                    services.Configure<MachineMetadataSetting>(context.Configuration.GetSection("MachineMetadata"));
                    services.Configure<CsvFilePathSetting>(context.Configuration.GetSection("CsvFilePath"));
                    services.Configure<RabbitMQPublisherSettings>(context.Configuration.GetSection("RabbitMQPublisherSettings"));

                    // 注册 MainWindow 及其依赖项
                    services.AddSingleton<MainWindow>();
                    
                    // 在此处注册其他服务和依赖项
                    // add rabbitmq connection factory
                    services.AddSingleton<IConnectionFactory>(provider =>
                    {
                        var rabbitMQSetting = provider.GetRequiredService<IOptions<RabbitmqSetting>>().Value;
                        return new ConnectionFactory()
                        {
                            HostName = rabbitMQSetting.HostName ?? "localhost",
                            UserName = rabbitMQSetting.UserName ?? "admin",
                            Password = rabbitMQSetting.Password ?? "123456",
                            Port = rabbitMQSetting.Port,
                            VirtualHost = rabbitMQSetting.VirtualHost ?? "/",
                            AutomaticRecoveryEnabled = rabbitMQSetting.AutomaticRecoveryEnabled,
                            NetworkRecoveryInterval = TimeSpan.FromSeconds(rabbitMQSetting.NetworkRecoveryIntervalSeconds)
                        };
                    });

                    // 注册 RabbitMQ 连接工厂包装器
                    services.AddSingleton<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();

                    // add rabbitmq service
                    services.AddSingleton<IRabbitMQService, RabbitMQService>();

                    // add services
                    services.AddSingleton<IMachineService, MachineService>();
                    services.AddSingleton<IFileProcessorService, FileProcessorService>();
                })
                .Build();

            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("Application starting up.");

            _host.Start();

            // 显示主窗口
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void AutoUpdate(StartupEventArgs e)
        {
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/update")
                {
                    MessageBox.Show("应用程序已成功更新!", "更新完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
