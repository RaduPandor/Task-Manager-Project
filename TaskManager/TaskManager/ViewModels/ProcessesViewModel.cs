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
    public class ProcessesViewModel : BaseViewModel, IDisposable, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsHelper;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;
        private ICollectionView processesView;

        public ProcessesViewModel(PerformanceMetricsService performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            Processes = new ObservableCollection<ProcessModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
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

        public void OnNavigatedFrom()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task OnNavigatedToAsync()
        {
            cancellationTokenSource = new CancellationTokenSource();
            await LoadProcessesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                Processes.Clear();
            }

            disposed = true;
        }

        private async Task LoadProcessesAsync(CancellationToken token)
        {
            var processes = Process.GetProcesses();

            await Task.Run(() =>
            {
                Parallel.ForEach(processes, process =>
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
                });
            });

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
                    await Task.Delay(2000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}