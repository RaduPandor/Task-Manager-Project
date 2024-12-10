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
    public class ProcessesViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsHelper;
        private CancellationTokenSource linkedCancellationTokenSource;
        private ICollectionView processesView;
        private Dictionary<int, Process> cachedProcesses;

        public ProcessesViewModel(PerformanceMetricsService performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            Processes = new ObservableCollection<ProcessModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
            cachedProcesses = new Dictionary<int, Process>();
        }

        public ObservableCollection<ProcessModel> Processes { get; }

        public ICollectionView ProcessesView
        {
            get => processesView;
            private set
            {
                processesView = value;
                OnPropertyChanged(nameof(ProcessesView));
            }
        }

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            await Task.Run(() => LoadProcessesAsync(token), token);
        }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            Processes.Clear();
            cachedProcesses.Clear();
        }

        private async Task LoadProcessesAsync(CancellationToken token)
        {
            cachedProcesses = Process.GetProcesses().ToDictionary(p => p.Id, p => p);
            foreach (var process in cachedProcesses.Values)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (!HasPermissionToAccessProcess(process))
                {
                    continue;
                }

                var processModel = new ProcessModel
                {
                    Name = process.ProcessName,
                    Id = process.Id,
                    MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1),
                    CpuUsage = 0,
                    NetworkUsage = 0,
                    DiskUsage = 0
                };

                await App.Current.Dispatcher.InvokeAsync(() => Processes.Add(processModel));
            }

            foreach (var processModel in Processes)
            {
                if (!token.IsCancellationRequested)
                {
                    _ = Task.Run(() => UpdateProcessMetricsAsync(processModel, token), token);
                }
            }
        }

        private async Task UpdateProcessMetricsAsync(ProcessModel processModel, CancellationToken token)
        {
            if (!cachedProcesses.TryGetValue(processModel.Id, out var process) || token.IsCancellationRequested || process.HasExited)
            {
                return;
            }

            while (!token.IsCancellationRequested && !process.HasExited)
            {
                var metrics = await GetProcessMetricsAsync(process);
                if (Math.Abs(processModel.CpuUsage - metrics.CpuUsage) > 0.1 ||
                    Math.Abs(processModel.MemoryUsage - metrics.MemoryUsage) > 0.1 ||
                    Math.Abs(processModel.NetworkUsage - metrics.NetworkUsage) > 0.1 ||
                    Math.Abs(processModel.DiskUsage - metrics.DiskUsage) > 0.1)
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        processModel.CpuUsage = metrics.CpuUsage;
                        processModel.MemoryUsage = metrics.MemoryUsage;
                        processModel.NetworkUsage = metrics.NetworkUsage;
                        processModel.DiskUsage = metrics.DiskUsage;
                        ProcessesView.Refresh();
                    });
                }

                await Task.Delay(1000, token);
            }
        }

        private async Task<(double CpuUsage, double MemoryUsage, double NetworkUsage, double DiskUsage)> GetProcessMetricsAsync(Process process)
        {
            var tasks = new[]
            {
                performanceMetricsHelper.GetCpuUsageAsync(process),
                Task.FromResult(Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1)),
                performanceMetricsHelper.GetNetworkUsageAsync(),
                performanceMetricsHelper.GetDiskUsageAsync(process)
            };
            await Task.WhenAll(tasks);
            return (tasks[0].Result, tasks[1].Result, tasks[2].Result, tasks[3].Result);
        }

        private bool HasPermissionToAccessProcess(Process process)
        {
            try
            {
                var handle = process.Handle;
                return true;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}