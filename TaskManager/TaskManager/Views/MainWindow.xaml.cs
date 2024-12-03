using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TaskManager.ViewModels;

namespace TaskManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<IMainWindowViewModel>();
        }
    }
}
