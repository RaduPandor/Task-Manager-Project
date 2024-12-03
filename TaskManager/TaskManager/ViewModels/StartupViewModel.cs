using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class StartupViewModel : BaseViewModel, ILoadableViewModel
    {
        public StartupViewModel()
        {
            StartupPrograms = new ObservableCollection<StartupModel>();
        }

        public ObservableCollection<StartupModel> StartupPrograms { get; }

        public void OnNavigatedFrom()
        {
            StartupPrograms.Clear();
        }

        public async Task OnNavigatedToAsync()
        {
            await Task.Run(() => LoadDataAsync());
        }

        private async Task LoadDataAsync()
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

            await Task.Run(() =>
            {
                foreach (var path in startupPaths)
                {
                    CheckStartupPrograms(path, RegistryHive.LocalMachine);
                    CheckStartupPrograms(path, RegistryHive.CurrentUser);
                }
            });
        }

        private void CheckStartupPrograms(string registryPath, RegistryHive hive)
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                return string.Empty;
            }
            catch (Exception)
            {
                throw new NotImplementedException();
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                return "Not measured";
            }
            catch (Exception)
            {
                throw new NotImplementedException();
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                return false;
            }
            catch (Exception)
            {
                throw new NotImplementedException();
            }
        }
    }
}