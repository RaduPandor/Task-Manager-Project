using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class DetailsViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsHelper;
        private ICollectionView processesView;

        public DetailsViewModel(PerformanceMetricsService performanceMetricsHelper)
        {
            this.performanceMetricsHelper = performanceMetricsHelper;
            Processes = new ObservableCollection<DetailsModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
            ProcessesView.SortDescriptions.Add(new SortDescription(nameof(DetailsModel.CpuUsage), ListSortDirection.Descending));
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

        public void OnNavigatedFrom()
        {
            Processes.Clear();
        }

        public async Task OnNavigatedToAsync()
        {
            await Task.Run(() => LoadProcessesAsync());
        }

        private async Task LoadProcessesAsync()
        {
            var processes = Process.GetProcesses();
            var tasks = processes.Select(async process =>
            {
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
            });

            await Task.WhenAll(tasks);

            foreach (var processModel in Processes)
            {
                _ = Task.Run(() => UpdateProcessMetricsPeriodicallyAsync(processModel));
            }
        }

        private async Task UpdateProcessMetricsPeriodicallyAsync(DetailsModel processModel)
        {
            var process = Process.GetProcesses().FirstOrDefault(p => p.Id == processModel.Id);

            if (process?.HasExited != false)
            {
                return;
            }

            try
            {
                while (!process.HasExited)
                {
                    var cpuUsage = await performanceMetricsHelper.GetCpuUsageAsync(process);
                    var memoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 3);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var item = Processes.FirstOrDefault(p => p.Id == processModel.Id);
                        if (item != null)
                        {
                            item.CpuUsage = cpuUsage.ToString();
                            item.MemoryUsage = memoryUsage;
                        }

                        ProcessesView.Refresh();
                    });

                    await Task.Delay(2000);
                }
            }
            catch (TaskCanceledException)
            {
                throw new NotImplementedException();
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
            catch (Exception)
            {
                throw new NotImplementedException();
            }
        }
    }
}