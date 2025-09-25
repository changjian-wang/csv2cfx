using CSV2CFX.AppSettings;
using CSV2CFX.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CSV2CFX.Views
{
    public partial class LoginWindow : Window, IDisposable
    {
        private readonly HttpClient _client;
        private readonly IOptionsMonitor<ApiSetting> _apiOptions;
        private readonly IDisposable? _optionsChangeToken;

        // 登录成功后对外暴露的用户信息
        public string? LoggedInUser { get; private set; }
        public string? LoggedInRole { get; private set; }
        public string? LoggedInToken { get; private set; }

        public LoginWindow(HttpClient client, IOptionsMonitor<ApiSetting> apiOptions)
        {
            InitializeComponent();
            _client = client;
            _apiOptions = apiOptions;

            // 监听API配置更改
            _optionsChangeToken = _apiOptions.OnChange(OnApiSettingChanged);

            this.Loaded += (s, e) =>
            {
                // 确保焦点设到用户名框
                UserNameBox.Focus();
                Keyboard.Focus(UserNameBox);
            };

            // 有些自定义控件或模板会导致第一次点击不能获取键盘焦点，注册预处理以确保可以输入
            UserNameBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!UserNameBox.IsKeyboardFocused)
                {
                    e.Handled = true;
                    UserNameBox.Focus();
                }
            };
            PasswordBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!PasswordBox.IsKeyboardFocused)
                {
                    e.Handled = true;
                    PasswordBox.Focus();
                }
            };

            // 添加键盘快捷键支持
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    LoginButton_Click(LoginButton, new RoutedEventArgs());
                }
                else if (e.Key == Key.Escape)
                {
                    this.DialogResult = false;
                    this.Close();
                }
            };
        }

        private void OnApiSettingChanged(ApiSetting newApiSetting)
        {
            // 记录API配置更改
            System.Diagnostics.Debug.WriteLine($"登录窗口检测到API配置更改: {newApiSetting?.Endpoint}");
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserNameBox.Text))
            {
                MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                UserNameBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("请输入密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            // 禁用按钮防止重复提交
            LoginButton.IsEnabled = false;
            LoginButton.Content = "登录中...";
            this.Cursor = Cursors.Wait;

            try
            {
                var currentApiSetting = _apiOptions.CurrentValue;

                // 检查是否配置了登录URL
                if (string.IsNullOrEmpty(currentApiSetting.LoginUri))
                {
                    MessageBox.Show("登录服务未配置，请检查系统设置", "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 准备登录请求数据
                var loginRequest = new Dictionary<string, string>
                {
                    ["userName"] = UserNameBox.Text,
                    ["password"] = PasswordBox.Password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // 设置请求超时
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                // 发送登录请求
                var response = await _client.PostAsync(currentApiSetting.LoginUri, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var loginModel = JsonSerializer.Deserialize<LoginModel>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (loginModel != null && (loginModel.Status || loginModel.Success))
                        {
                            // 登录成功
                            LoggedInUser = loginModel.Data?.UserName ?? UserNameBox.Text;
                            LoggedInRole = loginModel.Data?.Roles.FirstOrDefault()?.RoleName;
                            LoggedInToken = loginModel.Data?.Token;

                            System.Diagnostics.Debug.WriteLine($"用户 {LoggedInUser} 登录成功");

                            this.DialogResult = true;
                            this.Close();
                        }
                        else
                        {
                            // 登录失败
                            var errorMessage = loginModel?.Message ?? "登录失败，请检查用户名和密码";
                            MessageBox.Show(errorMessage, "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);

                            // 清空密码框并设置焦点
                            PasswordBox.Clear();
                            PasswordBox.Focus();
                        }
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"JSON解析失败: {ex.Message}, 响应内容: {responseContent}");
                        MessageBox.Show("服务器响应格式错误", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"登录请求失败: {response.StatusCode}, 响应: {errorContent}");
                    MessageBox.Show($"登录服务返回错误: {response.StatusCode}", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("登录请求超时，请检查网络连接", "登录超时", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"网络请求异常: {ex.Message}");
                MessageBox.Show($"网络连接错误: {ex.Message}", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"登录异常: {ex.Message}");
                MessageBox.Show($"登录时发生错误: {ex.Message}", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复按钮状态
                LoginButton.IsEnabled = true;
                LoginButton.Content = "登录";
                this.Cursor = Cursors.Arrow;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public void Dispose()
        {
            _optionsChangeToken?.Dispose();
        }
    }
}