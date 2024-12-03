using LibreHardwareMonitor.Hardware;
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
    public class GPUViewModel : BaseViewModel, ICancellableViewModel, IDisposable
    {
        private readonly Computer computer;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;

        public GPUViewModel(string deviceId, string displayName)
        {
            DeviceId = deviceId;
            DisplayName = displayName;
            GpuModel = new ObservableCollection<GPUModel>();
            computer = new Computer { IsGpuEnabled = true };
            computer.Open();
            GpuUsageSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<ObservableValue>(),
                    PointGeometry = null
                }
            };
            cancellationTokenSource = new CancellationTokenSource();
            LoadStaticGpuMetrics();
            LoadDynamicGpuMetricsAsync(cancellationTokenSource.Token);
        }

        public string DeviceId { get; }

        public string DisplayName { get; }

        public ObservableCollection<GPUModel> GpuModel { get; }

        public SeriesCollection GpuUsageSeries { get; }

        public GPUModel LatestGpuModel => GpuModel.LastOrDefault();

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
                computer.Close();
            }

            disposed = true;
        }

        private void LoadStaticGpuMetrics()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                var gpuObjects = searcher.Get().Cast<ManagementObject>().ToList();
                var primaryGpu = gpuObjects.FirstOrDefault(obj => obj["DeviceID"].ToString() == DeviceId);

                if (primaryGpu != null)
                {
                    var gpuMetrics = new GPUModel
                    {
                        Model = primaryGpu["Name"]?.ToString() ?? " ",
                        DriverVersion = primaryGpu["DriverVersion"]?.ToString() ?? " ",
                        DriverDate = TryParseDriverDate(primaryGpu["DriverDate"]?.ToString()),
                        DirectXVersion = GetDirectXVersion(),
                        PhysicalLocation = primaryGpu["DeviceID"]?.ToString() ?? " "
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GpuModel.Add(gpuMetrics);
                        OnPropertyChanged(nameof(LatestGpuModel));
                    });
                }
            }
        }

        private async Task LoadDynamicGpuMetricsAsync(CancellationToken token)
        {
            cancellationTokenSource = new CancellationTokenSource();
            while (!token.IsCancellationRequested)
            {
                foreach (var hardware in computer.Hardware)
                {
                    hardware.Update();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var gpuMetrics = LatestGpuModel;
                    if (gpuMetrics == null)
                    {
                        return;
                    }

                    gpuMetrics.Usage = GetGPUUsage();
                    gpuMetrics.Temp = GetTemperature();
                    gpuMetrics.SharedMemory = GetSharedMemory() / (1024 * 1024 * 1000);
                    gpuMetrics.DedicatedMemory = GetDedicatedMemory() / (1024 * 1024 * 1000);
                    gpuMetrics.Memory = GetMemory() / (1024 * 1024 * 1000);

                    GpuUsageSeries[0].Values.Add(new ObservableValue(gpuMetrics.Usage));
                    if (GpuUsageSeries[0].Values.Count > 60)
                    {
                        GpuUsageSeries[0].Values.RemoveAt(0);
                    }

                    OnPropertyChanged(nameof(LatestGpuModel));
                    OnPropertyChanged(nameof(GpuUsageSeries));
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

        private float GetGPUUsage()
        {
            foreach (var hardware in computer.Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load)
                    {
                        return sensor.Value ?? 0;
                    }
                }
            }

            return 0;
        }

        private double GetTemperature()
        {
            foreach (var hardware in computer.Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        return sensor.Value ?? 0;
                    }
                }
            }

            return 0;
        }

        private double GetMemory()
        {
            return GetDedicatedMemory() + GetSharedMemory();
        }

        private double GetDedicatedMemory()
        {
            var category = new PerformanceCounterCategory("GPU Adapter Memory");
            var counterNames = category.GetInstanceNames();
            float totalDedicatedMemoryUsage = 0f;

            foreach (string counterName in counterNames)
            {
                foreach (var counter in category.GetCounters(counterName))
                {
                    if (counter.CounterName == "Dedicated Usage")
                    {
                        totalDedicatedMemoryUsage += counter.NextValue();
                    }
                }
            }

            return totalDedicatedMemoryUsage;
        }

        private double GetSharedMemory()
        {
            var category = new PerformanceCounterCategory("GPU Adapter Memory");
            var counterNames = category.GetInstanceNames();
            float totalSharedMemoryUsage = 0f;
            foreach (string counterName in counterNames)
            {
                foreach (var counter in category.GetCounters(counterName))
                {
                    if (counter.CounterName == "Shared Usage")
                    {
                        totalSharedMemoryUsage += counter.NextValue();
                    }
                }
            }

            return totalSharedMemoryUsage;
        }

        private string TryParseDriverDate(string rawDriverDate)
        {
            if (DateTime.TryParseExact(rawDriverDate, "yyyyMMddHHmmss.000000-000", null, System.Globalization.DateTimeStyles.None, out var driverDate))
            {
                return driverDate.ToString("dd-MM-yyyy");
            }

            return " ";
        }

        private string GetDirectXVersion()
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
            {
                if (key != null)
                {
                    var version = key.GetValue("Version")?.ToString();
                    return version ?? " ";
                }
            }

            return " ";
        }
    }
}
