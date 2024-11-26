using System;

namespace TaskManager.Models
{
    public class ProcessModel : BaseViewModel
    {
        private string name;
        private string status;
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

        public string Status
        {
            get => status;
            set
            {
                if (status == value)
                {
                    return;
                }

                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public double CpuUsage
        {
            get => cpuUsage;
            set
            {
                if (Math.Abs(cpuUsage - value) < 0.1)
                {
                    return;
                }

                cpuUsage = value;
                OnPropertyChanged(nameof(CpuUsage));
            }
        }

        public double MemoryUsage
        {
            get => memoryUsage;
            set
            {
                if (Math.Abs(memoryUsage - value) < 0.1)
                {
                    return;
                }

                memoryUsage = value;
                OnPropertyChanged(nameof(MemoryUsage));
            }
        }

        public double DiskUsage
        {
            get => diskUsage;
            set
            {
                if (Math.Abs(diskUsage - value) < 0.1)
                {
                    return;
                }

                diskUsage = value;
                OnPropertyChanged(nameof(DiskUsage));
            }
        }

        public double NetworkUsage
        {
            get => networkUsage;
            set
            {
                if (Math.Abs(networkUsage - value) < 0.1)
                {
                    return;
                }

                networkUsage = value;
                OnPropertyChanged(nameof(NetworkUsage));
            }
        }

        public double GpuUsage
        {
            get => gpuUsage;
            set
            {
                if (Math.Abs(gpuUsage - value) < 0.1)
                {
                    return;
                }

                gpuUsage = value;
                OnPropertyChanged(nameof(GpuUsage));
            }
        }
    }
}
