using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SudokuGame.Models;
using SudokuGame.Services;

namespace SudokuGame.Views
{
    public partial class MainWindow : Window
    {
        private TextBox[,] cells = new TextBox[9, 9];
        private int[,] solution = new int[9, 9];
        private int[,] puzzle = new int[9, 9];
        private Random random = new Random();
        private bool isGameStarted = false;
        private Stopwatch gameStopwatch = new();
        private DispatcherTimer displayTimer = new();
        private Grid gamePanel;
        private Grid contentArea;
        private MyPuzzlesView myPuzzlesView;
        private readonly int _userId;
        private readonly string _userRole;
        private MyPuzzlesView? _myPuzzlesView;
        private GameView? _gameView;
        private bool _isFavorited = false;
        private Button? favoriteButton;
        private SudokuPuzzle? currentPuzzle;
        private readonly DatabaseService _databaseService;
        private bool _isLoadingExistingPuzzle = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _databaseService = new DatabaseService();

            // 获取用户角色
            _userRole = _databaseService.GetUserRole(userId);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    InitializeControls();
                    SetupGame();
                    SetupEventHandlers();
                    InitializeNavigation();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"初始化错误: {ex.Message}");
                }
            });
        }

        private void InitializeControls()
        {
            gamePanel = this.FindControl<Grid>("GamePanel") ?? throw new InvalidOperationException("GamePanel not found");
            contentArea = this.FindControl<Grid>("ContentArea") ?? throw new InvalidOperationException("ContentArea not found");
            myPuzzlesView = new MyPuzzlesView(_userId);
            myPuzzlesView.OnPuzzleSelected += MyPuzzlesView_OnPuzzleSelected;
            favoriteButton = this.FindControl<Button>("FavoriteButton");
            
            if (favoriteButton != null)
            {
                favoriteButton.Click += FavoriteButton_Click;
            }

            // 初始化退出登录按钮
            var logoutButton = this.FindControl<Button>("LogoutButton");
            if (logoutButton != null)
            {
                logoutButton.Click += LogoutButton_Click;
            }
        }

        private void SetupEventHandlers()
        {
            try
            {
                var navList = this.FindControl<ListBox>("NavList");
                if (navList != null)
                {
                    navList.SelectionChanged += NavList_SelectionChanged;
                }

                var buttonPanel = this.FindControl<StackPanel>("ButtonPanel");
                if (buttonPanel != null)
                {
                    var buttons = buttonPanel.Children;
                    if (buttons.Count >= 3)
                    {
                        ((Button)buttons[0]).Click += (s, e) => GenerateNewGame();
                        ((Button)buttons[1]).Click += (s, e) => CheckSolution();
                        ((Button)buttons[2]).Click += StartTimerButton_Click;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置事件处理器错误: {ex.Message}");
            }
        }

        private void SetupGame()
        {
            try
            {
                var sudokuGrid = this.FindControl<Grid>("SudokuGrid");
                if (sudokuGrid == null)
                {
                    Debug.WriteLine("找不到SudokuGrid");
                    return;
                }

                sudokuGrid.RowDefinitions.Clear();
                sudokuGrid.ColumnDefinitions.Clear();
                sudokuGrid.Children.Clear();

                for (int i = 0; i < 9; i++)
                {
                    sudokuGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
                    sudokuGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                }

                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        var cell = new TextBox
                        {
                            MaxLength = 1,
                            MinWidth = 60,
                            MinHeight = 60,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            FontSize = 24,
                            FontWeight = FontWeight.Bold,
                            Margin = new Thickness(0),
                            Padding = new Thickness(0),
                            BorderThickness = new Thickness(
                                j % 3 == 0 ? 3 : 1,  // 左边框：3x3格子的开始处加粗
                                i % 3 == 0 ? 3 : 1,  // 上边框：3x3格子的开始处加粗
                                j == 8 ? 3 : (j % 3 == 2 ? 3 : 1),  // 右边框：3x3格子的结束处加粗
                                i == 8 ? 3 : (i % 3 == 2 ? 3 : 1)   // 下边框：3x3格子的结束处加粗
                            ),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51))  // 使用深灰色边框 #333333
                        };

                        // 设置输入验证
                        cell.TextInput += (s, e) =>
                        {
                            // 只允许输入1-9的数字
                            if (e.Text.Length == 1 && (e.Text[0] < '1' || e.Text[0] > '9'))
                            {
                                e.Handled = true;
                            }
                        };

                        // 添加文本变更事件处理
                        cell.PropertyChanged += (s, e) =>
                        {
                            if (e.Property == TextBox.TextProperty && s is TextBox textBox)
                            {
                                string text = textBox.Text ?? "";
                                if (text.Length > 0)
                                {
                                    // 检查是否是1-9的数字
                                    if (!char.IsDigit(text[0]) || text[0] == '0')
                                    {
                                        textBox.Text = "";
                                    }
                                }
                            }
                        };

                        // 添加键盘事件处理
                        cell.KeyDown += (s, e) =>
                        {
                            // 允许使用退格键和删除键
                            if (e.Key != Avalonia.Input.Key.Back && e.Key != Avalonia.Input.Key.Delete)
                            {
                                // 如果不是数字键（包括小键盘的数字键），则阻止输入
                                bool isNumericKey = (e.Key >= Avalonia.Input.Key.D1 && e.Key <= Avalonia.Input.Key.D9) ||
                                                  (e.Key >= Avalonia.Input.Key.NumPad1 && e.Key <= Avalonia.Input.Key.NumPad9);
                                if (!isNumericKey)
                                {
                                    e.Handled = true;
                                }
                            }
                        };

                        // 设置文本颜色
                        cell.TextChanged += (s, e) =>
                        {
                            if (!cell.IsReadOnly)
                            {
                                cell.Foreground = new SolidColorBrush(Color.FromRgb(64, 158, 255)); // 蓝色 #409EFF
                            }
                        };

                        cells[i, j] = cell;
                        Grid.SetRow(cell, i);
                        Grid.SetColumn(cell, j);
                        sudokuGrid.Children.Add(cell);
                    }
                }

                gameStopwatch.Start();
                displayTimer.Interval = TimeSpan.FromMilliseconds(10);
                displayTimer.Tick += DisplayTimer_Tick;

                // GenerateNewGame() 将在构造函数异步块的 SetupGame() 后调用
                // DisplayPuzzle() 将在 GenerateNewGame() 中调用，负责设置初始状态和颜色
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置游戏错误: {ex.Message}");
            }
        }

        private void DisplayPuzzle()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    // 根据谜题数据设置文本、只读状态和颜色
                    cells[i, j].Text = puzzle[i, j] == 0 ? "" : puzzle[i, j].ToString();
                    cells[i, j].IsReadOnly = puzzle[i, j] != 0;
                    cells[i, j].IsEnabled = false; // 初始状态下禁用所有单元格
                    
                   
                    if (cells[i, j].IsReadOnly)
                    {
                        // 初始数字（只读单元格）使用浅灰色背景和深灰色文字
                        cells[i, j].Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // 更浅的灰色 #F5F5F5
                        cells[i, j].Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)); // #333333
                    }
                    else
                    {
                        // 可编辑单元格使用纯白色背景和蓝色文字
                        cells[i, j].Background = Brushes.White;
                        cells[i, j].Foreground = new SolidColorBrush(Color.FromRgb(64, 158, 255)); // #409EFF
                    }

                     // 统一设置单元格边框
                    cells[i, j].BorderThickness = new Thickness(
                        j % 3 == 0 ? 3 : 1,  // 左边框：3x3格子的开始处加粗
                        i % 3 == 0 ? 3 : 1,  // 上边框：3x3格子的开始处加粗
                        j == 8 ? 3 : (j % 3 == 2 ? 3 : 1),  // 右边框：3x3格子的结束处加粗
                        i == 8 ? 3 : (i % 3 == 2 ? 3 : 1)   // 下边框：3x3格子的结束处加粗
                    );
                    cells[i, j].BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51));

                    // 设置单元格背景和文字颜色
                }
            }
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var selectedItem = (ListBoxItem)listBox.SelectedItem;
            var selectedContent = selectedItem?.Content.ToString();

            // 隐藏游戏面板
            gamePanel.IsVisible = false;

            // 清除其他视图
            var viewsToRemove = contentArea.Children.Where(c => c != gamePanel).ToList();
            foreach (var view in viewsToRemove)
            {
                contentArea.Children.Remove(view);
            }

            // 显示选中的内容
            switch (selectedContent)
            {
                case "随机一题":
                    gamePanel.IsVisible = true;
                    if (!_isLoadingExistingPuzzle)
                    {
                        GenerateNewGame();
                    }
                    _isLoadingExistingPuzzle = false;
                    break;
                case "我的题库":
                    Grid.SetRow(myPuzzlesView, 0);
                    Grid.SetRowSpan(myPuzzlesView, 4);
                    contentArea.Children.Add(myPuzzlesView);
                    break;
                case "在线比赛":
                    var contestListView = new ContestListView(_userId);
                    Grid.SetRow(contestListView, 0);
                    Grid.SetRowSpan(contestListView, 4);
                    contentArea.Children.Add(contestListView);
                    break;
                case "游戏管理":
                    var gameManagementView = new GameManagementView(_userId);
                    Grid.SetRow(gameManagementView, 0);
                    Grid.SetRowSpan(gameManagementView, 4);
                    contentArea.Children.Add(gameManagementView);
                    break;
            }
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            if (gameStopwatch.IsRunning)
            {
                var elapsed = gameStopwatch.Elapsed;
                var timerDisplay = this.FindControl<TextBlock>("TimerDisplay");
                if (timerDisplay != null)
                {
                    timerDisplay.Text = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}:{elapsed.Milliseconds:D3}";
                }
            }
        }

        private void StartTimerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (!isGameStarted)
            {
                isGameStarted = true;
                gameStopwatch.Start();
                displayTimer.Start();
                button.Content = "暂停";
                EnableAllCells(true);
            }
            else
            {
                gameStopwatch.Stop();
                displayTimer.Stop();
                button.Content = "继续";
                isGameStarted = false;
                EnableAllCells(false);
            }
        }

        private void EnableAllCells(bool enable)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (!cells[i, j].IsReadOnly)
                    {
                        cells[i, j].IsEnabled = enable;
                    }
                }
            }
        }

        private void GenerateNewGame()
        {
            gameStopwatch.Reset();
            displayTimer.Stop();
            var timerDisplay = this.FindControl<TextBlock>("TimerDisplay");
            if (timerDisplay != null)
            {
                timerDisplay.Text = "00:00:000";
            }
            isGameStarted = false;
            var startButton = this.FindControl<Button>("StartButton");
            if (startButton != null)
            {
                startButton.Content = "开始填写";
            }

            GenerateSudoku();
            DisplayPuzzle();
            EnableAllCells(false);

            // 重置收藏状态
            _isFavorited = false;
            if (favoriteButton != null)
            {
                favoriteButton.Classes.Remove("active");
            }

            // 创建新的数独题目对象
            currentPuzzle = new SudokuPuzzle
            {
                UserId = _userId,
                InitialBoard = BoardToString(puzzle),
                CurrentBoard = BoardToString(puzzle),
                Solution = BoardToString(solution),
                Difficulty = "普通", // 可以根据实际难度设置
                CreatedAt = DateTime.Now,
                LastPlayedAt = null,
                TotalPlayTime = TimeSpan.Zero,
                IsCompleted = false
            };
        }

        private string BoardToString(int[,] board)
        {
            var result = new char[81];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    result[i * 9 + j] = board[i, j].ToString()[0];
            return new string(result);
        }

        private async void FavoriteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (currentPuzzle == null) return;

            try
            {
                if (!_isFavorited)
                {
                    // 保存到数据库
                    _databaseService.SavePuzzle(currentPuzzle);
                    _isFavorited = true;
                    if (favoriteButton != null)
                    {
                        favoriteButton.Classes.Add("active");
                    }

                    // 显示成功消息
                    var messageWindow = new Window
                    {
                        Title = "成功",
                        Content = "题目已收藏",
                        Width = 200,
                        Height = 100,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    await messageWindow.ShowDialog(this);
                }
                else
                {
                    // 从数据库中删除
                    _databaseService.DeletePuzzle(currentPuzzle.Id);
                    _isFavorited = false;
                    if (favoriteButton != null)
                    {
                        favoriteButton.Classes.Remove("active");
                    }

                    // 显示成功消息
                    var messageWindow = new Window
                    {
                        Title = "成功",
                        Content = "已取消收藏",
                        Width = 200,
                        Height = 100,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    await messageWindow.ShowDialog(this);
                }

                // 刷新题库视图
                myPuzzlesView.RefreshPuzzles();
            }
            catch (Exception ex)
            {
                var messageWindow = new Window
                {
                    Title = "错误",
                    Content = $"操作失败: {ex.Message}",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                await messageWindow.ShowDialog(this);
            }
        }

        private void GenerateSudoku()
        {
            Array.Clear(solution, 0, solution.Length);
            Array.Clear(puzzle, 0, puzzle.Length);
            GenerateSolution(0, 0);
            Array.Copy(solution, puzzle, solution.Length);

            // 移除一些数字来创建谜题
            int cellsToRemove = 40; // 可以调整难度
            while (cellsToRemove > 0)
            {
                int row = random.Next(9);
                int col = random.Next(9);
                if (puzzle[row, col] != 0)
                {
                    puzzle[row, col] = 0;
                    cellsToRemove--;
                }
            }
        }

        private bool GenerateSolution(int row, int col)
        {
            if (col >= 9)
            {
                row++;
                col = 0;
            }
            if (row >= 9)
                return true;

            var numbers = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = numbers.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = numbers[i];
                numbers[i] = numbers[j];
                numbers[j] = temp;
            }

            foreach (int num in numbers)
            {
                if (IsValid(row, col, num))
                {
                    solution[row, col] = num;
                    if (GenerateSolution(row, col + 1))
                        return true;
                    solution[row, col] = 0;
                }
            }
            return false;
        }

        private bool IsValid(int row, int col, int num)
        {
            // 检查行
            for (int x = 0; x < 9; x++)
                if (solution[row, x] == num)
                    return false;

            // 检查列
            for (int x = 0; x < 9; x++)
                if (solution[x, col] == num)
                    return false;

            // 检查3x3方块
            int startRow = row - row % 3, startCol = col - col % 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (solution[i + startRow, j + startCol] == num)
                        return false;

            return true;
        }

        private void CheckSolution()
        {
            int[,] currentGrid = new int[9, 9];
            bool isComplete = true;

            // 收集当前输入
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (string.IsNullOrEmpty(cells[i, j].Text))
                    {
                        isComplete = false;
                        break;
                    }
                    if (int.TryParse(cells[i, j].Text, out int value))
                    {
                        currentGrid[i, j] = value;
                    }
                }
                if (!isComplete) break;
            }

            if (!isComplete)
            {
                var messageWindow = new Window
                {
                    Title = "提示",
                    Content = "还没有完成填写！",
                    Width = 250,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageWindow.ShowDialog(this);
                return;
            }

            // 验证答案
            bool isValid = ValidateSolution(currentGrid);

            if (isValid)
            {
                gameStopwatch.Stop();
                displayTimer.Stop();
                var messageWindow = new Window
                {
                    Title = "成功",
                    Content = $"恭喜！你已经完成了数独！\n用时: {this.FindControl<TextBlock>("TimerDisplay").Text}",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageWindow.ShowDialog(this);
            }
            else
            {
                var messageWindow = new Window
                {
                    Title = "提示",
                    Content = "答案不正确，请继续努力！",
                    Width = 250,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageWindow.ShowDialog(this);
            }
        }

        private bool ValidateSolution(int[,] grid)
        {
            // 检查行
            for (int row = 0; row < 9; row++)
            {
                bool[] used = new bool[10];
                for (int col = 0; col < 9; col++)
                {
                    int num = grid[row, col];
                    if (num < 1 || num > 9 || used[num])
                        return false;
                    used[num] = true;
                }
            }

            // 检查列
            for (int col = 0; col < 9; col++)
            {
                bool[] used = new bool[10];
                for (int row = 0; row < 9; row++)
                {
                    int num = grid[row, col];
                    if (used[num])
                        return false;
                    used[num] = true;
                }
            }

            // 检查3x3方块
            for (int block = 0; block < 9; block++)
            {
                bool[] used = new bool[10];
                int rowStart = (block / 3) * 3;
                int colStart = (block % 3) * 3;
                
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int num = grid[rowStart + i, colStart + j];
                        if (used[num])
                            return false;
                        used[num] = true;
                    }
                }
            }

            return true;
        }

        private void InitializeViews()
        {
            _myPuzzlesView = new MyPuzzlesView(_userId);
            _myPuzzlesView.OnPuzzleSelected += MyPuzzlesView_OnPuzzleSelected;
            _gameView = new GameView(_userId);

            // 初始显示题库视图
            var contentControl = this.FindControl<ContentControl>("MainContent");
            if (contentControl != null)
            {
                contentControl.Content = _myPuzzlesView;
            }
        }

        private void MyPuzzlesView_OnPuzzleSelected(object? sender, SudokuPuzzle puzzle)
        {
            Debug.WriteLine("开始加载题目...");
            _isLoadingExistingPuzzle = true;
            
            try
            {
                // 切换到游戏面板
                gamePanel.IsVisible = true;
                if (myPuzzlesView.Parent == contentArea)
                {
                    contentArea.Children.Remove(myPuzzlesView);
                }

                // 更新导航栏选中项
                var navList = this.FindControl<ListBox>("NavList");
                if (navList != null)
                {
                    navList.SelectedIndex = 1; // "随机一题"的索引
                }

                // 加载题目
                LoadExistingPuzzle(puzzle);
                Debug.WriteLine("题目加载完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载题目时出错: {ex.Message}");
            }
        }

        private void LoadExistingPuzzle(SudokuPuzzle puzzle)
        {
            currentPuzzle = puzzle;
            _isFavorited = true; // 设置为已收藏状态
            if (favoriteButton != null)
            {
                favoriteButton.Classes.Add("active");
            }

            // 将字符串转换为数独数组
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int index = i * 9 + j;
                    this.puzzle[i, j] = puzzle.InitialBoard[index] - '0';
                    this.solution[i, j] = puzzle.Solution[index] - '0';
                }
            }

            // 显示题目
            DisplayPuzzle();

            // 重置游戏状态
            gameStopwatch.Reset();
            displayTimer.Stop();
            var timerDisplay = this.FindControl<TextBlock>("TimerDisplay");
            if (timerDisplay != null)
            {
                timerDisplay.Text = "00:00:000";
            }
            isGameStarted = false;
            var startButton = this.FindControl<Button>("StartButton");
            if (startButton != null)
            {
                startButton.Content = "开始填写";
            }
            EnableAllCells(false);
        }

        // 添加新方法用于设置内容
        public void SetContent(UserControl content)
        {
            // 清除其他视图
            var viewsToRemove = contentArea.Children.Where(c => c != gamePanel).ToList();
            foreach (var view in viewsToRemove)
            {
                contentArea.Children.Remove(view);
            }

            // 添加新内容
            Grid.SetRow(content, 0);
            Grid.SetRowSpan(content, 4);
            contentArea.Children.Add(content);
        }

        private void InitializeNavigation()
        {
            var navList = this.FindControl<ListBox>("NavList");
            if (navList != null)
            {
                // 如果是普通用户，移除游戏管理选项
                if (_userRole != "admin")
                {
                    var gameManagementItem = navList.Items.Cast<ListBoxItem>()
                        .FirstOrDefault(item => item.Content?.ToString() == "游戏管理");
                    if (gameManagementItem != null)
                    {
                        navList.Items.Remove(gameManagementItem);
                    }
                }
            }
        }

        private void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            // 创建并显示新的登录窗口
            var startWindow = new StartWindow();
            startWindow.Show();

            // 关闭当前窗口
            Close();
        }
    }
} 