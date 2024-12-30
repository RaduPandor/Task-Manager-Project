using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class MemoryViewModel : BaseViewModel, ILoadableViewModel
    {
        private CancellationTokenSource linkedCancellationTokenSource;
        private Task runningTask;

        public MemoryViewModel()
        {
            MemoryModel = new ObservableCollection<MemoryInfoViewModel>();
            MemoryUsageSeries = new SeriesCollection
            {
                    new LineSeries
                    {
                        Values = new ChartValues<ObservableValue>(),
                        PointGeometry = null
                    }
            };
        }

        public ObservableCollection<MemoryInfoViewModel> MemoryModel { get; }

        public SeriesCollection MemoryUsageSeries { get; }

        public MemoryInfoViewModel LatestMemoryModel => MemoryModel.LastOrDefault();

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
            MemoryModel.Clear();
            if (runningTask == null)
            {
                return;
            }

            runningTask = null;
        }

        private async Task LoadDataAsync(CancellationToken token)
        {
            await LoadStaticMemoryMetricsAsync(token);
            await LoadDynamicMemoryMetricsAsync(token);
        }

        private async Task LoadStaticMemoryMetricsAsync(CancellationToken token)
        {
            var memoryMetrics = await Task.Run(
                () =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                var metrics = new MemoryInfoViewModel();
                var memoryDetails = GetMemoryDetails();
                var memoryStaticMetrics = GetStaticMemoryMetrics();
                metrics.Speed = memoryDetails.memorySpeed;
                metrics.SlotsUsed = memoryDetails.slotsUsed;
                metrics.FormFactor = memoryDetails.formFactor;
                metrics.CommittedMemory = memoryStaticMetrics.committedMemory;
                metrics.CachedMemory = memoryStaticMetrics.cachedMemory;
                metrics.PagedPool = memoryStaticMetrics.pagedPool;
                metrics.NonPagedPool = memoryStaticMetrics.nonPagedPool;

                return metrics;
            }, token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MemoryModel.Add(memoryMetrics);
                OnPropertyChanged(nameof(LatestMemoryModel));
            });
        }

        private async Task LoadDynamicMemoryMetricsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var memoryMetrics = LatestMemoryModel;
                if (memoryMetrics == null)
                {
                    return;
                }

                var (totalMemory, inUseMemory, availableMemory) = GetMemoryMetrics();
                memoryMetrics.TotalMemory = totalMemory;
                memoryMetrics.InUseMemory = inUseMemory;
                memoryMetrics.AvailableMemory = availableMemory;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MemoryUsageSeries[0].Values.Add(new ObservableValue(memoryMetrics.InUseMemory));
                    if (MemoryUsageSeries[0].Values.Count > 60)
                    {
                        MemoryUsageSeries[0].Values.RemoveAt(0);
                    }

                    OnPropertyChanged(nameof(LatestMemoryModel));
                    OnPropertyChanged(nameof(MemoryUsageSeries));
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

        private (double totalMemory, double inUseMemory, double availableMemory) GetMemoryMetrics()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                var item = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (item != null)
                {
                    double totalMemory = Math.Ceiling(Convert.ToDouble(item["TotalVisibleMemorySize"]) / (1024 * 1024));
                    double freeMemory = Convert.ToDouble(item["FreePhysicalMemory"]) / (1024 * 1024);
                    double inUseMemory = totalMemory - freeMemory;

                    return (totalMemory, inUseMemory, freeMemory);
                }

                return (0, 0, 0);
            }
        }

        private (double committedMemory, double cachedMemory, double pagedPool, double nonPagedPool) GetStaticMemoryMetrics()
        {
            using (var searcher = new ManagementObjectSearcher(
                "SELECT CommittedBytes, CacheBytes, PoolPagedBytes, PoolNonpagedBytes FROM Win32_PerfFormattedData_PerfOS_Memory"))
            {
                var item = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (item != null)
                {
                    double committedMemory = Convert.ToDouble(item["CommittedBytes"]) / (1024 * 1024 * 1024);
                    double cachedMemory = Convert.ToDouble(item["CacheBytes"]) / (1024 * 1024 * 1024);
                    double pagedPool = Convert.ToDouble(item["PoolPagedBytes"]) / (1024 * 1024 * 1024);
                    double nonPagedPool = Convert.ToDouble(item["PoolNonpagedBytes"]) / (1024 * 1024);

                    return (committedMemory, cachedMemory, pagedPool, nonPagedPool);
                }

                return (0, 0, 0, 0);
            }
        }

        private (double memorySpeed, int slotsUsed, string formFactor) GetMemoryDetails()
        {
            double memorySpeed = 0;
            int slotsUsed = 0;
            string formFactor = " ";

            using (var searcher = new ManagementObjectSearcher("SELECT Speed, FormFactor FROM Win32_PhysicalMemory"))
            {
                var items = searcher.Get().Cast<ManagementObject>().ToList();
                slotsUsed = items.Count;

                foreach (var item in items)
                {
                    memorySpeed = Math.Max(memorySpeed, Convert.ToDouble(item["Speed"]));
                    if (formFactor == " " && item["FormFactor"] != null)
                    {
                        int formFactorValue = Convert.ToInt32(item["FormFactor"]);
                        formFactor = formFactorValue switch
                        {
                            0 => " ",
                            1 => "Other",
                            2 => "SIMM",
                            3 => "DIMM",
                            4 => "SODIMM",
                            5 => "SRDIMM",
                            6 => "SMDIMM",
                            7 => "RDIMM",
                            8 => "LRDIMM",
                            9 => "SO-DIMM DDR",
                            _ => " "
                        };
                    }
                }
            }

            return (memorySpeed, slotsUsed, formFactor);
        }
    }
}