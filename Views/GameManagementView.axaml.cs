using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using SudokuGame.Models;
using SudokuGame.Services;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia;
using Avalonia.Layout;

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
        private bool _isLoading = false;
        private TextBlock _messageText;

        public GameManagementView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _databaseService = new DatabaseService();
            _puzzles = new ObservableCollection<SudokuPuzzle>();
            _users = new ObservableCollection<User>();
            _availablePuzzles = new ObservableCollection<SudokuPuzzle>();
            _selectedPuzzles = new ObservableCollection<SudokuPuzzle>();

            // 初始化消息文本块
            _messageText = this.FindControl<TextBlock>("MessageText");
            if (_messageText == null)
            {
                _messageText = new TextBlock
                {
                    Margin = new Thickness(10),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                var mainGrid = this.FindControl<Grid>("MainGrid");
                if (mainGrid != null)
                {
                    Grid.SetRow(_messageText, 1);
                    mainGrid.Children.Add(_messageText);
                }
            }

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

            // 异步加载数据
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                await Task.WhenAll(
                    LoadPuzzlesAsync(),
                    LoadUsersAsync(),
                    LoadAvailablePuzzlesAsync()
                );
            }
            catch (Exception ex)
            {
                ShowError($"加载数据失败: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadPuzzlesAsync()
        {
            await Task.Run(() =>
            {
                _puzzles.Clear();
            // TODO: 从数据库加载题目列表
            });
        }

        private async Task LoadUsersAsync()
        {
            await Task.Run(() =>
            {
                _users.Clear();
            // TODO: 从数据库加载用户列表
            });
        }

        private async Task LoadAvailablePuzzlesAsync()
        {
            if (_isLoading) return;

            var difficultyComboBox = this.FindControl<ComboBox>("PuzzleDifficultyComboBox");
            var selectedItem = difficultyComboBox?.SelectedItem as ComboBoxItem;
            string? selectedDifficulty = selectedItem?.Content?.ToString();

            if (selectedDifficulty == "全部难度")
            {
                selectedDifficulty = null;
            }

            await Task.Run(() =>
            {
                _availablePuzzles.Clear();
                var puzzles = _databaseService.GetAvailablePuzzles(selectedDifficulty);
                
                // 使用 Dispatcher 在 UI 线程上更新集合
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    foreach (var puzzle in puzzles)
                    {
                        _availablePuzzles.Add(puzzle);
                    }
                    UpdateSelectedPuzzlesCount();
                });
            });
        }

        private async void FilterPuzzlesButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await LoadAvailablePuzzlesAsync();
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

            // 验证是否选择了题目
            if (_selectedPuzzles.Count == 0)
            {
                ShowError("请至少选择一道题目");
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
                // 添加比赛题目
                var puzzleIds = _selectedPuzzles.Select(p => p.Id).ToList();
                if (_databaseService.AddPuzzlesToContest(contestId, puzzleIds))
                {
                    ShowSuccess("比赛创建成功!");
                    // 清空输入框
                    titleTextBox.Text = "";
                    descriptionTextBox.Text = "";
                    datePicker.SelectedDate = null;
                    timePicker.SelectedTime = null;
                    durationUpDown.Value = 60;
                    // 清空已选题目
                    ClearPuzzleSelectionButton_Click(null, new Avalonia.Interactivity.RoutedEventArgs());
                }
                else
                {
                    ShowError("比赛创建成功，但添加题目失败");
                }
            }
            else
            {
                ShowError(message);
            }
        }

        private void ShowError(string message)
        {
            if (_messageText != null)
            {
                _messageText.Text = message;
                _messageText.Foreground = Brushes.Red;
            }
        }

        private void ShowSuccess(string message)
        {
            if (_messageText != null)
            {
                _messageText.Text = message;
                _messageText.Foreground = Brushes.Green;
            }
        }

        private async void GeneratePuzzleButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var difficultyComboBox = this.FindControl<ComboBox>("DifficultyComboBox");
            var selectedDifficulty = ((ComboBoxItem)difficultyComboBox?.SelectedItem!)?.Content?.ToString() ?? "中等";

            var createWindow = new CreateOfficialPuzzleWindow(selectedDifficulty);
            await createWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
            
            // 刷新题目列表
            await LoadDataAsync();
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