using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Linq;
using SudokuGame.Models;
using SudokuGame.Services;
using Avalonia.Input;

namespace SudokuGame.Views
{
    public partial class CreateOfficialPuzzleWindow : Window
    {
        private readonly TextBox[,] _cells = new TextBox[9, 9];
        private readonly DatabaseService _databaseService;
        private readonly string _difficulty;
        private readonly TextBlock _messageText;

        public CreateOfficialPuzzleWindow(string difficulty)
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _difficulty = difficulty;
            
            // 添加消息文本块
            _messageText = new TextBlock
            {
                Foreground = Brushes.Red,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null)
            {
                Grid.SetRow(_messageText, 2); // 在数独网格和按钮之间显示消息
                mainGrid.Children.Add(_messageText);
            }
            
            InitializeSudokuGrid();
        }

        private void ShowMessage(string message, bool isError = true)
        {
            _messageText.Foreground = isError ? Brushes.Red : Brushes.Green;
            _messageText.Text = message;
        }

        private void InitializeSudokuGrid()
        {
            var sudokuGrid = this.FindControl<Grid>("SudokuGrid");
            if (sudokuGrid == null) return;

            // 创建9x9的文本框网格
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    var cell = new TextBox
                    {
                        MaxLength = 1,
                        HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        FontSize = 24,
                        MinWidth = 40,
                        MinHeight = 40,
                        Margin = new Thickness(1),
                        Background = Brushes.White,
                        BorderThickness = new Thickness(1),
                        BorderBrush = Brushes.Gray
                    };

                    // 设置边框
                    if (col % 3 == 0) cell.BorderThickness = new Thickness(3, cell.BorderThickness.Top, cell.BorderThickness.Right, cell.BorderThickness.Bottom);
                    if (row % 3 == 0) cell.BorderThickness = new Thickness(cell.BorderThickness.Left, 3, cell.BorderThickness.Right, cell.BorderThickness.Bottom);
                    if ((col + 1) % 3 == 0) cell.BorderThickness = new Thickness(cell.BorderThickness.Left, cell.BorderThickness.Top, 3, cell.BorderThickness.Bottom);
                    if ((row + 1) % 3 == 0) cell.BorderThickness = new Thickness(cell.BorderThickness.Left, cell.BorderThickness.Top, cell.BorderThickness.Right, 3);

                    // 只允许输入数字1-9
                    cell.KeyDown += (s, e) =>
                    {
                        if (e.Key < Key.D1 || e.Key > Key.D9)
                        {
                            if (e.Key < Key.NumPad1 || e.Key > Key.NumPad9)
                            {
                                if (e.Key != Key.Back && e.Key != Key.Delete)
                                {
                                    e.Handled = true;
                                }
                            }
                        }
                    };

                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    sudokuGrid.Children.Add(cell);
                    _cells[row, col] = cell;
                }
            }
        }

        private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
        {
            // 清除之前的消息
            ShowMessage("");

            // 收集数独数据
            int[,] board = new int[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    string value = _cells[row, col].Text?.Trim() ?? "";
                    board[row, col] = string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
                }
            }

            // 验证数独是否有效
            if (!IsValidSudoku(board))
            {
                ShowMessage("数独题目无效，请检查是否符合规则");
                return;
            }

            // 验证数独是否有解
            if (!TrySolveSudoku(board, out int[,] solution))
            {
                ShowMessage("数独题目无解，请修改后重试");
                return;
            }

            // 创建新的数独题目
            var puzzle = new SudokuPuzzle
            {
                InitialBoard = ConvertBoardToString(board),
                CurrentBoard = ConvertBoardToString(board),
                Solution = ConvertBoardToString(solution),
                Difficulty = _difficulty,
                CreatedAt = DateTime.Now,
                LastPlayedAt = null,
                TotalPlayTime = TimeSpan.Zero,
                IsCompleted = false,
                IsOfficial = true
            };

            // 保存到数据库
            try
            {
                _databaseService.SavePuzzle(puzzle, 1, true); // 使用ID为1的系统管理员账号
                ShowMessage("官方题目创建成功！", false);
                Close();
            }
            catch (Exception ex)
            {
                ShowMessage($"保存题目失败：{ex.Message}");
            }
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool IsValidSudoku(int[,] board)
        {
            bool[] used = new bool[9];

            // 检查每一行
            for (int row = 0; row < 9; row++)
            {
                Array.Clear(used, 0, 9);
                for (int col = 0; col < 9; col++)
                {
                    int num = board[row, col];
                    if (num != 0)
                    {
                        if (used[num - 1]) return false;
                        used[num - 1] = true;
                    }
                }
            }

            // 检查每一列
            for (int col = 0; col < 9; col++)
            {
                Array.Clear(used, 0, 9);
                for (int row = 0; row < 9; row++)
                {
                    int num = board[row, col];
                    if (num != 0)
                    {
                        if (used[num - 1]) return false;
                        used[num - 1] = true;
                    }
                }
            }

            // 检查每个3x3方格
            for (int block = 0; block < 9; block++)
            {
                Array.Clear(used, 0, 9);
                int rowStart = (block / 3) * 3;
                int colStart = (block % 3) * 3;
                for (int i = 0; i < 9; i++)
                {
                    int row = rowStart + (i / 3);
                    int col = colStart + (i % 3);
                    int num = board[row, col];
                    if (num != 0)
                    {
                        if (used[num - 1]) return false;
                        used[num - 1] = true;
                    }
                }
            }

            return true;
        }

        private bool TrySolveSudoku(int[,] board, out int[,] solution)
        {
            solution = new int[9, 9];
            Array.Copy(board, solution, 81);
            return SolveSudokuHelper(solution);
        }

        private bool SolveSudokuHelper(int[,] board)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (board[row, col] == 0)
                    {
                        for (int num = 1; num <= 9; num++)
                        {
                            if (IsSafe(board, row, col, num))
                            {
                                board[row, col] = num;
                                if (SolveSudokuHelper(board))
                                    return true;
                                board[row, col] = 0;
                            }
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private bool IsSafe(int[,] board, int row, int col, int num)
        {
            // 检查行
            for (int x = 0; x < 9; x++)
                if (board[row, x] == num)
                    return false;

            // 检查列
            for (int x = 0; x < 9; x++)
                if (board[x, col] == num)
                    return false;

            // 检查3x3方格
            int startRow = row - row % 3;
            int startCol = col - col % 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[i + startRow, j + startCol] == num)
                        return false;

            return true;
        }

        private string ConvertBoardToString(int[,] board)
        {
            var chars = new char[81];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    chars[row * 9 + col] = board[row, col] == 0 ? '0' : (char)(board[row, col] + '0');
                }
            }
            return new string(chars);
        }
    }
} 