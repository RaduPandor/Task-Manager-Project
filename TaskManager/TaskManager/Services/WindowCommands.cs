using System.Windows;
using System.Windows.Input;

namespace TaskManager.Services
{
    public class WindowCommands : IWindowCommands
    {
        public WindowCommands()
        {
            MinimizeCommand = new RelayCommand<object>(_ => Minimize());
            MaximizeCommand = new RelayCommand<object>(_ => Maximize());
            CloseCommand = new RelayCommand<object>(_ => Close());
            DragMoveCommand = new RelayCommand<Window>(DragMove);
        }

        public ICommand MinimizeCommand { get; }

        public ICommand MaximizeCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand DragMoveCommand { get; }

        private void Minimize()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void Maximize()
        {
            Application.Current.MainWindow.WindowState = Application.Current.MainWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close()
        {
            var mainWindow = Application.Current?.MainWindow;
            mainWindow?.Close();
        }

        private void DragMove(Window window)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            window.DragMove();
        }
    }
}
