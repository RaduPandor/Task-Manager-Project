using System.Windows;

namespace TaskManager.Services
{
    public class ErrorDialogService : IErrorDialogService
    {
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
