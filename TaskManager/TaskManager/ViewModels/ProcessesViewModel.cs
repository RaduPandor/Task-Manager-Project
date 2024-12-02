﻿using System;
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
                cachedProcesses.Clear();
            }

            disposed = true;
        }

        private async Task LoadProcessesAsync(CancellationToken token)
        {
            cachedProcesses = Process.GetProcesses()
                                      .ToDictionary(p => p.Id, p => p);

            await Task.Run(() =>
            {
                Parallel.ForEach(cachedProcesses.Values, process =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var processModel = new ProcessModel();
                    try
                    {
                        processModel.Name = process.ProcessName;
                        processModel.Id = process.Id;
                        processModel.MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);
                        processModel.CpuUsage = 0;
                        processModel.NetworkUsage = 0;
                        processModel.DiskUsage = 0;
                    }
                    catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
                    {
                        return;
                    }

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
            if (!cachedProcesses.TryGetValue(processModel.Id, out var process))
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
                catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
                {
                    break;
                }
            }
        }
    }
}