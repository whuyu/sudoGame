using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SudokuGame.Models;
using SudokuGame.Services;
using System.Linq;
using System.Text.RegularExpressions;

namespace SudokuGame.Views
{
    public partial class ContestPuzzleWindow : Window
    {
        private TextBox[,] cells = new TextBox[9, 9];
        private int[,] solution = new int[9, 9];
        private int[,] puzzle = new int[9, 9];
        private Stopwatch gameStopwatch = new();
        private DispatcherTimer displayTimer = new();
        private readonly int _contestId;
        private readonly int _userId;
        private readonly int _puzzleIndex;
        private readonly DatabaseService _databaseService;
        private SudokuPuzzle _currentPuzzle;

        public ContestPuzzleWindow(int contestId, int userId, int puzzleIndex, SudokuPuzzle puzzle)
        {
            InitializeComponent();
            _contestId = contestId;
            _userId = userId;
            _puzzleIndex = puzzleIndex;
            _currentPuzzle = puzzle;
            _databaseService = new DatabaseService();

            InitializeControls();
            SetupGame();
            SetupEventHandlers();
            LoadPuzzle();
        }

        private void InitializeControls()
        {
            var titleBlock = this.FindControl<TextBlock>("PuzzleTitle");
            if (titleBlock != null)
            {
                titleBlock.Text = $"题目 {_puzzleIndex + 1}";
            }
        }

        private void SetupEventHandlers()
        {
            var clearButton = this.FindControl<Button>("ClearButton");
            var checkButton = this.FindControl<Button>("CheckButton");
            var submitButton = this.FindControl<Button>("SubmitButton");

            if (clearButton != null) clearButton.Click += ClearButton_Click;
            if (checkButton != null) checkButton.Click += CheckButton_Click;
            if (submitButton != null) submitButton.Click += SubmitButton_Click;
        }

        private void SetupGame()
        {
            var sudokuGrid = this.FindControl<Grid>("SudokuGrid");
            if (sudokuGrid == null) return;

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
                            j % 3 == 0 ? 3 : 1,
                            i % 3 == 0 ? 3 : 1,
                            j == 8 ? 3 : (j % 3 == 2 ? 3 : 1),
                            i == 8 ? 3 : (i % 3 == 2 ? 3 : 1)
                        ),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                    };

                    // 设置输入验证
                    cell.TextInput += (s, e) =>
                    {
                        if (e.Text.Length == 1 && (e.Text[0] < '1' || e.Text[0] > '9'))
                        {
                            e.Handled = true;
                        }
                    };

                    cell.PropertyChanged += (s, e) =>
                    {
                        if (e.Property == TextBox.TextProperty && s is TextBox textBox)
                        {
                            string text = textBox.Text ?? "";
                            if (text.Length > 0)
                            {
                                if (!char.IsDigit(text[0]) || text[0] == '0')
                                {
                                    textBox.Text = "";
                                }
                            }
                        }
                    };

                    cell.KeyDown += (s, e) =>
                    {
                        if (e.Key != Avalonia.Input.Key.Back && e.Key != Avalonia.Input.Key.Delete)
                        {
                            bool isNumericKey = (e.Key >= Avalonia.Input.Key.D1 && e.Key <= Avalonia.Input.Key.D9) ||
                                              (e.Key >= Avalonia.Input.Key.NumPad1 && e.Key <= Avalonia.Input.Key.NumPad9);
                            if (!isNumericKey)
                            {
                                e.Handled = true;
                            }
                        }
                    };

                    cell.TextChanged += (s, e) =>
                    {
                        if (!cell.IsReadOnly)
                        {
                            cell.Foreground = new SolidColorBrush(Color.FromRgb(64, 158, 255));
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
            displayTimer.Start();
        }

        private void LoadPuzzle()
        {
            // 将字符串转换为数独数组
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int index = i * 9 + j;
                    puzzle[i, j] = _currentPuzzle.InitialBoard[index] - '0';
                    solution[i, j] = _currentPuzzle.Solution[index] - '0';
                }
            }

            // 显示题目
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    cells[i, j].Text = puzzle[i, j] == 0 ? "" : puzzle[i, j].ToString();
                    cells[i, j].IsReadOnly = puzzle[i, j] != 0;
                    
