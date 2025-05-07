using System;
using System.Drawing;
using System.Windows.Forms;

namespace SudokuGame
{
    public partial class MainForm : Form
    {
        private TextBox[,] cells = new TextBox[9, 9];
        private Button newGameButton;
        private Button checkButton;
        private Random random = new Random();

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
            GenerateNewGame();
        }

        private void InitializeComponent()
        {
            this.Text = "数独游戏";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupUI()
        {
            // 创建数独格子
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    cells[i, j] = new TextBox
                    {
                        Location = new Point(50 + j * 40, 50 + i * 40),
                        Size = new Size(35, 35),
                        MaxLength = 1,
                        TextAlign = HorizontalAlignment.Center,
                        Font = new Font("Arial", 16)
                    };

                    cells[i, j].KeyPress += Cell_KeyPress;
                    this.Controls.Add(cells[i, j]);
                }
            }

            // 创建按钮
            newGameButton = new Button
            {
                Text = "新游戏",
                Location = new Point(100, 450),
                Size = new Size(100, 40)
            };
            newGameButton.Click += NewGameButton_Click;
            this.Controls.Add(newGameButton);

            checkButton = new Button
            {
                Text = "检查",
                Location = new Point(250, 450),
                Size = new Size(100, 40)
            };
            checkButton.Click += CheckButton_Click;
            this.Controls.Add(checkButton);

            // 绘制分隔线
            this.Paint += MainForm_Paint;
        }

        private void Cell_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }
            else if (char.IsDigit(e.KeyChar) && e.KeyChar == '0')
            {
                e.Handled = true;
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Black, 2))
            {
                // 绘制粗线分隔
                for (int i = 0; i <= 9; i += 3)
                {
                    e.Graphics.DrawLine(pen, 48 + i * 40, 48, 48 + i * 40, 408);
                    e.Graphics.DrawLine(pen, 48, 48 + i * 40, 408, 48 + i * 40);
                }
            }
        }

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            GenerateNewGame();
        }

        private void CheckButton_Click(object sender, EventArgs e)
        {
            if (CheckSolution())
            {
                MessageBox.Show("恭喜！解答正确！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("解答不正确，请继续尝试。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void GenerateNewGame()
        {
            // 清空所有格子
            foreach (var cell in cells)
            {
                cell.Text = "";
                cell.ReadOnly = false;
            }

            // 生成一些初始数字
            for (int i = 0; i < 20; i++)
            {
                int row = random.Next(9);
                int col = random.Next(9);
                int num = random.Next(1, 10);

                while (!IsValidMove(row, col, num.ToString()) || !string.IsNullOrEmpty(cells[row, col].Text))
                {
                    row = random.Next(9);
                    col = random.Next(9);
                    num = random.Next(1, 10);
                }

                cells[row, col].Text = num.ToString();
                cells[row, col].ReadOnly = true;
                cells[row, col].BackColor = Color.LightGray;
            }
        }

        private bool IsValidMove(int row, int col, string num)
        {
            // 检查行
            for (int j = 0; j < 9; j++)
            {
                if (j != col && cells[row, j].Text == num)
                    return false;
            }

            // 检查列
            for (int i = 0; i < 9; i++)
            {
                if (i != row && cells[i, col].Text == num)
                    return false;
            }

            // 检查3x3方格
            int boxRow = row - row % 3;
            int boxCol = col - col % 3;
            for (int i = boxRow; i < boxRow + 3; i++)
            {
                for (int j = boxCol; j < boxCol + 3; j++)
                {
                    if ((i != row || j != col) && cells[i, j].Text == num)
                        return false;
                }
            }

            return true;
        }

        private bool CheckSolution()
        {
            // 检查是否所有格子都已填写
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (string.IsNullOrEmpty(cells[i, j].Text))
                        return false;
                }
            }

            // 检查每个数字的有效性
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    string currentNum = cells[i, j].Text;
                    cells[i, j].Text = "";
                    if (!IsValidMove(i, j, currentNum))
                    {
                        cells[i, j].Text = currentNum;
                        return false;
                    }
                    cells[i, j].Text = currentNum;
                }
            }

            return true;
        }
    }
} 