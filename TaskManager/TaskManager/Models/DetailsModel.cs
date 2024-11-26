using System;

namespace TaskManager.Models
{
    public class DetailsModel : BaseViewModel
    {
        private string cpuUsage;
        private double memoryUsage;

        public string Name { get; set; }

        public int Id { get; set; }

        public string Status { get; set; }

        public string UserName { get; set; }

        public string CpuUsage
        {
            get => cpuUsage;
            set
            {
                if (cpuUsage == value)
                {
                    return;
                }

                cpuUsage = value;
                OnPropertyChanged(nameof(Name));
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
