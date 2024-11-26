using System.Collections.ObjectModel;

namespace TaskManager.Models
{
    public class UsersModel : BaseViewModel
    {
        private double totalCpuUsage;
        private double totalMemoryUsage;
        private double totalDiskUsage;
        private double totalNetworkUsage;

        public UsersModel(string userName, int processCount)
        {
            UserName = userName;
            ProcessCount = processCount;
            Processes = new ObservableCollection<ProcessModel>();
        }

        public string UserName { get; }

        public int ProcessCount { get; set; }

        public string DisplayName => $"{UserName} ({ProcessCount})";

        public ObservableCollection<ProcessModel> Processes { get; }

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