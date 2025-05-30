using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SudokuGame.Models;
using SudokuGame.Services;
using Avalonia;
using Avalonia.VisualTree;
using System.Diagnostics;

namespace SudokuGame.Views
{
    public partial class MyPuzzlesView : UserControl
    {
        private readonly ItemsControl puzzlesItemsControl;
        private readonly ObservableCollection<SudokuPuzzle> puzzles;
        private readonly int _userId;
        private readonly DatabaseService _databaseService;
        private readonly SudokuGenerator _sudokuGenerator;
        private readonly ComboBox _difficultyComboBox;
        private readonly Button _createButton;

        // 定义一个事件来通知需要切换到游戏页面
        public event EventHandler<SudokuPuzzle>? OnPuzzleSelected;

        // 添加无参数的公共构造函数
        public MyPuzzlesView()
        {
            InitializeComponent();
        }

        public MyPuzzlesView(int userId)
        {
            InitializeComponent();
            
            _userId = userId;
            puzzles = new ObservableCollection<SudokuPuzzle>();
            
            // 初始化控件引用
            puzzlesItemsControl = this.FindControl<ItemsControl>("PuzzlesItemsControl") ?? throw new InvalidOperationException("PuzzlesItemsControl not found");
            _difficultyComboBox = this.FindControl<ComboBox>("DifficultyComboBox") ?? throw new InvalidOperationException("DifficultyComboBox not found");
            _createButton = this.FindControl<Button>("CreateButton") ?? throw new InvalidOperationException("CreateButton not found");
            
            // 设置数据源
            puzzlesItemsControl.ItemsSource = puzzles;
            
            // 初始化服务
            _databaseService = new DatabaseService();
            _sudokuGenerator = new SudokuGenerator();

            // 设置事件处理器
            _createButton.Click += CreateButton_Click;

            // 加载题目列表
            LoadPuzzles();
        }

        private void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 获取选择的难度
                string difficulty = "普通"; // 默认难度
                if (_difficultyComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    difficulty = selectedItem.Content?.ToString() ?? "普通";
                }

                // 生成新的数独题目
                var (initialBoard, solution) = _sudokuGenerator.GeneratePuzzle(difficulty);

                // 创建新的数独题目对象
                var newPuzzle = new SudokuPuzzle
                {
                    UserId = _userId,
                    InitialBoard = initialBoard,
                    CurrentBoard = initialBoard, // 初始时当前状态与初始状态相同
                    Solution = solution,
                    Difficulty = difficulty,
                    CreatedAt = DateTime.Now,
                    LastPlayedAt = null,
                    TotalPlayTime = TimeSpan.Zero,
                    IsCompleted = false
                };

                // 保存到数据库
                _databaseService.SavePuzzle(newPuzzle);

                // 刷新列表
                LoadPuzzles();

                // 显示成功消息
                var messageWindow = new Window
                {
                    Title = "成功",
                    Content = "新题目已创建",
                    Width = 200,
                    Height = 100,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
            }
            catch (Exception ex)
            {
                // 显示错误消息
                var messageWindow = new Window
                {
                    Title = "错误",
                    Content = $"创建题目失败: {ex.Message}",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                messageWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
            }
        }

        private void LoadPuzzles()
        {
            try
            {
                var userPuzzles = _databaseService.GetUserPuzzles(_userId);
                puzzles.Clear();
                foreach (var puzzle in userPuzzles)
                {
                    puzzles.Add(puzzle);
                }
            }
            catch (Exception ex)
            {
                // 这里可以添加错误处理逻辑
                Console.WriteLine($"加载题目失败: {ex.Message}");
            }
        }

        // 这个方法用于刷新题目列表
        public void RefreshPuzzles()
        {
            LoadPuzzles();
        }

        private void ContinueButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            // 从按钮的Tag属性获取题目数据
            if (button.Tag is SudokuPuzzle puzzle)
            {
                Debug.WriteLine($"正在加载题目，难度：{puzzle.Difficulty}");
                OnPuzzleSelected?.Invoke(this, puzzle);
            }
            else
            {
                Debug.WriteLine("无法获取题目数据");
            }
        }

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            // 获取要删除的题目
            SudokuPuzzle? puzzleToDelete = null;
            if (button.DataContext is SudokuPuzzle puzzle)
            {
                puzzleToDelete = puzzle;
            }
            else
            {
                var parentBorder = button.Parent?.Parent as Border;
                if (parentBorder?.DataContext is SudokuPuzzle parentPuzzle)
                {
                    puzzleToDelete = parentPuzzle;
                }
            }

            if (puzzleToDelete == null)
                return;

            // 显示确认对话框
            var messageWindow = new Window
            {
                Title = "确认删除",
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "确定要删除这个题目吗？",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Spacing = 20,
                            Children =
                            {
                                new Button
                                {
                                    Content = "确定",
                                    Width = 80,
                                    Height = 30,
                                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(244, 67, 54)),
                                    Foreground = Avalonia.Media.Brushes.White
                                },
                                new Button
                                {
                                    Content = "取消",
                                    Width = 80,
                                    Height = 30
                                }
                            }
                        }
                    }
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var confirmButton = ((messageWindow.Content as StackPanel)?.Children[1] as StackPanel)?.Children[0] as Button;
            var cancelButton = ((messageWindow.Content as StackPanel)?.Children[1] as StackPanel)?.Children[1] as Button;

            bool? result = null;
            if (confirmButton != null && cancelButton != null)
            {
                confirmButton.Click += (s, e) =>
                {
                    result = true;
                    messageWindow.Close();
                };
                cancelButton.Click += (s, e) =>
                {
                    result = false;
                    messageWindow.Close();
                };
            }

            // 显示对话框
            await messageWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);

            // 如果用户确认删除
            if (result == true)
            {
                try
                {
                    // 从数据库中删除
                    _databaseService.DeletePuzzle(puzzleToDelete.Id);
                    // 从列表中移除
                    puzzles.Remove(puzzleToDelete);

                    
                }
                catch (Exception ex)
                {
                    // 显示错误消息
                    var errorWindow = new Window
                    {
                        Title = "错误",
                        Content = $"删除失败: {ex.Message}",
                        Width = 300,
                        Height = 150,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    await errorWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
                }
            }
        }
    }
} 