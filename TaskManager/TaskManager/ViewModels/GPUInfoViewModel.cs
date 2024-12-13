using System;

namespace TaskManager.ViewModels
{
    public class GPUInfoViewModel : BaseViewModel
    {
        private double usage;
        private double temperature;
        private double dedicatedMemory;
        private double memory;
        private double sharedMemory;

        public string DisplayName { get; set; }

        public string DeviceId { get; set; }

        public string Model { get; set; }

        public string DriverVersion { get; set; }

        public string DriverDate { get; set; }

        public string DirectXVersion { get; set; }

        public string PhysicalLocation { get; set; }

        public double Temp
        {
            get => temperature;
            set
            {
                if (Math.Abs(temperature - value) < 0.1)
                {
                    return;
                }

                temperature = value;
                OnPropertyChanged(nameof(Temp));
            }
        }

        public double Usage
        {
            get => usage;
            set
            {
                if (Math.Abs(usage - value) < 0.1)
                {
                    return;
                }

                usage = value;
                OnPropertyChanged(nameof(Usage));
            }
        }

        public double DedicatedMemory
        {
            get => dedicatedMemory;
            set
            {
                if (Math.Abs(dedicatedMemory - value) < 0.1)
                {
                    return;
                }

                dedicatedMemory = value;
                OnPropertyChanged(nameof(DedicatedMemory));
            }
        }

        public double Memory
        {
            get => memory;
            set
            {
                if (Math.Abs(memory - value) < 0.1)
                {
                    return;
                }

                memory = value;
                OnPropertyChanged(nameof(Memory));
            }
        }

        public double SharedMemory
        {
            get => sharedMemory;
            set
            {
                if (Math.Abs(sharedMemory - value) < 0.1)
                {
                    return;
                }

                sharedMemory = value;
                OnPropertyChanged(nameof(SharedMemory));
            }
        }
    }
}
