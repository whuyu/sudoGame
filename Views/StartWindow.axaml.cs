using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;
using System;

namespace SudokuGame.Views
{
    public partial class StartWindow : Window
    {
        private readonly Canvas backgroundGrid;

        public StartWindow()
        {
            InitializeComponent();
            backgroundGrid = this.FindControl<Canvas>("BackgroundGrid");
            
            // 设置窗口大小改变时重绘背景网格
            this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(_ => DrawBackgroundGrid()));
            
            // 设置开始按钮点击事件
            var startButton = this.FindControl<Button>("StartButton");
            startButton.Click += StartButton_Click;
        }

        private void DrawBackgroundGrid()
        {
            backgroundGrid.Children.Clear();

            // 获取画布尺寸
            double width = this.Bounds.Width;
            double height = this.Bounds.Height;

            // 网格大小
            double gridSize = 30;

            // 创建网格线
            for (double x = 0; x < width; x += gridSize)
            {
                var line = new Line
                {
                    StartPoint = new Point(x, 0),
                    EndPoint = new Point(x, height),
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                backgroundGrid.Children.Add(line);
            }

            for (double y = 0; y < height; y += gridSize)
            {
                var line = new Line
                {
                    StartPoint = new Point(0, y),
                    EndPoint = new Point(width, y),
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                backgroundGrid.Children.Add(line);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建并显示主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();
            
            // 关闭启动窗口
            this.Close();
        }
    }

    // 创建一个匿名观察者类来处理Observable订阅
    internal class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;

        public AnonymousObserver(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => _onNext(value);
    }
} 