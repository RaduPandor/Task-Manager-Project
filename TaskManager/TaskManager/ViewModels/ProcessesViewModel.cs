using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class ProcessesViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsService;
        private CancellationTokenSource linkedCancellationTokenSource;
        private Task runningTask;

        public ProcessesViewModel(PerformanceMetricsService performanceMetricsService)
        {
            this.performanceMetricsService = performanceMetricsService;
            Processes = new ObservableCollection<ProcessViewModel>();
        }

        public ObservableCollection<ProcessViewModel> Processes { get; }

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            runningTask = Task.Run(async () => await LoadProcessesAsync(token));
            await runningTask;
        }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            Processes.Clear();
            if (runningTask == null)
            {
                return;
            }

            runningTask = null;
        }

        private async Task LoadProcessesAsync(CancellationToken token)
        {
            var processes = Process.GetProcesses();
            var filteredProcesses = IsRunningAsAdmin()
              ? processes.Where(p => HasPermissionToAccessProcess(p))
               : processes.Where(p =>
               {
                   var owner = performanceMetricsService.GetProcessOwner(p.Id);
                   return owner != string.Empty && !IsSystemUser(owner);
               });

            foreach (var process in filteredProcesses)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var processModel = new ProcessViewModel
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
                    _ = Task.Run(() => UpdateProcessMetricsAsync(processModel, token));
                }
            }
        }

        private async Task UpdateProcessMetricsAsync(ProcessViewModel processModel, CancellationToken token)
        {
            try
            {
                using (var process = Process.GetProcessById(processModel.Id))
                {
                    if (process?.HasExited != false)
                    {
                        return;
                    }

                    while (!token.IsCancellationRequested && !process.HasExited)
                    {
                        var cpuUsage = await performanceMetricsService.GetCpuUsageAsync(process);
                        var memoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);
                        var diskUsage = await performanceMetricsService.GetDiskUsageAsync(process);
                        var networkUsage = await performanceMetricsService.GetNetworkUsageAsync();

                        if (Math.Abs(processModel.CpuUsage - cpuUsage) > 0.1 ||
                             Math.Abs(processModel.MemoryUsage - memoryUsage) > 50 ||
                             Math.Abs(processModel.NetworkUsage - networkUsage) > 0.1 ||
                             Math.Abs(processModel.DiskUsage - diskUsage) > 0.1)
                        {
                            await App.Current.Dispatcher.InvokeAsync(() =>
                            {
                                processModel.CpuUsage = cpuUsage;
                                processModel.MemoryUsage = memoryUsage;
                                processModel.NetworkUsage = networkUsage;
                                processModel.DiskUsage = diskUsage;
                            });
                        }

                        if (!token.IsCancellationRequested)
                        {
                            await Task.Delay(1000, token);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"Task cancelled for: {processModel.Name} {processModel.Id}");
            }
            catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
            {
                await App.Current.Dispatcher.InvokeAsync(() => Processes.Remove(processModel));
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

        private bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private bool HasPermissionToAccessProcess(Process process)
        {
            try
            {
                var handle = process.Handle;
                return true;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                return false;
            }
        }
    }
}