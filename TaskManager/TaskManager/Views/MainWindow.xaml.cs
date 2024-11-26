namespace TaskManager
{
    using System.Windows;
    using TaskManager.ViewModels;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }
    }
}
