using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class DetailsViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsService;
        private CancellationTokenSource linkedCancellationTokenSource;
        private ICollectionView processesView;
        private Dictionary<int, Process> cachedProcesses;

        public DetailsViewModel(PerformanceMetricsService performanceMetricsService)
        {
            this.performanceMetricsService = performanceMetricsService;
            Processes = new ObservableCollection<DetailsModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
            ProcessesView.SortDescriptions.Add(new SortDescription(nameof(DetailsModel.CpuUsage), ListSortDirection.Descending));
            cachedProcesses = new Dictionary<int, Process>();
        }

        public ObservableCollection<DetailsModel> Processes { get; }

        public ICollectionView ProcessesView
        {
            get => processesView;
            private set
            {
                processesView = value;
                OnPropertyChanged(nameof(ProcessesView));
            }
        }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            Processes.Clear();
            cachedProcesses.Clear();
        }

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            await Task.Run(() => LoadProcessesAsync(token), token);
        }

        private async Task LoadProcessesAsync(CancellationToken token)
        {
            cachedProcesses = Process.GetProcesses()
                                      .ToDictionary(p => p.Id, p => p);

            var tasks = cachedProcesses.Values.Select(async process =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                var processModel = new DetailsModel
                {
                    Name = process.ProcessName,
                    Id = process.Id,
                    Status = process.Responding ? "Running" : "Suspended",
                    UserName = performanceMetricsService.GetProcessOwner(process.Id),
                    CpuUsage = "0",
                    MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 3),
                    Architecture = Environment.Is64BitProcess ? "x64" : "x86",
                    Description = GetProcessDescription(process)
                };

                App.Current.Dispatcher.Invoke(() => Processes.Add(processModel));
            });

            await Task.WhenAll(tasks);

            if (!token.IsCancellationRequested)
            {
                foreach (var processModel in Processes)
                {
                    _ = Task.Run(() => UpdateProcessMetricsPeriodicallyAsync(processModel, token), token);
                }
            }
        }

        private async Task UpdateProcessMetricsPeriodicallyAsync(DetailsModel processModel, CancellationToken token)
        {
            if (!cachedProcesses.TryGetValue(processModel.Id, out var process) || process.HasExited)
            {
                return;
            }

            while (!token.IsCancellationRequested && !process.HasExited)
            {
                var cpuUsage = await performanceMetricsService.GetCpuUsageAsync(process);
                var memoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 3);

                if (Math.Abs(double.Parse(processModel.CpuUsage) - cpuUsage) > 0.1 ||
                    Math.Abs(processModel.MemoryUsage - memoryUsage) > 50)
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        processModel.CpuUsage = cpuUsage.ToString();
                        processModel.MemoryUsage = memoryUsage;
                        ProcessesView.Refresh();
                    });
                }

                await Task.Delay(1000, token);
            }
        }

        private string GetProcessDescription(Process process)
        {
            try
            {
                return process.MainModule?.FileVersionInfo.FileDescription ?? "No description";
            }
            catch (Win32Exception)
            {
                return "Host Process for Windows Services";
            }
            catch (InvalidOperationException)
            {
                return "Process exited";
            }
            catch (UnauthorizedAccessException)
            {
                return "Access denied";
            }
        }
    }
}
