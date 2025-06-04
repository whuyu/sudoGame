using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SudokuGame.Models;
using SudokuGame.Services;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Diagnostics;
using Avalonia.Interactivity;

namespace SudokuGame.Views
{
    public partial class OfficialPuzzleView : UserControl
    {
        private readonly DatabaseService _databaseService;
        private List<SudokuPuzzle> _puzzles;
        private TextBlock _messageText;

        public event EventHandler<SudokuPuzzle>? OnPuzzleSelected;

        public OfficialPuzzleView()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _messageText = this.FindControl<TextBlock>("MessageText");
            LoadOfficialPuzzles();
        }

        private void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            // 从按钮的DataContext获取题目数据
            if (button.DataContext is SudokuPuzzle puzzle)
            {
                Debug.WriteLine($"正在加载官方题目，难度：{puzzle.Difficulty}");
                OnPuzzleSelected?.Invoke(this, puzzle);
            }
            else
            {
                Debug.WriteLine("无法获取题目数据");
            }
        }

        private void ShowMessage(string message, bool isError = true)
        {
            if (_messageText != null)
            {
                _messageText.Text = message;
                _messageText.Foreground = isError ? Brushes.Red : Brushes.Green;
            }
        }

        private void LoadOfficialPuzzles()
        {
            try
            {
                ShowMessage("正在加载题目...", false);
                
                // 获取所有官方题目
                _puzzles = _databaseService.GetOfficialPuzzles();
                
                if (_puzzles.Count == 0)
                {
                    ShowMessage("暂无官方题目");
                    return;
                }

                var puzzleList = this.FindControl<ItemsControl>("PuzzleList");
                if (puzzleList != null)
                {
                    puzzleList.ItemsSource = _puzzles;
                    ShowMessage("", false); // 清除消息
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"加载题目失败: {ex.Message}");
            }
        }
    }
} 