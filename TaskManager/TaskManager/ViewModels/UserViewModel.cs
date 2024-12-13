using System.Collections.ObjectModel;

namespace TaskManager.ViewModels
{
    public class UserViewModel : BaseViewModel
    {
        private double totalCpuUsage;
        private double totalMemoryUsage;
        private double totalDiskUsage;
        private double totalNetworkUsage;

        public UserViewModel(string userName, int processCount)
        {
            UserName = userName;
            ProcessCount = processCount;
            Processes = new ObservableCollection<ProcessViewModel>();
        }

        public string UserName { get; }

        public int ProcessCount { get; set; }

        public string DisplayName => $"{UserName} ({ProcessCount})";

        public ObservableCollection<ProcessViewModel> Processes { get; }

        public double TotalCpuUsage
        {
            get => totalCpuUsage;
            set
            {
                totalCpuUsage = value;
                OnPropertyChanged(nameof(TotalCpuUsage));
            }
        }

        public double TotalMemoryUsage
        {
            get => totalMemoryUsage;
            set
            {
                totalMemoryUsage = value;
                OnPropertyChanged(nameof(TotalMemoryUsage));
            }
        }

        public double TotalDiskUsage
        {
            get => totalDiskUsage;
            set
            {
                totalDiskUsage = value;
                OnPropertyChanged(nameof(TotalDiskUsage));
            }
        }

        public double TotalNetworkUsage
        {
            get => totalNetworkUsage;
            set
            {
                totalNetworkUsage = value;
                OnPropertyChanged(nameof(TotalNetworkUsage));
            }
        }
    }
}