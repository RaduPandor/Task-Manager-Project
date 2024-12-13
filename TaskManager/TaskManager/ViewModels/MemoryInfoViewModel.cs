using System;

namespace TaskManager.ViewModels
{
    public class MemoryInfoViewModel : BaseViewModel
    {
        private double inUseMemory;
        private double availableMemory;
        private double committedMemory;
        private double cachedMemory;

        public double TotalMemory { get; set; }

        public double PagedPool { get; set; }

        public double NonPagedPool { get; set; }

        public double Speed { get; set; }

        public int SlotsUsed { get; set; }

        public string FormFactor { get; set; }

        public double InUseMemory
        {
            get => inUseMemory;
            set
            {
                if (Math.Abs(inUseMemory - value) < 0.1)
                {
                    return;
                }

                inUseMemory = value;
                OnPropertyChanged(nameof(InUseMemory));
            }
        }

        public double AvailableMemory
        {
            get => availableMemory;
            set
            {
                if (Math.Abs(availableMemory - value) < 0.1)
                {
                    return;
                }

                availableMemory = value;
                OnPropertyChanged(nameof(AvailableMemory));
            }
        }

        public double CommittedMemory
        {
            get => committedMemory;
            set
            {
                if (Math.Abs(committedMemory - value) < 0.1)
                {
                    return;
                }

                committedMemory = value;
                OnPropertyChanged(nameof(CommittedMemory));
            }
        }

        public double CachedMemory
        {
            get => cachedMemory;
            set
            {
                if (Math.Abs(cachedMemory - value) < 0.1)
                {
                    return;
                }

                cachedMemory = value;
                OnPropertyChanged(nameof(CachedMemory));
            }
        }
    }
}
