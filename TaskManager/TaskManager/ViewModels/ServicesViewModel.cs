﻿using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class ServicesViewModel : BaseViewModel, ILoadableViewModel
    {
        public ServicesViewModel()
        {
            Services = new ObservableCollection<ServicesModel>();
        }

        public ObservableCollection<ServicesModel> Services { get; }

        public async Task LoadDataAsync()
        {
            var servicesList = new List<ServicesModel>();
            var tasks = ServiceController.GetServices().Select(async service =>
            {
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
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var serviceModel in allServices)
                {
                    Services.Add(serviceModel);
                }
            });
        }

        private async Task<string> GetServiceProcessIdAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                using (var sc = new ServiceController(serviceName))
                {
                    var process = Process.GetProcessesByName(serviceName);
                    if (process.Length > 0)
                    {
                        return process[0].Id.ToString();
                    }
                }

                return string.Empty;
            });
        }

        private async Task<string> GetServiceDescriptionAsync(string serviceName)
        {
            return await Task.Run(() =>
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

                return string.Empty;
            });
        }

        private async Task<string> GetServiceGroupNameAsync(string serviceName)
        {
            return await Task.Run(() =>
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

                return string.Empty;
            });
        }
    }
}
