using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;
using System;
using System.Windows.Input;
using System.Threading.Tasks;
using SudokuGame.Services;
using Avalonia.Controls.Primitives;
using System.Diagnostics;

namespace SudokuGame.Views
{
    public partial class StartWindow : Window
    {
        private readonly Canvas backgroundGrid;
        private DatabaseService? databaseService;
        private StackPanel loginPanel = null!;
        private StackPanel registerPanel = null!;
        private Button startButton = null!;
        private TextBox usernameTextBox = null!;
        private TextBox passwordTextBox = null!;
        private TextBox registerUsernameTextBox = null!;
        private TextBox registerPasswordTextBox = null!;
        private TextBox confirmPasswordTextBox = null!;
        private bool _isDatabaseReady = false;
        private int _currentUserId = 0;

        public StartWindow()
        {
            // 设置窗口初始属性
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            MinWidth = 800;
            MinHeight = 600;

            InitializeComponent();
            InitializeDatabase();
            
            // 初始化控件
            backgroundGrid = this.FindControl<Canvas>("BackgroundGrid") ?? throw new InvalidOperationException("BackgroundGrid not found");
            loginPanel = this.FindControl<StackPanel>("LoginPanel") ?? throw new InvalidOperationException("LoginPanel not found");
            registerPanel = this.FindControl<StackPanel>("RegisterPanel") ?? throw new InvalidOperationException("RegisterPanel not found");
            startButton = this.FindControl<Button>("StartButton") ?? throw new InvalidOperationException("StartButton not found");
            usernameTextBox = this.FindControl<TextBox>("UsernameTextBox") ?? throw new InvalidOperationException("UsernameTextBox not found");
            passwordTextBox = this.FindControl<TextBox>("PasswordTextBox") ?? throw new InvalidOperationException("PasswordTextBox not found");
            registerUsernameTextBox = this.FindControl<TextBox>("RegisterUsernameTextBox") ?? throw new InvalidOperationException("RegisterUsernameTextBox not found");
            registerPasswordTextBox = this.FindControl<TextBox>("RegisterPasswordTextBox") ?? throw new InvalidOperationException("RegisterPasswordTextBox not found");
            confirmPasswordTextBox = this.FindControl<TextBox>("ConfirmPasswordTextBox") ?? throw new InvalidOperationException("ConfirmPasswordTextBox not found");
            
            // 设置窗口大小改变时重绘背景网格
            this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(_ => DrawBackgroundGrid()));
            
            // 设置按钮点击事件
            var loginButton = this.FindControl<Button>("LoginButton") ?? throw new InvalidOperationException("LoginButton not found");
            var registerButton = this.FindControl<Button>("RegisterButton") ?? throw new InvalidOperationException("RegisterButton not found");
            var showRegisterButton = this.FindControl<Button>("ShowRegisterButton") ?? throw new InvalidOperationException("ShowRegisterButton not found");
            var showLoginButton = this.FindControl<Button>("ShowLoginButton") ?? throw new InvalidOperationException("ShowLoginButton not found");
            
            loginButton.Click += LoginButton_Click;
            registerButton.Click += RegisterButton_Click;
            showRegisterButton.Click += ShowRegisterButton_Click;
            showLoginButton.Click += ShowLoginButton_Click;
            startButton.Click += StartButton_Click;
        }

        private void InitializeDatabase()
        {
            Task.Run(() =>
            {
                try
                {
                    databaseService = new DatabaseService();
                    Dispatcher.UIThread.Post(() => 
                    {
                        _isDatabaseReady = true;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                        _isDatabaseReady = false;
                        ShowError($"数据库连接失败: {ex.Message}");
                    });
                }
            });
        }

        private void DrawBackgroundGrid()
        {
            backgroundGrid.Children.Clear();

            // 获取画布尺寸
            double width = this.Bounds.Width;
            double height = this.Bounds.Height;

            // 网格大小
            double gridSize = 30;

            // 创建网格线
            for (double x = 0; x < width; x += gridSize)
            {
                var line = new Line
                {
                    StartPoint = new Point(x, 0),
                    EndPoint = new Point(x, height),
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                backgroundGrid.Children.Add(line);
            }

            for (double y = 0; y < height; y += gridSize)
            {
                var line = new Line
                {
                    StartPoint = new Point(0, y),
                    EndPoint = new Point(width, y),
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                backgroundGrid.Children.Add(line);
            }
        }

        private void LoginButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!_isDatabaseReady || databaseService == null)
            {
                ShowError("数据库未准备好，请稍后重试");
                return;
            }

            string username = usernameTextBox.Text ?? "";
            string password = passwordTextBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("用户名和密码不能为空");
                return;
            }

            var (success, message, userId) = databaseService.ValidateUser(username, password);
            if (success)
            {
                
                _currentUserId = userId;
                loginPanel.IsVisible = false;
                startButton.IsVisible = true;
            }
            else
            {
                ShowError(message);
            }
        }

        private void RegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!_isDatabaseReady || databaseService == null)
            {
                ShowError("数据库未准备好，请稍后重试");
                return;
            }

            string username = registerUsernameTextBox.Text ?? "";
            string password = registerPasswordTextBox.Text ?? "";
            string confirmPassword = confirmPasswordTextBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("用户名和密码不能为空");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("两次输入的密码不一致");
                return;
            }

            var (success, message, userId) = databaseService.RegisterUser(username, password);
            if (success)
            {
                _currentUserId = userId;
                ShowLoginButton_Click(sender, e);
            }
            else
            {
                ShowError(message);
            }
        }

        private void ShowRegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            loginPanel.IsVisible = false;
            registerPanel.IsVisible = true;
        }

        private void ShowLoginButton_Click(object? sender, RoutedEventArgs e)
        {
            registerPanel.IsVisible = false;
            loginPanel.IsVisible = true;
        }

        private void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(_currentUserId);
            mainWindow.Show();
            this.Close();
        }

        private async void ShowError(string message)
        {
            Window dialog = null!;
            dialog = new Window
            {
                Title = "错误",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = message, Margin = new Thickness(20) },
                        new Button
                        {
                            Content = "确定",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Margin = new Thickness(0, 10, 0, 20),
                            Command = new RelayCommand(() => dialog.Close())
                        }
                    }
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await dialog.ShowDialog(this);
        }

        private async void ShowSuccess(string message)
        {
            Window dialog = null!;
            dialog = new Window
            {
                Title = "成功",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = message, Margin = new Thickness(20) },
                        new Button
                        {
                            Content = "确定",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Margin = new Thickness(0, 10, 0, 20),
                            Command = new RelayCommand(() => dialog.Close())
                        }
                    }
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await dialog.ShowDialog(this);
        }
    }

    // 创建一个匿名观察者类来处理Observable订阅
    internal class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;

        public AnonymousObserver(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => _onNext(value);
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
} 