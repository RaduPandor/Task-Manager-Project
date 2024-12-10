using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class AppHistoryViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsService;
        private CancellationTokenSource linkedCancellationTokenSource;

        public AppHistoryViewModel(PerformanceMetricsService performanceMetricsService)
        {
            this.performanceMetricsService = performanceMetricsService;
            AppHistory = new ObservableCollection<AppHistoryModel>();
        }

        public ObservableCollection<AppHistoryModel> AppHistory { get; }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            AppHistory.Clear();
        }

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            await Task.Run(() => LoadDataAsync(token), token);
        }

        public async Task LoadDataAsync(CancellationToken token)
        {
            var apps = await Task.Run(() => Process.GetProcesses()
                                                   .Where(p => p.MainWindowHandle != IntPtr.Zero)
                                                   .ToArray());

            var appModels = new List<AppHistoryModel>();
            var tasks = apps.Select(async app =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                var appModel = new AppHistoryModel
                {
                    Name = app.ProcessName,
                    CPUTime = GetFormattedCpuTime(app),
                    NetworkUsage = await performanceMetricsService.GetNetworkUsageAsync()
                };

                appModels.Add(appModel);
            }).ToArray();

            await Task.WhenAll(tasks);
            if (token.IsCancellationRequested)
            {
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var appModel in appModels)
                {
                    AppHistory.Add(appModel);
                }
            });
        }

        private string GetFormattedCpuTime(Process process)
        {
            try
            {
                var cpuTime = process.TotalProcessorTime;
                return $"{cpuTime.Hours}:{cpuTime.Minutes:D2}:{cpuTime.Seconds:D2}";
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception || ex is UnauthorizedAccessException)
            {
                return "0:00:00";
            }
        }
    }
}