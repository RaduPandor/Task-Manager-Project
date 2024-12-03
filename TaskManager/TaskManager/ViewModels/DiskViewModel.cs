using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class DiskViewModel : BaseViewModel, ICancellableViewModel, IDisposable
    {
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;
        private PerformanceCounter diskReadCounter;
        private PerformanceCounter diskWriteCounter;
        private PerformanceCounter diskAvgTimeCounter;
        private PerformanceCounter diskAvgWriteTimeCounter;
        private PerformanceCounter diskTimeCounter;

        public DiskViewModel(string deviceId, string displayName)
        {
            DeviceId = deviceId;
            DisplayName = displayName;
            DiskModel = new ObservableCollection<DiskModel>();
            DiskUsageSeries = new SeriesCollection
            {
                    new LineSeries
                    {
                        Values = new ChartValues<ObservableValue>(),
                        PointGeometry = null
                    }
            };
            cancellationTokenSource = new CancellationTokenSource();
            LoadStaticDiskMetrics();
            InitializePerformanceCounters();
            LoadDynamicDiskMetricsAsync(cancellationTokenSource.Token);
        }

        public string DeviceId { get; }

        public string DisplayName { get; }

        public ObservableCollection<DiskModel> DiskModel { get; }

        public SeriesCollection DiskUsageSeries { get; }

        public DiskModel LatestDiskModel => DiskModel.LastOrDefault();

        public void StopMonitoring()
        {
            if (cancellationTokenSource == null)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                StopMonitoring();
                diskReadCounter?.Dispose();
                diskWriteCounter?.Dispose();
                diskAvgTimeCounter?.Dispose();
                diskAvgWriteTimeCounter?.Dispose();
                diskTimeCounter?.Dispose();
            }

            disposed = true;
        }

        private void InitializePerformanceCounters()
        {
            diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total");
            diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total");
            diskAvgTimeCounter = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total");
            diskAvgWriteTimeCounter = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", "_Total");
            diskTimeCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        }

        private void LoadStaticDiskMetrics()
        {
            var diskMetrics = new DiskModel
            {
                Model = GetDiskModel(),
                Capacity = GetCapacity(),
                Formatted = IsFormatted(),
                SystemDisk = GetLogicalDrivesForDeviceId(DeviceId).Contains("C:") ? "yes" : "no",
                PageFile = IsPageFile()
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                DiskModel.Add(diskMetrics);
                OnPropertyChanged(nameof(LatestDiskModel));
            });
        }

        private async Task LoadDynamicDiskMetricsAsync(CancellationToken token)
        {
            cancellationTokenSource = new CancellationTokenSource();
            diskReadCounter.NextValue();
            diskWriteCounter.NextValue();
            diskAvgTimeCounter.NextValue();
            while (!token.IsCancellationRequested)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var diskMetrics = LatestDiskModel;
                    if (diskMetrics == null)
                    {
                        return;
                    }

                    diskMetrics.ActiveTime = Math.Round(diskTimeCounter.NextValue());
                    diskMetrics.AverageResponseTime = GetAverageResponseTime();
                    diskMetrics.ReadSpeed = diskReadCounter.NextValue() * 512 / 1024;
                    diskMetrics.WriteSpeed = diskWriteCounter.NextValue() * 512 / 1024;

                    DiskUsageSeries[0].Values.Add(new ObservableValue(diskMetrics.ActiveTime));
                    if (DiskUsageSeries[0].Values.Count > 60)
                    {
                        DiskUsageSeries[0].Values.RemoveAt(0);
                    }

                    OnPropertyChanged(nameof(LatestDiskModel));
                    OnPropertyChanged(nameof(DiskUsageSeries));
                });

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private string GetDiskModel()
        {
            string formattedDeviceId = DeviceId.Replace("\\", "\\\\");
            using (var searcher = new ManagementObjectSearcher($"SELECT Model FROM Win32_DiskDrive WHERE DeviceID = '{formattedDeviceId}'"))
            {
                var disk = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                return disk != null ? disk["Model"].ToString() : "Unknown Model";
            }
        }

        private double GetAverageResponseTime()
        {
            double avgReadTimeInSeconds = diskAvgTimeCounter.NextValue();
            double avgWriteTimeInSeconds = diskAvgWriteTimeCounter.NextValue();
            double avgResponseTime = (avgReadTimeInSeconds + avgWriteTimeInSeconds) / 2;
            return Math.Round(avgResponseTime * 1000, 2);
        }

        private double GetCapacity()
        {
            string formattedDeviceId = DeviceId.Replace("\\", "\\\\");
            using (var searcher = new ManagementObjectSearcher($"SELECT Size FROM Win32_DiskDrive WHERE DeviceID = '{formattedDeviceId}'"))
            {
                var disk = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                return disk != null ? Convert.ToDouble(disk["Size"]) / (1024 * 1024 * 1024) : 0;
            }
        }

        private double IsFormatted()
        {
            double formattedCapacity = 0;

            foreach (var logicalDrive in GetLogicalDrivesForDeviceId(DeviceId))
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT Size, FreeSpace FROM Win32_LogicalDisk WHERE DeviceID = '{logicalDrive}'"))
                {
                    foreach (var disk in searcher.Get().Cast<ManagementObject>())
                    {
                        double totalSize = Convert.ToDouble(disk["Size"]);
                        formattedCapacity += totalSize / (1024 * 1024 * 1024);
                    }
                }
            }

            return formattedCapacity;
        }

        private string IsPageFile()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PageFile"))
            {
                foreach (var pageFile in searcher.Get().Cast<ManagementObject>())
                {
                    string pageFileName = pageFile["Name"].ToString();
                    if (pageFileName.StartsWith(DeviceId, StringComparison.OrdinalIgnoreCase))
                    {
                        return "yes";
                    }
                }
            }

            return "no";
        }

        private ObservableCollection<string> GetLogicalDrivesForDeviceId(string deviceId)
        {
            var logicalDrives = new ObservableCollection<string>();
            using (var searcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition"))
            {
                foreach (var partition in searcher.Get().Cast<ManagementObject>())
                {
                    string queryString = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
                    using (var logicalSearcher = new ManagementObjectSearcher(queryString))
                    {
                        foreach (var logical in logicalSearcher.Get().Cast<ManagementObject>())
                        {
                            logicalDrives.Add(logical["DeviceID"].ToString());
                        }
                    }
                }
            }

            return logicalDrives;
        }
    }
}
