using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class StartupViewModel : BaseViewModel, ILoadableViewModel
    {
        private CancellationTokenSource linkedCancellationTokenSource;
        private Task runningTask;

        public StartupViewModel()
        {
            StartupPrograms = new ObservableCollection<StartupModel>();
        }

        public ObservableCollection<StartupModel> StartupPrograms { get; }

        public void OnNavigatedFrom()
        {
            linkedCancellationTokenSource?.Cancel();
            linkedCancellationTokenSource?.Dispose();
            linkedCancellationTokenSource = null;
            StartupPrograms.Clear();
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
            runningTask = Task.Run(() => LoadDataAsync(token), token);
            await runningTask;
        }

        private async Task LoadDataAsync(CancellationToken token)
        {
            var startupPaths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Userinit",
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell"
            };

            foreach (var path in startupPaths)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await Task.Run(() => CheckStartupPrograms(path, RegistryHive.LocalMachine, token), token);
                await Task.Run(() => CheckStartupPrograms(path, RegistryHive.CurrentUser, token), token);
            }
        }

        private void CheckStartupPrograms(string registryPath, RegistryHive hive, CancellationToken token)
        {
            RegistryKey? key = null;
            try
            {
                key = hive == RegistryHive.LocalMachine
                    ? Registry.LocalMachine.OpenSubKey(registryPath)
                    : Registry.CurrentUser.OpenSubKey(registryPath);

                if (key == null)
                {
                    return;
                }

                foreach (string valueName in key.GetValueNames())
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    string programPath = key.GetValue(valueName) as string;
                    programPath = Environment.ExpandEnvironmentVariables(programPath);

                    var processModel = new StartupModel
                    {
                        Name = Path.GetFileNameWithoutExtension(programPath),
                        Publisher = GetPublisher(programPath),
                        Status = IsStartupAppEnabled(key, valueName) ? "Enabled" : "Disabled",
                        StartupImpact = CalculateStartupImpact(programPath)
                    };

                    App.Current.Dispatcher.Invoke(() => StartupPrograms.Add(processModel));
                }
            }
            finally
            {
                key?.Close();
            }
        }

        private string GetPublisher(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                    return versionInfo.CompanyName ?? string.Empty;
                }
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private string CalculateStartupImpact(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    if (fileInfo.Length > 50 * 1024 * 1024)
                    {
                        return "High";
                    }
                    else if (fileInfo.Length > 10 * 1024 * 1024)
                    {
                        return "Medium";
                    }
                    else
                    {
                        return "Low";
                    }
                }
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return "Not measured";
            }

            return "None";
        }

        private bool IsStartupAppEnabled(RegistryKey key, string valueName)
        {
            try
            {
                object? value = key.GetValue(valueName);
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return false;
                }

                string expandedPath = Environment.ExpandEnvironmentVariables(value.ToString());
                string executablePath = expandedPath.Split(' ')[0];

                return File.Exists(executablePath);
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}