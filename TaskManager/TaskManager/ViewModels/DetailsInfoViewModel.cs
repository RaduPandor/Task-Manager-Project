using System;

namespace TaskManager.ViewModels
{
    public class DetailsInfoViewModel : BaseViewModel
    {
        private double cpuUsage;
        private double memoryUsage;

        public string Name { get; set; }

        public int Id { get; set; }

        public string Status { get; set; }

        public string UserName { get; set; }

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
                if (Math.Abs(memoryUsage - value) < 0.1)
                {
                    return;
                }

                memoryUsage = value;
                OnPropertyChanged(nameof(MemoryUsage));
            }
        }

        public string Architecture { get; set; }

        public string Description { get; set; }
    }
}
