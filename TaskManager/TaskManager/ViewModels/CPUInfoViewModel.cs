using System;

namespace TaskManager.ViewModels
{
    public class CPUInfoViewModel : BaseViewModel
    {
        private double cpuUsage;
        private int processes;
        private int threads;
        private int handles;
        private TimeSpan uptime;

        public string CPUName { get; set; }

        public double BaseSpeed { get; set; }

        public int Sockets { get; set; }

        public int Cores { get; set; }

        public int LogicalProcessors { get; set; }

        public string Virtualization { get; set; }

        public double L1Cache { get; set; }

        public double L2Cache { get; set; }

        public double L3Cache { get; set; }

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

        public int Processes
        {
            get => processes;
            set
            {
                if (processes == value)
                {
                    return;
                }

                processes = value;
                OnPropertyChanged(nameof(Processes));
            }
        }

        public int Threads
        {
            get => threads;
            set
            {
                if (threads == value)
                {
                    return;
                }

                threads = value;
                OnPropertyChanged(nameof(Threads));
            }
        }

        public int Handles
        {
            get => handles;
            set
            {
                if (handles == value)
                {
                    return;
                }

                handles = value;
                OnPropertyChanged(nameof(Handles));
            }
        }

        public TimeSpan UpTime
        {
            get => uptime;
            set
            {
                if (uptime == value)
                {
                    return;
                }

                uptime = value;
                OnPropertyChanged(nameof(UpTime));
            }
        }
    }
}
