using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace SudokuGame.Forms
{
    public partial class StartForm : Form
    {
        private Label titleLabel;
        private Button startButton;
        
        // 基准尺寸（设计时的窗体大小）
        private readonly Size baseSize = new Size(1000, 800);
        // 原始字体大小
        private float baseTitleFontSize = 46f;
        private float baseButtonFontSize = 18f;

        public StartForm()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Load += StartForm_Load;
            this.Resize += (s, e) => UpdateFontScaling();
            SetupUI();
        }

        private void StartForm_Load(object sender, EventArgs e)
        {
            // 记录控件原始字体
            titleLabel.Tag = titleLabel.Font;
            startButton.Tag = startButton.Font;
            UpdateFontScaling();
        }

        private void UpdateFontScaling()
        {
            // 计算缩放比例（取宽高比例较小值保持内容完整）
            float scaleX = (float)this.ClientSize.Width / baseSize.Width;
            float scaleY = (float)this.ClientSize.Height / baseSize.Height;
            float scaleFactor = Math.Min(scaleX, scaleY);

            // 设置最小/最大缩放限制
            scaleFactor = Math.Max(0.5f, Math.Min(scaleFactor, 2f));

            // 更新标题字体
            Font originalTitleFont = (Font)titleLabel.Tag;
            float newTitleSize = baseTitleFontSize * scaleFactor;
            titleLabel.Font = new Font(originalTitleFont.FontFamily, newTitleSize, originalTitleFont.Style);

            // 更新按钮字体
            Font originalButtonFont = (Font)startButton.Tag;
            float newButtonSize = baseButtonFontSize * scaleFactor;
            startButton.Font = new Font(originalButtonFont.FontFamily, newButtonSize, originalButtonFont.Style);

            // 调整控件位置（保持居中）
            titleLabel.Location = new Point(
                (this.ClientSize.Width - titleLabel.Width) / 2,
                (int)(150 * scaleFactor)
            );

            startButton.Location = new Point(
                (this.ClientSize.Width - startButton.Width) / 2,
                (int)(300 * scaleFactor)
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            base.OnPaint(e);
        }

        private void InitializeComponent()
        {
            this.Text = "数独游戏";
            this.Size = baseSize;
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
                Font = new Font("微软雅黑", baseTitleFontSize, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 51, 51),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true
            };
            this.Controls.Add(titleLabel);

            // 创建开始游戏按钮
            startButton = new Button
            {
                Text = "开始游戏",
                Font = new Font("微软雅黑", baseButtonFontSize, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 158, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                AutoSize = true
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
                AutoSize = true
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