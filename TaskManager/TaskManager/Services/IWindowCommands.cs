using System.Windows.Input;

namespace TaskManager.Services
{
    public interface IWindowCommands
    {
        ICommand MinimizeCommand { get; }

        ICommand MaximizeCommand { get; }

        ICommand CloseCommand { get; }

        ICommand DragMoveCommand { get; }
    }
}
