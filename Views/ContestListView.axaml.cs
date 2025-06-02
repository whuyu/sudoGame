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
    public partial class ContestListView : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly int _userId;
        private DispatcherTimer _refreshTimer;

        public ContestListView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _databaseService = new DatabaseService();

            // 设置定时刷新
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            LoadContests();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadContests();
        }

        private void LoadContests()
        {
            var contests = _databaseService.GetContests();
            
            // 检查用户是否已加入每个比赛，但不自动加入
            foreach (var contest in contests)
            {
                // 只检查用户是否已加入比赛
                contest.HasJoined = _databaseService.HasUserJoinedContest(contest.Id, _userId);
            }
            
            var contestList = this.FindControl<ItemsControl>("ContestList");
            if (contestList != null)
            {
                contestList.ItemsSource = contests;
            }
        }

        private async void JoinContest_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var contest = (Contest)button.DataContext;

            var mainWindow = this.FindAncestorOfType<MainWindow>();
            if (mainWindow == null) return;

            ContestView contestView;
            if (contest.HasJoined)
            {
                // 直接打开比赛页面
                contestView = new ContestView(contest.Id, _userId);
                mainWindow.SetContent(contestView);
                return;
            }

            // 尝试加入比赛
            var (success, message) = _databaseService.JoinContest(contest.Id, _userId);
            
            if (!success)
            {
                var messageWindow = new Window
                {
                    Title = "错误",
                    Content = message,
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                
                await messageWindow.ShowDialog(mainWindow);
                return;
            }

            // 加入成功后更新状态
            contest.HasJoined = true;

            // 打开比赛页面
            contestView = new ContestView(contest.Id, _userId);
            mainWindow.SetContent(contestView);
            
            // 刷新列表
            LoadContests();
        }
    }
} 