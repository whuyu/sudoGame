using Avalonia.Controls;
using System;
using SudokuGame.Models;
using Avalonia;
using Avalonia.Media;
using System.Diagnostics;
using Avalonia.Threading;

namespace SudokuGame.Views
{
    public partial class GameView : UserControl
    {
        private readonly int _userId;
        private SudokuPuzzle? _currentPuzzle;
        private TextBox[,] cells = new TextBox[9, 9];
        private Stopwatch gameStopwatch = new();
        private bool isGameStarted = false;
        private DispatcherTimer displayTimer;

        public GameView(int userId)
        {
            _userId = userId;
            InitializeComponent();
            InitializeGameGrid();
            SetupEventHandlers();
            SetupTimer();
        }

        private void InitializeGameGrid()
        {
            var sudokuGrid = this.FindControl<Grid>("SudokuGrid");
            if (sudokuGrid == null) return;

            sudokuGrid.RowDefinitions.Clear();
            sudokuGrid.ColumnDefinitions.Clear();
            sudokuGrid.Children.Clear();

            // 创建9x9的网格
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
                        BorderThickness = new Thickness(
                            j % 3 == 0 ? 2 : 0.5,
                            i % 3 == 0 ? 2 : 0.5,
                            j == 8 ? 2 : 0.5,
                            i == 8 ? 2 : 0.5
                        ),
                        BorderBrush = Brushes.Black,
                        Margin = new Thickness(0)
                    };

                    cells[i, j] = cell;
                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    sudokuGrid.Children.Add(cell);
                }
            }
        }

        private void SetupEventHandlers()
        {
            var startButton = this.FindControl<Button>("StartButton");
            var checkButton = this.FindControl<Button>("CheckButton");
            var restartButton = this.FindControl<Button>("RestartButton");

            if (startButton != null)
                startButton.Click += StartButton_Click;
            if (checkButton != null)
                checkButton.Click += CheckButton_Click;
            if (restartButton != null)
                restartButton.Click += RestartButton_Click;
        }

        private void SetupTimer()
        {
            displayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            displayTimer.Tick += DisplayTimer_Tick;
        }

        private void DisplayTimer_Tick(object? sender, EventArgs e)
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

        public void LoadPuzzle(SudokuPuzzle puzzle)
        {
            _currentPuzzle = puzzle;
            LoadPuzzleToGrid(puzzle.CurrentBoard);
            ResetGame();
        }

        private void LoadPuzzleToGrid(string board)
        {
            if (board.Length != 81) return;

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int index = i * 9 + j;
                    char value = board[index];
                    var cell = cells[i, j];

                    if (value != '0')
                    {
                        cell.Text = value.ToString();
                        cell.IsReadOnly = true;
                        cell.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                        cell.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                    }
                    else
                    {
                        cell.Text = "";
                        cell.IsReadOnly = false;
                        cell.Background = Brushes.White;
                        cell.Foreground = new SolidColorBrush(Color.FromRgb(64, 158, 255));
                    }
                }
            }
        }

        private void ResetGame()
        {
            isGameStarted = false;
            gameStopwatch.Reset();
            displayTimer.Stop();
            var timerDisplay = this.FindControl<TextBlock>("TimerDisplay");
            if (timerDisplay != null)
            {
                timerDisplay.Text = "00:00:000";
            }
            var startButton = this.FindControl<Button>("StartButton");
            if (startButton != null)
            {
                startButton.Content = "开始填写";
            }
            EnableAllCells(false);
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

        private void StartButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button)
            {
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
                    isGameStarted = false;
                    gameStopwatch.Stop();
                    displayTimer.Stop();
                    button.Content = "继续";
                    EnableAllCells(false);
                }
            }
        }

        private void CheckButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // TODO: 实现检查答案的逻辑
        }

        private void RestartButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_currentPuzzle != null)
            {
                LoadPuzzle(_currentPuzzle);
            }
        }
    }
} 