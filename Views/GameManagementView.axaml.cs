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
        private readonly ObservableCollection<SudokuPuzzle> _availablePuzzles;
        private readonly ObservableCollection<SudokuPuzzle> _selectedPuzzles;

        public GameManagementView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _databaseService = new DatabaseService();
            _puzzles = new ObservableCollection<SudokuPuzzle>();
            _users = new ObservableCollection<User>();
            _availablePuzzles = new ObservableCollection<SudokuPuzzle>();
            _selectedPuzzles = new ObservableCollection<SudokuPuzzle>();

            // 设置数据源
            var puzzleList = this.FindControl<ItemsControl>("PuzzleList");
            var userList = this.FindControl<ItemsControl>("UserList");
            var availablePuzzlesList = this.FindControl<ListBox>("AvailablePuzzlesList");
            if (puzzleList != null) puzzleList.ItemsSource = _puzzles;
            if (userList != null) userList.ItemsSource = _users;
            if (availablePuzzlesList != null)
            {
                availablePuzzlesList.ItemsSource = _availablePuzzles;
                availablePuzzlesList.SelectionChanged += AvailablePuzzlesList_SelectionChanged;
            }

            // 设置事件处理器
            var createContestButton = this.FindControl<Button>("CreateContestButton");
            var generatePuzzleButton = this.FindControl<Button>("GeneratePuzzleButton");
            var filterPuzzlesButton = this.FindControl<Button>("FilterPuzzlesButton");
            var clearPuzzleSelectionButton = this.FindControl<Button>("ClearPuzzleSelectionButton");

            if (createContestButton != null) createContestButton.Click += CreateContestButton_Click;
            if (generatePuzzleButton != null) generatePuzzleButton.Click += GeneratePuzzleButton_Click;
            if (filterPuzzlesButton != null) filterPuzzlesButton.Click += FilterPuzzlesButton_Click;
            if (clearPuzzleSelectionButton != null) clearPuzzleSelectionButton.Click += ClearPuzzleSelectionButton_Click;

            // 加载数据
            LoadPuzzles();
            LoadUsers();
            LoadAvailablePuzzles();
        }

        private void LoadPuzzles()
        {
            // TODO: 从数据库加载题目列表
            _puzzles.Clear();
        }

        private void LoadUsers()
        {
            // TODO: 从数据库加载用户列表
            _users.Clear();
        }

        private void LoadAvailablePuzzles()
        {
            // TODO: 从数据库加载可选题目列表
            _availablePuzzles.Clear();
            UpdateSelectedPuzzlesCount();
        }

        private void FilterPuzzlesButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // TODO: 根据选择的难度筛选题目
            var difficultyComboBox = this.FindControl<ComboBox>("PuzzleDifficultyComboBox");
            var selectedDifficulty = ((ComboBoxItem)difficultyComboBox?.SelectedItem!)?.Content?.ToString();
            LoadAvailablePuzzles();
        }

        private void AvailablePuzzlesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // TODO: 处理题目选择变化
            if (sender is ListBox listBox)
            {
                foreach (SudokuPuzzle puzzle in e.AddedItems)
                {
                    if (!_selectedPuzzles.Contains(puzzle))
                    {
                        _selectedPuzzles.Add(puzzle);
                    }
                }

                foreach (SudokuPuzzle puzzle in e.RemovedItems)
                {
                    _selectedPuzzles.Remove(puzzle);
                }

                UpdateSelectedPuzzlesCount();
            }
        }

        private void ClearPuzzleSelectionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // TODO: 清空已选题目
            var availablePuzzlesList = this.FindControl<ListBox>("AvailablePuzzlesList");
            if (availablePuzzlesList != null)
            {
                availablePuzzlesList.SelectedItems.Clear();
            }
            _selectedPuzzles.Clear();
            UpdateSelectedPuzzlesCount();
        }

        private void UpdateSelectedPuzzlesCount()
        {
            var selectedPuzzlesCount = this.FindControl<TextBlock>("SelectedPuzzlesCount");
            if (selectedPuzzlesCount != null)
            {
                selectedPuzzlesCount.Text = $"已选择题目数：{_selectedPuzzles.Count}";
            }
        }

        private void CreateContestButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var titleTextBox = this.FindControl<TextBox>("ContestTitleTextBox");
            var descriptionTextBox = this.FindControl<TextBox>("ContestDescriptionTextBox");
            var datePicker = this.FindControl<DatePicker>("ContestDatePicker");
            var timePicker = this.FindControl<TimePicker>("ContestTimePicker");
            var durationUpDown = this.FindControl<NumericUpDown>("ContestDurationUpDown");

            if (titleTextBox == null || descriptionTextBox == null || datePicker == null || 
                timePicker == null || durationUpDown == null)
            {
                ShowError("界面控件初始化失败");
                return;
            }

            // 验证输入
            if (string.IsNullOrWhiteSpace(titleTextBox.Text))
            {
                ShowError("请输入比赛标题");
                return;
            }

            if (!datePicker.SelectedDate.HasValue)
            {
                ShowError("请选择比赛日期");
                return;
            }

            if (!timePicker.SelectedTime.HasValue)
            {
                ShowError("请选择比赛时间");
                return;
            }

            // 组合日期和时间
            var startDate = datePicker.SelectedDate.Value;
            var startTime = timePicker.SelectedTime.Value;
            var startDateTime = startDate.Date.Add(startTime);

            // 获取比赛时长（分钟）
            int duration = (int)durationUpDown.Value;

            // 创建比赛
            var (success, message, contestId) = _databaseService.AddContest(
                titleTextBox.Text,
                descriptionTextBox.Text ?? "",
                startDateTime,
                duration
            );

            if (success)
            {
                ShowSuccess("比赛创建成功");
                // 清空输入框
                titleTextBox.Text = "";
                descriptionTextBox.Text = "";
                datePicker.SelectedDate = null;
                timePicker.SelectedTime = null;
                durationUpDown.Value = 60;
            }
            else
            {
                ShowError(message);
            }
        }

        private void ShowError(string message)
        {
            // TODO: 实现错误提示，可以使用对话框或其他UI元素
            Console.WriteLine($"Error: {message}");
        }

        private void ShowSuccess(string message)
        {
            // TODO: 实现成功提示，可以使用对话框或其他UI元素
            Console.WriteLine($"Success: {message}");
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