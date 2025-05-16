using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace SudokuGame.Forms
{
    public partial class MainForm : Form
    {
        private TextBox[,] cells = new TextBox[9, 9];
        private Button newGameButton;
        private Button checkButton;
        private Button startTimerButton;
        private Label timerLabel;
        private Panel timerPanel;
        private Label timerTitleLabel;
        private Stopwatch gameStopwatch;
        private System.Windows.Forms.Timer displayTimer;
        private int secondsElapsed;
        private Random random = new Random();
        private bool isGameStarted = false;
        private int[,] solution = new int[9, 9];
        private int[,] puzzle = new int[9, 9];
        private Panel gamePanel;
        private Panel controlPanel;
        private Label titleLabel;
        private const int CELL_SIZE = 60;  // 统一单元格大小
        private const int GRID_SIZE = 540; // 9 * CELL_SIZE
        private const int CELL_PADDING = 3;

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
            SetupTimer();
            GenerateNewGame();
        }

        private void InitializeComponent()
        {
            this.Text = "数独游戏";
            this.Size = new Size(1200, 1000);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("微软雅黑", 12F);
        }

        private void SetupUI()
        {
            // 添加标题
            titleLabel = new Label
            {
                Text = "数独游戏",
                Font = new Font("微软雅黑", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 51, 51),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(400, 60),
                Location = new Point((this.ClientSize.Width - 400) / 2, 20)
            };
            this.Controls.Add(titleLabel);

            // 创建游戏面板
            gamePanel = new Panel
            {
                Size = new Size(GRID_SIZE, GRID_SIZE),
                Location = new Point((this.ClientSize.Width - GRID_SIZE) / 2, 90),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            gamePanel.Paint += (s, e) => DrawGridLines(e.Graphics);
            this.Controls.Add(gamePanel);

            // 创建9x9的数独格子
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    cells[i, j] = new TextBox
                    {
                        Location = new Point(j * CELL_SIZE + CELL_PADDING, i * CELL_SIZE + CELL_PADDING),
                        Size = new Size(CELL_SIZE - CELL_PADDING * 2, CELL_SIZE - CELL_PADDING * 2),
                        MaxLength = 1,
                        TextAlign = HorizontalAlignment.Center,
                        Font = new Font("Arial", 24F, FontStyle.Bold),
                        BorderStyle = BorderStyle.None,
                        BackColor = Color.White,
                        ForeColor = Color.FromArgb(51, 51, 51)
                    };
                    gamePanel.Controls.Add(cells[i, j]);
                }
            }

            // 创建控制面板，放在计时器下方
            controlPanel = new Panel
            {
                Size = new Size(600, 80),
                Location = new Point((this.ClientSize.Width - 600) / 2, gamePanel.Bottom + 140), // 调整位置到计时器下方
                BackColor = Color.Transparent
            };
            this.Controls.Add(controlPanel);

            // 创建新游戏按钮
            newGameButton = CreateStyledButton("新游戏", 0);
            newGameButton.Click += (s, e) => GenerateNewGame();
            controlPanel.Controls.Add(newGameButton);

            // 创建检查按钮
            checkButton = CreateStyledButton("检查", 1);
            checkButton.Click += (s, e) => CheckSolution();
            controlPanel.Controls.Add(checkButton);

            // 创建开始按钮
            startTimerButton = CreateStyledButton("开始填写", 2);
            startTimerButton.Click += StartTimerButton_Click;
            controlPanel.Controls.Add(startTimerButton);
        }

        private Button CreateStyledButton(string text, int index)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(150, 45),
                Location = new Point(30 + index * 180, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                BackColor = Color.FromArgb(64, 158, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.MouseEnter += (s, e) => button.BackColor = Color.FromArgb(58, 142, 230);
            button.MouseLeave += (s, e) => button.BackColor = Color.FromArgb(64, 158, 255);
            return button;
        }

        private void DrawGridLines(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // 绘制细网格线
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1))
            {
                for (int i = 0; i <= 9; i++)
                {
                    g.DrawLine(pen, i * CELL_SIZE, 0, i * CELL_SIZE, GRID_SIZE);
                    g.DrawLine(pen, 0, i * CELL_SIZE, GRID_SIZE, i * CELL_SIZE);
                }
            }

            // 绘制粗网格线
            using (var pen = new Pen(Color.FromArgb(51, 51, 51), 2))
            {
                for (int i = 0; i <= 3; i++)
                {
                    g.DrawLine(pen, i * (CELL_SIZE * 3), 0, i * (CELL_SIZE * 3), GRID_SIZE);
                    g.DrawLine(pen, 0, i * (CELL_SIZE * 3), GRID_SIZE, i * (CELL_SIZE * 3));
                }
            }
        }

        private void SetupTimer()
        {
            // 创建计时器面板
            timerPanel = new Panel
            {
                Size = new Size(300, 100),
                Location = new Point((this.ClientSize.Width - 300) / 2, gamePanel.Bottom + 20), // 相对于游戏面板定位
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            timerPanel.Paint += TimerPanel_Paint;
            this.Controls.Add(timerPanel);

            // 添加计时器标题
            timerTitleLabel = new Label
            {
                Text = "用时",
                Font = new Font("微软雅黑", 12F),
                ForeColor = Color.FromArgb(102, 102, 102),
                Size = new Size(300, 30),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            timerPanel.Controls.Add(timerTitleLabel);

            // 创建计时显示标签
            timerLabel = new Label
            {
                Text = "00:00:000",
                Font = new Font("Consolas", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 158, 255),
                Size = new Size(300, 50),
                Location = new Point(0, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            timerPanel.Controls.Add(timerLabel);

            // 创建高精度计时器
            gameStopwatch = new Stopwatch();
            
            // 创建显示更新计时器（100Hz刷新率）
            displayTimer = new System.Windows.Forms.Timer
            {
                Interval = 10 // 10ms，即0.01秒
            };
            displayTimer.Tick += DisplayTimer_Tick;
        }

        private void TimerPanel_Paint(object sender, PaintEventArgs e)
        {
            using (var path = new GraphicsPath())
            {
                var rect = new Rectangle(0, 0, timerPanel.Width - 1, timerPanel.Height - 1);
                int radius = 10;
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // 绘制阴影
                using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                {
                    e.Graphics.TranslateTransform(2, 2);
                    e.Graphics.FillPath(shadowBrush, path);
                    e.Graphics.TranslateTransform(-2, -2);
                }

                // 绘制背景
                e.Graphics.FillPath(Brushes.White, path);
                
                // 绘制边框
                using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            if (gameStopwatch.IsRunning)
            {
                TimeSpan elapsed = gameStopwatch.Elapsed;
                timerLabel.Text = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}:{elapsed.Milliseconds:D3}";
            }
        }

        private void StartTimerButton_Click(object sender, EventArgs e)
        {
            if (!isGameStarted)
            {
                isGameStarted = true;
                gameStopwatch.Start();
                displayTimer.Start();
                startTimerButton.Text = "暂停";
                EnableAllCells(true);
            }
            else
            {
                gameStopwatch.Stop();
                displayTimer.Stop();
                startTimerButton.Text = "继续";
                isGameStarted = false;
                EnableAllCells(false);
            }
        }

        private void EnableAllCells(bool enable)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (!cells[i, j].ReadOnly)
                    {
                        cells[i, j].Enabled = enable;
                    }
                }
            }
        }

        private void GenerateNewGame()
        {
            gameStopwatch.Reset();
            displayTimer.Stop();
            timerLabel.Text = "00:00:000";
            isGameStarted = false;
            startTimerButton.Text = "开始填写";

            GenerateSudoku();
            DisplayPuzzle();
            EnableAllCells(false);
        }

        private void GenerateSudoku()
        {
            Array.Clear(solution, 0, solution.Length);
            Array.Clear(puzzle, 0, puzzle.Length);
            GenerateSolution(0, 0);
            Array.Copy(solution, puzzle, solution.Length);

            int cellsToRemove = 40;
            while (cellsToRemove > 0)
            {
                int row = random.Next(9);
                int col = random.Next(9);
                if (puzzle[row, col] != 0)
                {
                    puzzle[row, col] = 0;
                    cellsToRemove--;
                }
            }
        }

        private bool GenerateSolution(int row, int col)
        {
            if (col >= 9)
            {
                row++;
                col = 0;
            }
            if (row >= 9)
                return true;

            var numbers = Enumerable.Range(1, 9).ToList();
            while (numbers.Count > 0)
            {
                int index = random.Next(numbers.Count);
                int num = numbers[index];
                numbers.RemoveAt(index);

                if (IsValid(row, col, num))
                {
                    solution[row, col] = num;
                    if (GenerateSolution(row, col + 1))
                        return true;
                    solution[row, col] = 0;
                }
            }
            return false;
        }

        private bool IsValid(int row, int col, int num)
        {
            for (int x = 0; x < 9; x++)
                if (solution[row, x] == num || solution[x, col] == num)
                    return false;

            int startRow = row - row % 3, startCol = col - col % 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (solution[i + startRow, j + startCol] == num)
                        return false;

            return true;
        }

        private void DisplayPuzzle()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    cells[i, j].Text = puzzle[i, j] == 0 ? "" : puzzle[i, j].ToString();
                    cells[i, j].ReadOnly = puzzle[i, j] != 0;
                    cells[i, j].BackColor = cells[i, j].ReadOnly ? Color.FromArgb(245, 245, 245) : Color.White;
                    cells[i, j].ForeColor = cells[i, j].ReadOnly ? Color.FromArgb(51, 51, 51) : Color.FromArgb(64, 158, 255);

                    // 移除之前的事件处理器
                    cells[i, j].KeyPress -= Cell_KeyPress;

                    // 添加新的事件处理器
                    if (!cells[i, j].ReadOnly)
                    {
                        cells[i, j].KeyPress += Cell_KeyPress;
                    }
                }
            }
        }

        private void Cell_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
                return;
            }
            if (char.IsDigit(e.KeyChar))
            {
                int value = int.Parse(e.KeyChar.ToString());
                if (value == 0)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void CheckSolution()
        {
            int[,] currentGrid = new int[9, 9];
            bool isComplete = true;

            // 收集当前输入到网格中
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (string.IsNullOrEmpty(cells[i, j].Text))
                    {
                        isComplete = false;
                        break;
                    }
                    currentGrid[i, j] = int.Parse(cells[i, j].Text);
                }
                if (!isComplete) break;
            }

            if (!isComplete)
            {
                MessageBox.Show("还没有完成填写！", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 验证答案是否符合数独规则
            bool isValid = true;
            
            // 检查行
            for (int row = 0; row < 9 && isValid; row++)
            {
                bool[] used = new bool[10];
                for (int col = 0; col < 9; col++)
                {
                    int num = currentGrid[row, col];
                    if (num < 1 || num > 9 || used[num])
                    {
                        isValid = false;
                        break;
                    }
                    used[num] = true;
                }
            }

            // 检查列
            for (int col = 0; col < 9 && isValid; col++)
            {
                bool[] used = new bool[10];
                for (int row = 0; row < 9; row++)
                {
                    int num = currentGrid[row, col];
                    if (used[num])
                    {
                        isValid = false;
                        break;
                    }
                    used[num] = true;
                }
            }

            // 检查3x3方块
            for (int block = 0; block < 9 && isValid; block++)
            {
                bool[] used = new bool[10];
                int rowStart = (block / 3) * 3;
                int colStart = (block % 3) * 3;
                
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int num = currentGrid[rowStart + i, colStart + j];
                        if (used[num])
                        {
                            isValid = false;
                            break;
                        }
                        used[num] = true;
                    }
                }
            }

            // 检查是否保持了原始数字不变
            bool maintainsOriginal = true;
            for (int i = 0; i < 9 && maintainsOriginal; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (puzzle[i, j] != 0 && puzzle[i, j] != currentGrid[i, j])
                    {
                        maintainsOriginal = false;
                        break;
                    }
                }
            }

            if (!maintainsOriginal)
            {
                MessageBox.Show("你修改了题目中的原始数字！", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isValid)
            {
                gameStopwatch.Stop();
                displayTimer.Stop();
                MessageBox.Show($"恭喜！你已经完成了数独！\n用时: {timerLabel.Text}", "成功", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("答案不正确，请继续努力！", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}