using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SudokuGame.Views;

namespace SudokuGame
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var startWindow = new StartWindow();
                desktop.MainWindow = startWindow;
                startWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
} 