using System.Windows;
using System.Windows.Input;

namespace TaskManager.Services
{
    public interface IMainWindowViewModel
    {
        ICommand MinimizeCommand { get; }

        ICommand MaximizeCommand { get; }

        ICommand CloseCommand { get; }

        ICommand DragMoveCommand { get; }

        ICommand ToggleMenuCommand { get; }

        ICommand ShowProcesses { get; }

        ICommand ShowPerformance { get; }

        ICommand ShowDetails { get; }

        ICommand ShowServices { get; }

        ICommand ShowStartup { get; }

        ICommand ShowAppHistory { get; }

        ICommand ShowUsers { get; }

        BaseViewModel CurrentView { get; set; }

        bool IsMenuVisible { get; set; }

        bool IsLoading { get; set; }

        GridLength MenuColumnWidth { get; }
    }
}
