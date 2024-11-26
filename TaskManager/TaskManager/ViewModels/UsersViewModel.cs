using System;
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
    public class UsersViewModel : BaseViewModel, ICancellableViewModel, IDisposable
    {
        private readonly PerformanceMetricsHelper performanceMetricsHelper;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;
        private ICollectionView usersView;

        public UsersViewModel(PerformanceMetricsHelper performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            Users = new ObservableCollection<UsersModel>();
            UsersView = CollectionViewSource.GetDefaultView(Users);
            cancellationTokenSource = new CancellationTokenSource();
            LoadUsersAsync(cancellationTokenSource.Token);
        }

        public ObservableCollection<UsersModel> Users { get; }

        public ICollectionView UsersView
        {
            get => usersView;
            private set
            {
                usersView = value;
                OnPropertyChanged(nameof(UsersView));
            }
        }

        public void StopMonitoring()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
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

        private async Task LoadUsersAsync(CancellationToken token)
        {
            var processes = Process.GetProcesses().GroupBy(p => performanceMetricsHelper.GetProcessOwner(p.Id))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && !IsSystemUser(g.Key))
                .ToList();

            foreach (var group in processes)
            {
                var userName = group.Key;
                var processCount = group.Count();
                var userModel = new UsersModel(userName, processCount);
                Users.Add(userModel);
                Task.Run(() => LoadDynamicUserMetricsAsync(userModel, group, token), token);
            }
        }

        private async Task LoadDynamicUserMetricsAsync(UsersModel userModel, IGrouping<string, Process> processGroup, CancellationToken token)
        {
            userModel.Processes.Clear();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tasks = processGroup.Select(process =>
                    {
                        var processModel = new ProcessModel
                        {
                            Name = process.ProcessName,
                            Status = performanceMetricsHelper.GetProcessStatus(process)
                        };

                        return Task.Run(async () =>
                        {
                            processModel.MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);
                            processModel.CpuUsage = await performanceMetricsHelper.GetCpuUsageAsync(process);
                            processModel.DiskUsage = await performanceMetricsHelper.GetDiskUsageAsync(process);
                            processModel.NetworkUsage = await performanceMetricsHelper.GetNetworkUsageAsync();

                            return processModel;
                        });
                    }).ToArray();

                    var processModels = await Task.WhenAll(tasks);
                    foreach (var processModel in processModels)
                    {
                        userModel.Processes.Add(processModel);
                    }

                    var totalMemoryUsage = processGroup.Sum(p => Math.Round(p.WorkingSet64 / (1024.0 * 1024.0), 1));
                    var totalCpuUsage = userModel.Processes.Sum(p => p.CpuUsage);
                    var totalDiskUsage = userModel.Processes.Sum(p => p.DiskUsage);
                    var totalNetworkUsage = userModel.Processes.Sum(p => p.NetworkUsage);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        userModel.TotalMemoryUsage = totalMemoryUsage;
                        userModel.TotalCpuUsage = totalCpuUsage;
                        userModel.TotalDiskUsage = totalDiskUsage;
                        userModel.TotalNetworkUsage = totalNetworkUsage;
                        UsersView.Refresh();
                    });

                    await Task.Delay(2000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private bool IsSystemUser(string userName)
        {
            var systemUsers = new[]
            {
                "SYSTEM",
                "LOCAL SERVICE",
                "NETWORK SERVICE",
                "DefaultAccount"
            };

            return systemUsers.Contains(userName, StringComparer.OrdinalIgnoreCase)
                   || userName.StartsWith("UMFD", StringComparison.OrdinalIgnoreCase)
                   || userName.StartsWith("DWM", StringComparison.OrdinalIgnoreCase);
        }
    }
}
