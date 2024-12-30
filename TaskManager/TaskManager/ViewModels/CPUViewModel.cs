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
    public class CPUViewModel : BaseViewModel, ILoadableViewModel
    {
        private CancellationTokenSource linkedCancellationTokenSource;
        private PerformanceCounter cpuCounter;
        private Task runningTask;

        public CPUViewModel()
        {
            CPUData = new ObservableCollection<CPUInfoViewModel>();
            CpuUsageSeries = new SeriesCollection
            {
                    new LineSeries
                    {
                        Values = new ChartValues<ObservableValue>(),
                        PointGeometry = null
                    }
            };
        }

        public ObservableCollection<CPUInfoViewModel> CPUData { get; }

        public SeriesCollection CpuUsageSeries { get; }

        public CPUInfoViewModel LatestCPUModel => CPUData.LastOrDefault();

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            runningTask = Task.Run(async () => await LoadDataAsync(token), token);
            await runningTask;
        }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            cpuCounter?.Dispose();
            CPUData.Clear();
            if (runningTask == null)
            {
                return;
            }

            runningTask = null;
        }

        private static double ConvertToDouble(object value)
        {
            return value != null ? Convert.ToDouble(value) : 0;
        }

        private static int ConvertToInt(object value)
        {
            return value != null ? Convert.ToInt32(value) : 0;
        }

        private static bool ConvertToBool(object value)
        {
            return value != null && Convert.ToBoolean(value);
        }

        private async Task LoadDataAsync(CancellationToken token)
        {
            await LoadStaticCPUMetricsAsync(token);
            await LoadDynamicCPUMetricsAsync(token);
        }

        private async Task LoadStaticCPUMetricsAsync(CancellationToken token)
        {
            var cpuMetrics = await Task.Run(
                () =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                var metrics = new CPUInfoViewModel();
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name, MaxClockSpeed, NumberOfCores, L2CacheSize, L3CacheSize, VirtualizationFirmwareEnabled FROM Win32_Processor"))
                {
                    var processor = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

                    if (processor != null)
                    {
                        metrics.CPUName = processor["Name"]?.ToString() ?? "Unknown";
                        metrics.BaseSpeed = ConvertToDouble(processor["MaxClockSpeed"]) / 1000.0;
                        metrics.Sockets = searcher.Get().Count;
                        metrics.Cores = ConvertToInt(processor["NumberOfCores"]);
                        metrics.LogicalProcessors = Environment.ProcessorCount;
                        metrics.L1Cache = 0;
                        metrics.L2Cache = Math.Round(ConvertToDouble(processor["L2CacheSize"]) / 1024, 1);
                        metrics.L3Cache = Math.Round(ConvertToDouble(processor["L3CacheSize"]) / 1024, 1);
                        metrics.Virtualization = ConvertToBool(processor["VirtualizationFirmwareEnabled"]) ? "Enabled" : "Disabled";
                    }
                }

                return metrics;
            }, token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() => CPUData.Add(cpuMetrics));
        }

        private async Task LoadDynamicCPUMetricsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var cpuUsage = Math.Round(cpuCounter.NextValue(), 2);
                var processCount = Process.GetProcesses().Length;
                var currentProcess = Process.GetCurrentProcess();
                var metricsUpdate = new
                {
                    CpuUsage = cpuUsage,
                    Processes = processCount,
                    Threads = currentProcess.Threads.Count,
                    Handles = currentProcess.HandleCount,
                    UpTime = GetUpTime()
                };

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var cpuMetrics = LatestCPUModel;
                    if (cpuMetrics == null)
                    {
                        return;
                    }

                    cpuMetrics.CpuUsage = metricsUpdate.CpuUsage;
                    cpuMetrics.Processes = metricsUpdate.Processes;
                    cpuMetrics.Threads = metricsUpdate.Threads;
                    cpuMetrics.Handles = metricsUpdate.Handles;
                    cpuMetrics.UpTime = metricsUpdate.UpTime;

                    CpuUsageSeries[0].Values.Add(new ObservableValue(cpuMetrics.CpuUsage));
                    if (CpuUsageSeries[0].Values.Count > 60)
                    {
                        CpuUsageSeries[0].Values.RemoveAt(0);
                    }

                    OnPropertyChanged(nameof(LatestCPUModel));
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

        private TimeSpan GetUpTime()
        {
            var workingTime = TimeSpan.FromMilliseconds(Environment.TickCount);
            return new TimeSpan(workingTime.Days, workingTime.Hours, workingTime.Minutes, workingTime.Seconds);
        }
    }
}