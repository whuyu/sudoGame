using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using SudokuGame.Models;
using SudokuGame.Services;

namespace SudokuGame.Views
{
    public partial class ContestView : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly int _contestId;
        private readonly int _userId;
        private Contest _contest;
        private DispatcherTimer _timer;
        private DateTime _endTime;

        public ContestView(int contestId, int userId)
        {
            InitializeComponent();
            _contestId = contestId;
            _userId = userId;
            _databaseService = new DatabaseService();

            LoadContest();
            SetupTimer();
            LoadPuzzles();
            LoadLeaderboard();
        }

        private void LoadContest()
        {
            _contest = _databaseService.GetContest(_contestId);
            if (_contest != null)
            {
                var titleBlock = this.FindControl<TextBlock>("ContestTitle");
                var descBlock = this.FindControl<TextBlock>("ContestDescription");
                if (titleBlock != null) titleBlock.Text = _contest.Title;
                if (descBlock != null) descBlock.Text = _contest.Description;

                _endTime = _contest.StartTime.AddMinutes(_contest.Duration);
            }
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var remaining = _endTime - DateTime.Now;
            var timeBlock = this.FindControl<TextBlock>("ContestTime");
            
            if (remaining.TotalSeconds <= 0)
            {
                _timer.Stop();
                if (timeBlock != null) timeBlock.Text = "比赛已结束";
                return;
            }

            if (timeBlock != null)
            {
                timeBlock.Text = $"剩余时间：{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        private void LoadPuzzles()
        {
            var puzzles = _databaseService.GetContestPuzzles(_contestId);
            var puzzleList = this.FindControl<ItemsControl>("PuzzleList");
            if (puzzleList != null)
            {
                var puzzleItems = puzzles.Select((p, i) => new
                {
                    Title = $"题目 {i + 1}",
                    Status = p.IsCompleted ? "已完成" : "未完成",
                    Puzzle = p
                }).ToList();

                puzzleList.ItemsSource = puzzleItems;
            }
        }

        private void LoadLeaderboard()
        {
            var participants = _databaseService.GetContestLeaderboard(_contestId);
            var leaderboardList = this.FindControl<ItemsControl>("LeaderboardList");
            if (leaderboardList != null)
            {
                leaderboardList.ItemsSource = participants.Select((p, i) => new
                {
                    Rank = i + 1,
                    p.Username,
                    p.CompletedPuzzles,
                    p.TotalTime
                });
            }
        }

        private async void Puzzle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SudokuPuzzle puzzle)
            {
                var mainWindow = this.FindAncestorOfType<MainWindow>();
                if (mainWindow != null)
                {
                    var gameView = new GameView(_userId);
                    gameView.LoadPuzzle(puzzle);
                    mainWindow.Content = gameView;
                }
            }
        }

        private void BackToList_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            if (mainWindow != null)
            {
                var contestListView = new ContestListView(_userId);
                mainWindow.SetContent(contestListView);
            }
        }
    }
} 