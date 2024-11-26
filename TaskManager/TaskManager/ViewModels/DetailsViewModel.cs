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
    public class DetailsViewModel : BaseViewModel, ICancellableViewModel, IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly PerformanceMetricsHelper performanceMetricsHelper;
        private bool disposed;
        private ICollectionView processesView;

        public DetailsViewModel(PerformanceMetricsHelper performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            Processes = new ObservableCollection<DetailsModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
            ProcessesView.SortDescriptions.Add(new SortDescription(nameof(DetailsModel.CpuUsage), ListSortDirection.Descending));
            cancellationTokenSource = new CancellationTokenSource();
            LoadProcessesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }

        ~DetailsViewModel()
        {
            Dispose(false);
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

                    var processModel = new DetailsModel
                    {
                        Name = process.ProcessName,
                        Id = process.Id,
                        Status = process.Responding ? "Running" : "Suspended",
                        UserName = await performanceMetricsHelper.GetProcessOwnerAsync(process.Id),
                        CpuUsage = "0",
                        MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 3),
                        Architecture = Environment.Is64BitProcess ? "x64" : "x86",
                        Description = GetProcessDescription(process)
                    };

                    App.Current.Dispatcher.Invoke(() => Processes.Add(processModel));
                }, token));
            }

            await Task.WhenAll(tasks);
            foreach (var processModel in Processes)
            {
                if (!token.IsCancellationRequested)
                {
                    Task.Run(() => UpdateProcessMetricsPeriodicallyAsync(processModel, token), token);
                }
            }
        }

        private async Task UpdateProcessMetricsPeriodicallyAsync(DetailsModel processModel, CancellationToken token)
        {
            var process = Process.GetProcesses().FirstOrDefault(p => p.Id == processModel.Id);

            if (process == null)
            {
                return;
            }

            while (!process.HasExited && !token.IsCancellationRequested)
            {
                processModel.CpuUsage = (await performanceMetricsHelper.GetCpuUsageAsync(process)).ToString();
                processModel.MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 3);
                App.Current.Dispatcher.Invoke(() =>
                {
                    var item = Processes.FirstOrDefault(p => p.Id == processModel.Id);
                    if (item == null)
                    {
                        return;
                    }

                    item.CpuUsage = processModel.CpuUsage;
                    item.MemoryUsage = processModel.MemoryUsage;
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
        }
    }
}