                    if (cells[i, j].IsReadOnly)
                    {
                        cells[i, j].Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                        cells[i, j].Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                    }
                    else
                    {
                        cells[i, j].Background = Brushes.White;
                        cells[i, j].Foreground = new SolidColorBrush(Color.FromRgb(64, 158, 255));
                    }
                }
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

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var messageWindow = new Window
            {
                Title = "确认",
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "确定要清空所有已填写的内容吗？",
                            TextWrapping = TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 10,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children =
                            {
                                new Button
                                {
                                    Content = "取消",
                                    Width = 80,
                                    Height = 30
                                },
                                new Button
                                {
                                    Content = "确定",
                                    Width = 80,
                                    Height = 30,
                                    Background = new SolidColorBrush(Color.FromRgb(244, 67, 54))
                                }
                            }
                        }
                    }
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var cancelButton = ((messageWindow.Content as StackPanel)?.Children[1] as StackPanel)?.Children[0] as Button;
            var confirmButton = ((messageWindow.Content as StackPanel)?.Children[1] as StackPanel)?.Children[1] as Button;

            var tcs = new TaskCompletionSource<bool>();

            if (cancelButton != null)
                cancelButton.Click += (s, e) =>
                {
                    tcs.SetResult(false);
                    messageWindow.Close();
                };

            if (confirmButton != null)
                confirmButton.Click += (s, e) =>
                {
                    tcs.SetResult(true);
                    messageWindow.Close();
                };

            await messageWindow.ShowDialog(this);

            if (await tcs.Task)
            {
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        if (!cells[i, j].IsReadOnly)
                        {
                            cells[i, j].Text = "";
                        }
                    }
                }
            }
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            int[,] currentGrid = new int[9, 9];
            bool isComplete = true;

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

            bool isValid = ValidateSolution(currentGrid);

            var resultWindow = new Window
            {
                Title = isValid ? "正确" : "错误",
                Content = isValid ? "答案正确！" : "答案不正确，请继续努力！",
                Width = 250,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            resultWindow.ShowDialog(this);
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("开始提交答案...");
                
                // 获取当前答案
                int[,] currentGrid = new int[9, 9];
                bool isComplete = true;

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
                    await messageWindow.ShowDialog(this);
                    return;
                }

                // 验证答案
                bool isValid = ValidateSolution(currentGrid);

                if (isValid)
                {
                    gameStopwatch.Stop();
                    displayTimer.Stop();
                    
                    // 更新数据库中的完成状态
                    var participants = _databaseService.GetContestLeaderboard(_contestId);
                    var currentParticipant = participants.FirstOrDefault(p => p.UserId == _userId);
                    
                    if (currentParticipant != null)
                    {
                        int completedPuzzles = currentParticipant.CompletedPuzzles + 1;
                        int totalTime = currentParticipant.TotalTime + (int)gameStopwatch.Elapsed.TotalSeconds;
                        
                        Debug.WriteLine($"更新比赛进度 - 用户ID: {_userId}, 完成题目数: {completedPuzzles}, 总用时: {totalTime}秒");
                        _databaseService.UpdateContestParticipant(_contestId, _userId, completedPuzzles, totalTime);
                    }
                    else
                    {
                        Debug.WriteLine($"未找到用户 {_userId} 的比赛记录");
                    }

                    // 显示成功消息
                    var successWindow = new Window
                    {
                        Title = "成功",
                        Content = $"恭喜！你已经完成了这道题目！\n用时: {gameStopwatch.Elapsed.TotalSeconds:F1}秒",
                        Width = 300,
                        Height = 150,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    await successWindow.ShowDialog(this);

                    // 关闭当前窗口
                    Close();

                    // 刷新比赛视图
                    var mainWindow = this.GetVisualAncestors().OfType<MainWindow>().FirstOrDefault();
                    if (mainWindow != null)
                    {
                        Debug.WriteLine("重新创建比赛视图...");
                        var contestView = new ContestView(_contestId, _userId);
                        mainWindow.SetContent(contestView);
                    }
                    else
                    {
                        Debug.WriteLine("未找到主窗口");
                    }
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
                    await messageWindow.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"提交答案时出错: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
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
    }
} 