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
    public class ProcessesViewModel : BaseViewModel, ICancellableViewModel, IDisposable
    {
        private readonly PerformanceMetricsHelper performanceMetricsHelper;
        private readonly CancellationTokenSource cancellationTokenSource;
        private bool disposed;
        private ICollectionView processesView;

        public ProcessesViewModel(PerformanceMetricsHelper performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            Processes = new ObservableCollection<ProcessModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
            cancellationTokenSource = new CancellationTokenSource();
            LoadProcessesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }

        ~ProcessesViewModel()
        {
            Dispose(false);
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

        public void StopMonitoring()
        {
            if (cancellationTokenSource == null)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
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
                Processes.Clear();
            }

            disposed = true;
        }

        private async Task LoadProcessesAsync(CancellationToken token)
        {
            var processes = Process.GetProcesses();
            var tasks = new List<Task>();

            foreach (var process in processes)
            {
                tasks.Add(Task.Run(
                    async () =>
                    {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var processModel = new ProcessModel
                    {
                        Name = process.ProcessName,
                        Id = process.Id,
                        Status = performanceMetricsHelper.GetProcessStatus(process),
                        CpuUsage = 0,
                        MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1),
                        NetworkUsage = 0,
                        DiskUsage = 0
                    };

                    App.Current.Dispatcher.Invoke(() => Processes.Add(processModel));
                }, token));
            }

            await Task.WhenAll(tasks);

            foreach (var processModel in Processes)
            {
                if (!token.IsCancellationRequested)
                {
                    Task.Run(() => UpdateProcessMetricsAsync(processModel, token), token);
                }
            }
        }

        private async Task UpdateProcessMetricsAsync(ProcessModel processModel, CancellationToken token)
        {
            var process = Process.GetProcesses().FirstOrDefault(p => p.Id == processModel.Id);
            if (process == null)
            {
                return;
            }

            while (!process.HasExited && !token.IsCancellationRequested)
            {
                processModel.CpuUsage = await performanceMetricsHelper.GetCpuUsageAsync(process);
                processModel.MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);
                processModel.NetworkUsage = await performanceMetricsHelper.GetNetworkUsageAsync();
                processModel.DiskUsage = await performanceMetricsHelper.GetDiskUsageAsync(process);

                App.Current.Dispatcher.Invoke(() =>
                {
                    var item = Processes.FirstOrDefault(p => p.Id == processModel.Id);
                    if (item == null)
                    {
                        return;
                    }

                    item.CpuUsage = processModel.CpuUsage;
                    item.MemoryUsage = processModel.MemoryUsage;
                    item.NetworkUsage = processModel.NetworkUsage;
                    item.DiskUsage = processModel.DiskUsage;
                    ProcessesView.Refresh();
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
    }
}
