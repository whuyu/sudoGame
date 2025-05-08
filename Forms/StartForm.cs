using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace SudokuGame.Forms
{
    public partial class StartForm : Form
    {
        private Label titleLabel;
        private Button startButton;

        public StartForm()
        {
            InitializeComponent();
            SetupUI();
        }

        private void InitializeComponent()
        {
            this.Text = "数独游戏";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Paint += StartForm_Paint;
        }

        private void SetupUI()
        {
            // 创建标题标签
            titleLabel = new Label
            {
                Text = "数独游戏",
                Font = new Font("微软雅黑", 36, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 51, 51),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(400, 80),
                Location = new Point((this.ClientSize.Width - 400) / 2, 150)
            };
            this.Controls.Add(titleLabel);

            // 创建开始游戏按钮
            startButton = new Button
            {
                Text = "开始游戏",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                Size = new Size(200, 60),
                Location = new Point((this.ClientSize.Width - 200) / 2, 300),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 158, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            startButton.FlatAppearance.BorderSize = 0;
            startButton.MouseEnter += (s, e) => startButton.BackColor = Color.FromArgb(58, 142, 230);
            startButton.MouseLeave += (s, e) => startButton.BackColor = Color.FromArgb(64, 158, 255);
            startButton.Click += StartButton_Click;
            this.Controls.Add(startButton);

            // 添加版权信息
            Label copyrightLabel = new Label
            {
                Text = "© 2024 数独游戏",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.FromArgb(153, 153, 153),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(200, 30),
                Location = new Point((this.ClientSize.Width - 200) / 2, this.ClientSize.Height - 50)
            };
            this.Controls.Add(copyrightLabel);
        }

        private void StartForm_Paint(object sender, PaintEventArgs e)
        {
            // 使用抗锯齿
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制装饰性的数独网格背景
            using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
            {
                int gridSize = 30;
                for (int x = 0; x < this.Width; x += gridSize)
                {
                    e.Graphics.DrawLine(pen, x, 0, x, this.Height);
                }
                for (int y = 0; y < this.Height; y += gridSize)
                {
                    e.Graphics.DrawLine(pen, 0, y, this.Width, y);
                }
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            MainForm gameForm = new MainForm();
            gameForm.FormClosed += (s, args) => this.Close();
            gameForm.Show();
        }
    }
}