using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.ViewModels;

namespace TaskManager.Services
{
    public class ServiceManager : IServiceManager
    {
        public async Task<List<ServiceViewModel>> GetAllServicesAsync(CancellationToken token)
        {
            var list = new List<ServiceViewModel>();
            foreach (var service in ServiceController.GetServices())
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var model = new ServiceViewModel
                {
                    Name = service.ServiceName,
                    Status = service.Status.ToString(),
                    Id = await GetProcessIdAsync(service.ServiceName),
                    Description = await GetDescriptionAsync(service.ServiceName),
                    GroupName = await GetGroupNameAsync(service.ServiceName)
                };

                list.Add(model);
            }

            return list;
        }

        public async Task<bool> StartServiceAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(serviceName);
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    }

                    return true;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> StopServiceAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(serviceName);
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }

                    return true;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
                {
                    return false;
                }
            });
        }

        public async Task<bool> RestartServiceAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(serviceName);
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }

                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    return true;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
                {
                    return false;
                }
            });
        }

        private async Task<string> GetProcessIdAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetProcessesByName(serviceName).FirstOrDefault();
                    return process?.Id.ToString() ?? string.Empty;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
                {
                    return string.Empty;
                }
            });
        }

        private async Task<string> GetDescriptionAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
                    var value = key?.GetValue("Description")?.ToString();
                    return value?.StartsWith("@") == true ? string.Empty : value ?? string.Empty;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
                {
                    return string.Empty;
                }
            });
        }

        private async Task<string> GetGroupNameAsync(string serviceName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
                    return key?.GetValue("Group")?.ToString() ?? string.Empty;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
                {
                    return string.Empty;
                }
            });
        }
    }
}
