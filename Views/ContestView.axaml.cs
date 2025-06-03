using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using SudokuGame.Models;
using SudokuGame.Services;
using System.Diagnostics;

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
            try
            {
                InitializeComponent();
                _contestId = contestId;
                _userId = userId;
                _databaseService = new DatabaseService();

                Debug.WriteLine($"初始化比赛视图 - 比赛ID: {contestId}, 用户ID: {userId}");

                LoadContest();
                SetupTimer();
                LoadPuzzles();
                LoadLeaderboard();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"比赛视图初始化失败: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                throw;
            }
        }

        private void LoadContest()
        {
            try
            {
                _contest = _databaseService.GetContest(_contestId);
                if (_contest != null)
                {
                    var titleBlock = this.FindControl<TextBlock>("ContestTitle");
                    var descBlock = this.FindControl<TextBlock>("ContestDescription");
                    if (titleBlock != null) titleBlock.Text = _contest.Title;
                    if (descBlock != null) descBlock.Text = _contest.Description;

                    _endTime = _contest.StartTime.AddMinutes(_contest.Duration);
                    Debug.WriteLine($"比赛信息加载成功 - 标题: {_contest.Title}");
                }
                else
                {
                    Debug.WriteLine($"未找到比赛信息 - 比赛ID: {_contestId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载比赛信息失败: {ex.Message}");
                throw;
            }
        }

        private void SetupTimer()
        {
            try
            {
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _timer.Tick += Timer_Tick;
                _timer.Start();
                Debug.WriteLine("计时器设置成功");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置计时器失败: {ex.Message}");
                throw;
            }
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

            // 每秒刷新一次排行榜
            if (DateTime.Now.Second % 1 == 0)
            {
                LoadLeaderboard();
            }
        }

        private async void LoadPuzzles()
        {
            try
            {
                Debug.WriteLine("开始加载比赛题目...");
                var puzzles = await _databaseService.GetContestPuzzles(_contestId);
                Debug.WriteLine($"获取到 {puzzles.Count} 个题目");

                var puzzleItems = puzzles.Select((puzzle, index) => new PuzzleItem
                {
                    Title = $"题目 {index + 1}",
                    Status = "未完成",
                    Puzzle = puzzle,
                    Index = index
                }).ToList();

                var puzzleList = this.FindControl<ItemsControl>("PuzzleList");
                if (puzzleList != null)
                {
                    puzzleList.ItemsSource = puzzleItems;
                    Debug.WriteLine("题目列表更新成功");
                }
                else
                {
                    Debug.WriteLine("未找到题目列表控件");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载题目失败: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        private void LoadLeaderboard()
        {
            try
            {
                Debug.WriteLine("开始加载排行榜...");
                var participants = _databaseService.GetContestLeaderboard(_contestId);
                Debug.WriteLine($"获取到 {participants.Count} 个参赛者");

                var leaderboardList = this.FindControl<ItemsControl>("LeaderboardList");
                if (leaderboardList != null)
                {
                    var leaderboardData = participants
                        .OrderByDescending(x => x.CompletedPuzzles)
                        .ThenBy(x => x.TotalTime)
                        .Select((p, i) => new
                        {
                            Rank = i + 1,
                            p.Username,
                            p.CompletedPuzzles,
                            TotalTimeStr = TimeSpan.FromSeconds(p.TotalTime).ToString(@"hh\:mm\:ss"),
                            p.TotalTime,
                            JoinTimeStr = p.JoinTime.ToString("MM-dd HH:mm")
                        })
                        .ToList();

                    Debug.WriteLine($"排行榜数据: {string.Join(", ", leaderboardData.Select(d => $"排名:{d.Rank} 用户:{d.Username} 完成:{d.CompletedPuzzles} 用时:{d.TotalTimeStr} 加入时间:{d.JoinTimeStr}"))}");
                    
                    leaderboardList.ItemsSource = leaderboardData;
                    Debug.WriteLine($"排行榜更新成功 - 显示 {leaderboardData.Count} 条记录");
                }
                else
                {
                    Debug.WriteLine("未找到排行榜控件");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载排行榜失败: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        private void Puzzle_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Puzzle_Click 被触发");
            
            if (sender is Button button)
            {
                Debug.WriteLine($"按钮的 DataContext 类型: {button.DataContext?.GetType().Name}");
                
                if (button.DataContext is PuzzleItem puzzleItem)
                {
                    Debug.WriteLine($"题目索引: {puzzleItem.Index}");
                    Debug.WriteLine($"题目标题: {puzzleItem.Title}");
                    
                    if (puzzleItem.Puzzle != null)
                    {
                        var window = new ContestPuzzleWindow(_contestId, _userId, puzzleItem.Index, puzzleItem.Puzzle);
                        window.Show();
                    }
                    else
                    {
                        Debug.WriteLine("题目数据为空");
                    }
                }
                else
                {
                    Debug.WriteLine("DataContext 不是 PuzzleItem 类型");
                }
            }
            else
            {
                Debug.WriteLine("发送者不是按钮");
            }
        }

        private class PuzzleItem
        {
            public string Title { get; set; } = "";
            public string Status { get; set; } = "";
            public SudokuPuzzle Puzzle { get; set; } = null!;
            public int Index { get; set; }
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