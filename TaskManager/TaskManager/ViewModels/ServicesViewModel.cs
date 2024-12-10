using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class ServicesViewModel : BaseViewModel, ILoadableViewModel
    {
        private CancellationTokenSource linkedCancellationTokenSource;

        public ServicesViewModel()
        {
            Services = new ObservableCollection<ServicesModel>();
        }

        public ObservableCollection<ServicesModel> Services { get; }

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            await Task.Run(() => LoadDataAsync(token), token);
        }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            Services.Clear();
        }

        private async Task LoadDataAsync(CancellationToken token)
        {
            var servicesList = new List<ServicesModel>();
            var tasks = ServiceController.GetServices().Select(async service =>
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                return new ServicesModel
                {
                    Name = service.ServiceName,
                    Id = await GetServiceProcessIdAsync(service.ServiceName),
                    Description = await GetServiceDescriptionAsync(service.ServiceName),
                    Status = service.Status.ToString(),
                    GroupName = await GetServiceGroupNameAsync(service.ServiceName)
                };
            });

            var allServices = await Task.WhenAll(tasks);

            if (token.IsCancellationRequested)
            {
                return;
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var serviceModel in allServices.Where(s => s != null))
                {
                    Services.Add(serviceModel);
                }
            });
        }

        private async Task<string> GetServiceProcessIdAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var sc = new ServiceController(serviceName))
                    {
                        var process = Process.GetProcessesByName(serviceName);
                        if (process.Length > 0)
                        {
                            return process[0].Id.ToString();
                        }
                    }
                }
                catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
                {
                    return string.Empty;
                }

                return string.Empty;
            });
        }

        private async Task<string> GetServiceDescriptionAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string registryPath = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            object description = key.GetValue("Description");
                            string descriptionString = description?.ToString() ?? string.Empty;
                            if (descriptionString.StartsWith("@"))
                            {
                                return string.Empty;
                            }

                            return descriptionString;
                        }
                    }
                }
                catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
                {
                    return string.Empty;
                }

                return string.Empty;
            });
        }

        private async Task<string> GetServiceGroupNameAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string registryPath = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            object groupName = key.GetValue("Group");
                            return groupName?.ToString() ?? string.Empty;
                        }
                    }
                }
                catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
                {
                    return string.Empty;
                }

                return string.Empty;
            });
        }
    }
}
