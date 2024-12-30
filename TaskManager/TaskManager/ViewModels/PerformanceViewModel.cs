using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Input;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class PerformanceViewModel : BaseViewModel
    {
        private BaseViewModel currentView;

        public PerformanceViewModel()
        {
            Disks = new ObservableCollection<DiskInfo>();
            NetworkInterfaces = new ObservableCollection<NetworkInfo>();
            GPU = new ObservableCollection<GPUInfo>();
            LoadDiskDrives();
            LoadNetworkInterfaces();
            LoadGPU();
            ShowCPU = Show<CPUViewModel>();
            ShowMemory = Show<MemoryViewModel>();
            ShowDisk = ShowDiskCommand();
            ShowNetwork = ShowNetworkInterfaceCommand();
            ShowGPU = ShowGPUCommand();
        }

        public ObservableCollection<DiskInfo> Disks { get; }

        public ObservableCollection<NetworkInfo> NetworkInterfaces { get; }

        public ObservableCollection<GPUInfo> GPU { get; }

        public ICommand ShowCPU { get; }

        public ICommand ShowMemory { get; }

        public ICommand ShowDisk { get; }

        public ICommand ShowNetwork { get; }

        public ICommand ShowGPU { get; }

        public BaseViewModel CurrentView
        {
            get => currentView;
            set
            {
                if (currentView is ILoadableViewModel oldLoadable)
                {
                    oldLoadable.OnNavigatedFrom();
                }

                currentView = value;
                if (currentView is ILoadableViewModel newLoadable)
                {
                    using var cancellationTokenSource = new CancellationTokenSource();
                    newLoadable.OnNavigatedToAsync(cancellationTokenSource.Token);
                }

                OnPropertyChanged(nameof(CurrentView));
            }
        }

        private RelayCommand<object> Show<ViewModel>()
            where ViewModel : BaseViewModel, new()
        {
            return new RelayCommand<object>(param => CurrentView = new ViewModel());
        }

        private RelayCommand<string> ShowDiskCommand()
        {
            return new RelayCommand<string>(deviceId =>
            {
                var diskInfo = Disks.FirstOrDefault(d => d.DeviceId == deviceId);
                if (diskInfo == null)
                {
                    return;
                }

                CurrentView = new DiskViewModel(diskInfo.DeviceId, diskInfo.DisplayName);
            });
        }

        private void LoadDiskDrives()
        {
            foreach (var disk in GetDiskDrives())
            {
                Disks.Add(disk);
            }
        }

        private void LoadNetworkInterfaces()
        {
            foreach (var networkInterface in GetNetworkInterfaces())
            {
                NetworkInterfaces.Add(networkInterface);
            }
        }

        private ObservableCollection<DiskInfo> GetDiskDrives()
        {
            var disks = new ObservableCollection<DiskInfo>();
            using (var diskSearcher = new ManagementObjectSearcher("SELECT DeviceID FROM Win32_DiskDrive"))
            {
                var diskList = diskSearcher.Get().Cast<ManagementObject>().ToList();
                for (int i = 0; i < diskList.Count; i++)
                {
                    var disk = diskList[i];
                    var partitions = GetPartitions(disk["DeviceID"].ToString());
                    var displayName = $"Disk {i} ({string.Join(", ", partitions)})";
                    disks.Add(new DiskInfo
                    {
                        DisplayName = displayName,
                        DeviceId = disk["DeviceID"].ToString()
                    });
                }
            }

            return disks;
        }

        private ObservableCollection<string> GetPartitions(string deviceId)
        {
            var partitions = new ObservableCollection<string>();
            using (var searcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition"))
            {
                foreach (var partition in searcher.Get().Cast<ManagementObject>())
                {
                    string queryString = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
                    using (var logicalSearcher = new ManagementObjectSearcher(queryString))
                    {
                        foreach (var logical in logicalSearcher.Get().Cast<ManagementObject>())
                        {
                            partitions.Add(logical["DeviceID"].ToString());
                        }
                    }
                }
            }

            return partitions;
        }

        private ObservableCollection<NetworkInfo> GetNetworkInterfaces()
        {
            var networkInterfaces = new ObservableCollection<NetworkInfo>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    networkInterfaces.Add(new NetworkInfo
                    {
                        DisplayName = networkInterface.Name,
                        NetworkId = networkInterface.Id
                    });
                }
            }

            return networkInterfaces;
        }

        private RelayCommand<string> ShowNetworkInterfaceCommand()
        {
            return new RelayCommand<string>(interfaceId =>
            {
                var networkInfo = NetworkInterfaces.FirstOrDefault(n => n.NetworkId == interfaceId);
                if (networkInfo == null)
                {
                    return;
                }

                CurrentView = new NetworkViewModel(networkInfo.DisplayName);
            });
        }

        private void LoadGPU()
        {
            using (var gpuSearcher = new ManagementObjectSearcher("SELECT Name, DeviceID FROM Win32_VideoController"))
            {
                var gpuList = gpuSearcher.Get().Cast<ManagementObject>().ToList();
                for (int i = 0; i < gpuList.Count; i++)
                {
                    var gpu = gpuList[i];
                    var displayName = $"GPU {i}";
                    GPU.Add(new GPUInfo
                    {
                        DisplayName = displayName,
                        DeviceId = gpu["DeviceID"].ToString()
                    });
                }
            }
        }

        private RelayCommand<string> ShowGPUCommand()
        {
            return new RelayCommand<string>(deviceId =>
            {
                var gpuInfo = GPU.FirstOrDefault(g => g.DeviceId == deviceId);
                if (gpuInfo == null)
                {
                    return;
                }

                CurrentView = new GPUViewModel(gpuInfo.DeviceId, gpuInfo.DisplayName);
            });
        }
    }
}