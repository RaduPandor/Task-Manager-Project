using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class AppHistoryViewModel : BaseViewModel
    {
        private readonly PerformanceMetricsHelper performanceMetricsHelper;

        public AppHistoryViewModel(PerformanceMetricsHelper performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            AppHistory = new ObservableCollection<AppHistoryModel>();
            LoadAppHistoryAsync();
        }

        public ObservableCollection<AppHistoryModel> AppHistory { get; }

        private async Task LoadAppHistoryAsync()
        {
            var apps = Process.GetProcesses()
                              .Where(p => p.MainWindowHandle != IntPtr.Zero)
                              .ToArray();

            var appModels = new List<AppHistoryModel>();
            var tasks = apps.Select(async app =>
            {
                var appModel = new AppHistoryModel
                {
                    Name = app.ProcessName,
                    CPUTime = GetFormattedCpuTime(app),
                    NetworkUsage = await performanceMetricsHelper.GetNetworkUsageAsync()
                };

                appModels.Add(appModel);
            }).ToArray();

            await Task.WhenAll(tasks);

            App.Current.Dispatcher.Invoke(() =>
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
            catch (Exception ex) when (ex is InvalidOperationException || ex is Win32Exception)
            {
                return "0:00:00";
            }
        }
    }
}
