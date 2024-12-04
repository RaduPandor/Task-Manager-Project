using DocumentFormat.OpenXml.Drawing.Diagrams;
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
        private Dictionary<int, Process> cachedProcesses;

        public ProcessesViewModel(PerformanceMetricsService performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            Processes = new ObservableCollection<ProcessModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
            cachedProcesses = new Dictionary<int, Process>();
        }

        public event EventHandler CancelRequested;

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
            await LoadProcessesAsync(cancellationTokenSource.Token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                CancelRequested?.Invoke(this, EventArgs.Empty);

                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                Processes.Clear();
                cachedProcesses.Clear();
            }

            disposed = true;
        }

        private async Task LoadProcessesAsync(CancellationToken externalToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = linkedCts.Token;

            EventHandler value = (_, _) => linkedCts.Cancel();
            CancelRequested += value;

            try
            {
                cachedProcesses = Process.GetProcesses().ToDictionary(p => p.Id, p => p);
                foreach (var process in cachedProcesses.Values)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    var processModel = new ProcessModel();

                    processModel.Name = process.ProcessName;
                    processModel.Id = process.Id;
                    processModel.MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);
                    processModel.CpuUsage = 0;
                    processModel.NetworkUsage = 0;
                    processModel.DiskUsage = 0;

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
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                CancelRequested -= value;
            }
        }

        private async Task UpdateProcessMetricsAsync(ProcessModel processModel, CancellationToken externalToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = linkedCts.Token;

            EventHandler value = (_, _) => linkedCts.Cancel();
            CancelRequested += value;

            try
            {
                if (!cachedProcesses.TryGetValue(processModel.Id, out var process) || externalToken.IsCancellationRequested)
                {
                    return;
                }

                await MonitorProcessAsync(processModel, process, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                CancelRequested -= value;
            }
        }

        private async Task MonitorProcessAsync(ProcessModel processModel, Process process, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (ProcessHasExited(process))
                {
                    RemoveExitedProcess(processModel.Id);
                    break;
                }

                await UpdateMetricsAsync(processModel, process);
                await Task.Delay(2000, token);
            }
        }

        private bool ProcessHasExited(Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return true;
            }
        }

        private void RemoveExitedProcess(int processId)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var item = Processes.FirstOrDefault(p => p.Id == processId);
                if (item == null)
                {
                    return;
                }

                Processes.Remove(item);
                ProcessesView.Refresh();
            });
        }

        private async Task UpdateMetricsAsync(ProcessModel processModel, Process process)
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
        }
    }
}