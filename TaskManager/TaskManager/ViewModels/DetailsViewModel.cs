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
    public class DetailsViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsService;
        private CancellationTokenSource linkedCancellationTokenSource;
        private Task runningTask;

        public DetailsViewModel(PerformanceMetricsService performanceMetricsService)
        {
            this.performanceMetricsService = performanceMetricsService;
            Processes = new ObservableCollection<DetailsInfoViewModel>();
        }

        public ObservableCollection<DetailsInfoViewModel> Processes { get; }

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

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            runningTask = Task.Run(async () => await LoadProcessesAsync(token));
            await runningTask;
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

                var processModel = new DetailsInfoViewModel
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

        private async Task UpdateProcessMetricsAsync(DetailsInfoViewModel processModel, CancellationToken token)
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
                        var memoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 3);

                        if (Math.Abs(double.Parse(processModel.CpuUsage) - cpuUsage) > 0.1 ||
                              Math.Abs(processModel.MemoryUsage - memoryUsage) > 50)
                        {
                            await App.Current.Dispatcher.InvokeAsync(() =>
                            {
                                processModel.CpuUsage = cpuUsage.ToString();
                                processModel.MemoryUsage = memoryUsage;
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
            catch (Exception ex) when (ex is ArgumentException)
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