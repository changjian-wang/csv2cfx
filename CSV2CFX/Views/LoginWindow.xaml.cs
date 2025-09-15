using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace CSV2CFX.Views
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : FluentWindow
    {
        // 硬编码的用户凭据
        private readonly Dictionary<string, UserCredential> _users = new Dictionary<string, UserCredential>
        {
            { "admin", new UserCredential("admin", "123456", "Administrator") },
            { "user", new UserCredential("user", "123456", "Employee") }
        };

        public bool IsLoggedIn { get; private set; } = false;
        public string? LoggedInUser { get; private set; }
        public string? LoggedInRole { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            
            // 设置默认选中第一个角色
            RoleComboBox.SelectedIndex = 0;

            // 设置焦点到用户名输入框
            Loaded += (s, e) => UsernameTextBox.Focus();

            // 添加回车键登录功能
            KeyDown += LoginWindow_KeyDown;
        }

        private void LoginWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text.Trim();
                string password = PasswordBox.Password;
                string selectedRole = GetSelectedRole();

                // 隐藏错误消息
                HideErrorMessage();

                // 验证输入
                if (string.IsNullOrEmpty(username))
                {
                    ShowErrorMessage("请输入用户名");
                    UsernameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    ShowErrorMessage("请输入密码");
                    PasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(selectedRole))
                {
                    ShowErrorMessage("请选择角色");
                    RoleComboBox.Focus();
                    return;
                }

                // 验证凭据
                if (ValidateCredentials(username, password, selectedRole))
                {
                    // 登录成功
                    IsLoggedIn = true;
                    LoggedInUser = username;
                    LoggedInRole = selectedRole;

                    // 显示成功消息
                    var result = System.Windows.MessageBox.Show(
                        $"登录成功！\n\n用户：{username}\n角色：{GetRoleDisplayName(selectedRole)}",
                        "登录成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // 关闭登录窗口
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    // 登录失败
                    ShowErrorMessage("用户名、密码或角色不正确，请检查后重试");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"登录过程中发生错误：{ex.Message}");
            }
        }

        private bool ValidateCredentials(string username, string password, string role)
        {
            if (!_users.ContainsKey(username))
                return false;

            var userCredential = _users[username];
            return userCredential.Username == username &&
                   userCredential.Password == password &&
                   userCredential.Role == role;
        }

        private string GetSelectedRole()
        {
            if (RoleComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        private string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "Administrator" => "管理员",
                "Employee" => "普通员工",
                _ => role
            };
        }

        private void ShowErrorMessage(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void HideErrorMessage()
        {
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
            ErrorMessageTextBlock.Text = string.Empty;
        }
    }

    /// <summary>
    /// 用户凭据类
    /// </summary>
    public class UserCredential
    {
        public string Username { get; }
        public string Password { get; }
        public string Role { get; }

        public UserCredential(string username, string password, string role)
        {
            Username = username;
            Password = password;
            Role = role;
        }
    }
}