using System.Windows;
using System.Windows.Input;

namespace TaskManager.ViewModels
{
    public interface IMainWindowViewModel
    {
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
