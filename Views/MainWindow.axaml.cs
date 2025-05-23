using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;

namespace SudokuGame.Views
{
    public partial class MainWindow : Window
    {
        private TextBox[,] cells = new TextBox[9, 9];
        private int[,] solution = new int[9, 9];
        private int[,] puzzle = new int[9, 9];
        private Random random = new Random();
        private bool isGameStarted = false;
        private Stopwatch gameStopwatch;
        private DispatcherTimer displayTimer;

        public MainWindow()
        {
            InitializeComponent();
            SetupGame();
            SetupEventHandlers();
        }

        private void SetupGame()
        {
            // 设置数独网格
            var sudokuGrid = this.FindControl<Grid>("SudokuGrid");
            
            // 创建9x9的网格定义
            for (int i = 0; i < 9; i++)
            {
                sudokuGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
                sudokuGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            }

            // 创建单元格
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    var cell = new TextBox
                    {
                        MaxLength = 1,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                        HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        FontSize = 24,
                        FontWeight = FontWeight.Bold,
                        Background = Brushes.White,
                        BorderThickness = new Thickness(
                            j % 3 == 0 ? 2 : 0.5,  // 左边框
                            i % 3 == 0 ? 2 : 0.5,  // 上边框
                            j == 8 ? 2 : 0.5,      // 右边框
                            i == 8 ? 2 : 0.5       // 下边框
                        ),
                        BorderBrush = Brushes.Black,
                        Margin = new Thickness(0)
                    };

                    // 设置输入验证
                    cell.TextInput += (s, e) =>
                    {
                        if (!char.IsDigit(e.Text[0]) || e.Text[0] == '0')
                        {
                            e.Handled = true;
                        }
                    };

                    cells[i, j] = cell;
                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    sudokuGrid.Children.Add(cell);
                }
            }

            // 设置计时器
            gameStopwatch = new Stopwatch();
            displayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            displayTimer.Tick += DisplayTimer_Tick;
        }

        private void SetupEventHandlers()
        {
            // 设置导航列表事件处理
            var navList = this.FindControl<ListBox>("NavList");
            navList.SelectionChanged += NavList_SelectionChanged;

            // 设置按钮事件处理
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

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var selectedItem = (ListBoxItem)listBox.SelectedItem;

            if (selectedItem?.Content.ToString() == "随机一题")
            {
                var gamePanel = this.FindControl<Grid>("GamePanel");
                if (gamePanel != null)
                {
                    gamePanel.IsVisible = true;
                    GenerateNewGame();
                }
            }
            else
            {
                var gamePanel = this.FindControl<Grid>("GamePanel");
                if (gamePanel != null)
                {
                    gamePanel.IsVisible = false;
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

        private void DisplayPuzzle()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    cells[i, j].Text = puzzle[i, j] == 0 ? "" : puzzle[i, j].ToString();
                    cells[i, j].IsReadOnly = puzzle[i, j] != 0;
                    cells[i, j].Background = cells[i, j].IsReadOnly ? 
                        new SolidColorBrush(Color.FromRgb(240, 240, 240)) : 
                        Brushes.White;
                    cells[i, j].Foreground = cells[i, j].IsReadOnly ? 
                        new SolidColorBrush(Color.FromRgb(51, 51, 51)) : 
                        new SolidColorBrush(Color.FromRgb(64, 158, 255));
                }
            }
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
    }
} 