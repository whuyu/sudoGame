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

            var (success, message) = _databaseService.JoinContest(contest.Id, _userId);
            
            var messageWindow = new Window
            {
                Title = success ? "成功" : "错误",
                Content = message,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            if (mainWindow != null)
            {
                await messageWindow.ShowDialog(mainWindow);
            }

            if (success)
            {
                // 打开比赛页面
                var contestView = new ContestView(contest.Id, _userId);
                if (mainWindow != null)
                {
                    mainWindow.Content = contestView;
                }
            }
        }
    }
} 