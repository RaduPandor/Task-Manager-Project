﻿using DocumentFormat.OpenXml.Drawing;
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
    public class UsersViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly PerformanceMetricsService performanceMetricsService;
        private CancellationTokenSource linkedCancellationTokenSource;

        public UsersViewModel(PerformanceMetricsService performanceMetricsService)
        {
            this.performanceMetricsService = performanceMetricsService;
            Users = new ObservableCollection<UserViewModel>();
        }

        public ObservableCollection<UserViewModel> Users { get; }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            Users.Clear();
        }

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            CancellationToken token = linkedCancellationTokenSource.Token;
            await Task.Run(() => LoadUsersAsync(token), token);
        }

        private async Task LoadUsersAsync(CancellationToken token)
        {
            var processes = Process.GetProcesses()
                .GroupBy(p => performanceMetricsService.GetProcessOwner(p.Id))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && !IsSystemUser(g.Key))
                .ToList();

            var tasks = new List<Task>();

            foreach (var group in processes)
            {
                var userName = group.Key;
                var processCount = group.Count();
                var userModel = new UserViewModel(userName, processCount);
                System.Windows.Application.Current.Dispatcher.Invoke(() => Users.Add(userModel));
                var loadMetricsTask = LoadDynamicUserMetricsAsync(userModel, group, token);
                tasks.Add(loadMetricsTask);
            }

            await Task.WhenAll(tasks);
        }

        private async Task LoadDynamicUserMetricsAsync(UserViewModel userModel, IGrouping<string, Process> processGroup, CancellationToken token)
        {
            userModel.Processes.Clear();

            try
            {
                var tasks = processGroup.Select(process =>
                {
                    var processModel = new ProcessModel
                    {
                        Name = process.ProcessName
                    };

                    return Task.Run(async () =>
                    {
                        processModel.MemoryUsage = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 1);
                        processModel.CpuUsage = await performanceMetricsService.GetCpuUsageAsync(process);
                        processModel.DiskUsage = await performanceMetricsService.GetDiskUsageAsync(process);
                        processModel.NetworkUsage = await performanceMetricsService.GetNetworkUsageAsync();

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
                });
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                throw;
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