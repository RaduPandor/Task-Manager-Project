using System;

namespace TaskManager.ViewModels
{
    public class ProcessViewModel : BaseViewModel
    {
        private string name;
        private double cpuUsage;
        private double memoryUsage;
        private double diskUsage;
        private double networkUsage;
        private double gpuUsage;

        public int Id { get; set; }

        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                {
                    return;
                }

                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public double CpuUsage
        {
            get => cpuUsage;
            set
            {
                cpuUsage = value;
                OnPropertyChanged(nameof(CpuUsage));
            }
        }

        public double MemoryUsage
        {
            get => memoryUsage;
            set
            {
                memoryUsage = value;
                OnPropertyChanged(nameof(MemoryUsage));
            }
        }

        public double DiskUsage
        {
            get => diskUsage;
            set
            {
                diskUsage = value;
                OnPropertyChanged(nameof(DiskUsage));
            }
        }

        public double NetworkUsage
        {
            get => networkUsage;
            set
            {
                networkUsage = value;
                OnPropertyChanged(nameof(NetworkUsage));
            }
        }

        public double GpuUsage
        {
            get => gpuUsage;
            set
            {
                gpuUsage = value;
                OnPropertyChanged(nameof(GpuUsage));
            }
        }
    }
}
