using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using SudokuGame.Models;
using SudokuGame.Services;

namespace SudokuGame.Views
{
    public partial class GameManagementView : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly int _userId;
        private readonly ObservableCollection<SudokuPuzzle> _puzzles;
        private readonly ObservableCollection<User> _users;

        public GameManagementView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _databaseService = new DatabaseService();
            _puzzles = new ObservableCollection<SudokuPuzzle>();
            _users = new ObservableCollection<User>();

            // 设置数据源
            var puzzleList = this.FindControl<ItemsControl>("PuzzleList");
            var userList = this.FindControl<ItemsControl>("UserList");
            if (puzzleList != null) puzzleList.ItemsSource = _puzzles;
            if (userList != null) userList.ItemsSource = _users;

            // 设置事件处理器
            var createContestButton = this.FindControl<Button>("CreateContestButton");
            var generatePuzzleButton = this.FindControl<Button>("GeneratePuzzleButton");
            if (createContestButton != null) createContestButton.Click += CreateContestButton_Click;
            if (generatePuzzleButton != null) generatePuzzleButton.Click += GeneratePuzzleButton_Click;

            // 加载数据
            LoadPuzzles();
            LoadUsers();
        }

        private void LoadPuzzles()
        {
            // TODO: 从数据库加载题目列表
            _puzzles.Clear();
            // var puzzles = _databaseService.GetAllPuzzles();
            // foreach (var puzzle in puzzles)
            // {
            //     _puzzles.Add(puzzle);
            // }
        }

        private void LoadUsers()
        {
            // TODO: 从数据库加载用户列表
            _users.Clear();
            // var users = _databaseService.GetAllUsers();
            // foreach (var user in users)
            // {
            //     _users.Add(user);
            // }
        }

        private void CreateContestButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // TODO: 实现创建比赛的逻辑
            var titleTextBox = this.FindControl<TextBox>("ContestTitleTextBox");
            var descriptionTextBox = this.FindControl<TextBox>("ContestDescriptionTextBox");
            var datePicker = this.FindControl<DatePicker>("ContestDatePicker");
            var timePicker = this.FindControl<TimePicker>("ContestTimePicker");
            var durationUpDown = this.FindControl<NumericUpDown>("ContestDurationUpDown");

            // 获取输入值并创建比赛
            // ...
        }

        private void GeneratePuzzleButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // TODO: 实现生成新题目的逻辑
            var difficultyComboBox = this.FindControl<ComboBox>("DifficultyComboBox");
            var selectedDifficulty = ((ComboBoxItem)difficultyComboBox?.SelectedItem!)?.Content?.ToString() ?? "中等";

            // 生成新题目
            // ...
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
} 