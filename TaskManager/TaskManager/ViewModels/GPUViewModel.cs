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
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class GPUViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly Computer computer;
        private CancellationTokenSource linkedCancellationTokenSource;
        private Task runningTask;

        public GPUViewModel(string deviceId, string displayName)
        {
            DeviceId = deviceId;
            DisplayName = displayName;
            GpuModel = new ObservableCollection<GPUInfoViewModel>();
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
        }

        public string DeviceId { get; }

        public string DisplayName { get; }

        public ObservableCollection<GPUInfoViewModel> GpuModel { get; }

        public SeriesCollection GpuUsageSeries { get; }

        public GPUInfoViewModel LatestGpuModel => GpuModel.LastOrDefault();

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            runningTask = Task.Run(async () => await LoadDataAsync(token), token);
            await runningTask;
        }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            computer.Close();
            GpuModel.Clear();
            if (runningTask == null)
            {
                return;
            }

            runningTask = null;
        }

        private async Task LoadDataAsync(CancellationToken token)
        {
            await LoadStaticGpuMetricsAsync(token);
            await LoadDynamicGpuMetricsAsync(token);
        }

        private async Task LoadStaticGpuMetricsAsync(CancellationToken token)
        {
            var gpuMetrics = await Task.Run(
                () =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    var gpuObjects = searcher.Get().Cast<ManagementObject>().ToList();
                    var primaryGpu = gpuObjects.FirstOrDefault(obj => obj["DeviceID"].ToString() == DeviceId);

                    if (primaryGpu != null)
                    {
                        return new GPUInfoViewModel
                        {
                            Model = primaryGpu["Name"]?.ToString() ?? " ",
                            DriverVersion = primaryGpu["DriverVersion"]?.ToString() ?? " ",
                            DriverDate = TryParseDriverDate(primaryGpu["DriverDate"]?.ToString()),
                            DirectXVersion = GetDirectXVersion(),
                            PhysicalLocation = primaryGpu["DeviceID"]?.ToString() ?? " "
                        };
                    }

                    return null;
                }
            }, token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (gpuMetrics == null)
                {
                    return;
                }

                GpuModel.Add(gpuMetrics);
                OnPropertyChanged(nameof(LatestGpuModel));
            });
        }

        private async Task LoadDynamicGpuMetricsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var hardware in computer.Hardware)
                {
                    hardware.Update();
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
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