using System;

namespace TaskManager.Models
{
    public class DiskModel : BaseViewModel
    {
        private double activeTime;
        private double averageResponseTime;
        private double readSpeed;
        private double writeSpeed;

        public string DisplayName { get; set; }

        public string DeviceId { get; set; }

        public string Model { get; set; }

        public double Capacity { get; set; }

        public double Formatted { get; set; }

        public string SystemDisk { get; set; }

        public string PageFile { get; set; }

        public string Type { get; set; }

        public double ActiveTime
        {
            get => activeTime;
            set
            {
                if (Math.Abs(activeTime - value) < 0.1)
                {
                    return;
                }

                activeTime = value;
                OnPropertyChanged(nameof(ActiveTime));
            }
        }

        public double AverageResponseTime
        {
            get => averageResponseTime;
            set
            {
                if (Math.Abs(averageResponseTime - value) < 0.1)
                {
                    return;
                }

                averageResponseTime = value;
                OnPropertyChanged(nameof(AverageResponseTime));
            }
        }

        public double ReadSpeed
        {
            get => readSpeed;
            set
            {
                if (Math.Abs(readSpeed - value) < 0.1)
                {
                    return;
                }

                readSpeed = value;
                OnPropertyChanged(nameof(ReadSpeed));
            }
        }

        public double WriteSpeed
        {
            get => writeSpeed;
            set
            {
                if (Math.Abs(writeSpeed - value) < 0.1)
                {
                    return;
                }

                writeSpeed = value;
                OnPropertyChanged(nameof(WriteSpeed));
            }
        }
    }
}
