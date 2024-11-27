using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    public class PerformanceMetricsHelper : IPerformanceMetricsHelper
    {
        private const uint Token = 0x0008;
        private const uint TokenUser = 1;
        private readonly INativeMethodsService nativeMethodsService;

        public PerformanceMetricsHelper(INativeMethodsService nativeMethods)
        {
            nativeMethodsService = nativeMethods;
        }

        public async Task<double> GetCpuUsageAsync(Process process)
        {
            return await Task.Run(async () =>
            {
                TimeSpan startCpuUsage = process.TotalProcessorTime;
                DateTime startTime = DateTime.UtcNow;
                await Task.Delay(50);
                TimeSpan endCpuUsage = process.TotalProcessorTime;
                DateTime endTime = DateTime.UtcNow;
                double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                double totalMsPassed = (endTime - startTime).TotalMilliseconds;
                double cpuUsageTotal = (cpuUsedMs / totalMsPassed) / Environment.ProcessorCount * 100;
                return Math.Round(cpuUsageTotal, 1);
            });
        }

        public async Task<double> GetDiskUsageAsync(Process process)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using PerformanceCounter diskReadCounter = new ("Process", "IO Read Bytes/sec", process.ProcessName, true);
                    using PerformanceCounter diskWriteCounter = new ("Process", "IO Write Bytes/sec", process.ProcessName, true);
                    diskReadCounter.NextValue();
                    diskWriteCounter.NextValue();
                    float diskReads = diskReadCounter.NextValue();
                    float diskWrites = diskWriteCounter.NextValue();
                    return Math.Round((diskReads + diskWrites) / (1024.0 * 1024.0), 1);
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            });
        }

        public async Task<double> GetNetworkUsageAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string[] networkAdapters = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
                    double totalSent = 0;
                    double totalReceived = 0;

                    foreach (string adapter in networkAdapters)
                    {
                        using PerformanceCounter networkSentCounter = new ("Network Interface", "Bytes Sent/sec", adapter);
                        using PerformanceCounter networkReceivedCounter = new ("Network Interface", "Bytes Received/sec", adapter);
                        networkSentCounter.NextValue();
                        networkReceivedCounter.NextValue();
                        totalSent += networkSentCounter.NextValue();
                        totalReceived += networkReceivedCounter.NextValue();
                    }

                    return Math.Round((totalSent + totalReceived) / (1024.0 * 1024.0), 1);
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            });
        }

        public string GetProcessStatus(Process process)
        {
            if (IsProcessSuspended(process))
            {
                return "Suspended";
            }

            if (IsProcessInEfficiencyMode(process))
            {
                return "Efficiency mode";
            }

            return " ";
        }

        public async Task<string> GetProcessOwnerAsync(int processId)
        {
            return await Task.Run(() =>
            {
                IntPtr processHandle = nativeMethodsService.OpenProcess(0x0400 | 0x0010, false, processId);
                if (processHandle == IntPtr.Zero)
                {
                    return " ";
                }

                try
                {
                    return GetOwnerFromToken(processHandle);
                }
                finally
                {
                    nativeMethodsService.CloseHandle(processHandle);
                }
            });
        }

        public string GetProcessOwner(int processId)
        {
            IntPtr processHandle = nativeMethodsService.OpenProcess(0x0400 | 0x0010, false, processId);
            if (processHandle == IntPtr.Zero)
            {
                return " ";
            }

            try
            {
                return GetOwnerFromToken(processHandle);
            }
            finally
            {
                nativeMethodsService.CloseHandle(processHandle);
            }
        }

        private static bool IsProcessSuspended(Process process)
        {
            return false;
        }

        private static bool IsProcessInEfficiencyMode(Process process)
        {
            try
            {
                return process.PriorityClass == ProcessPriorityClass.BelowNormal;
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                return false;
            }
        }

        private string GetOwnerFromToken(IntPtr processHandle)
        {
            IntPtr tokenHandle;
            if (!nativeMethodsService.OpenProcessToken(processHandle, Token, out tokenHandle))
            {
                return " ";
            }

            try
            {
                return ResolveTokenUser(tokenHandle);
            }
            finally
            {
                nativeMethodsService.CloseHandle(tokenHandle);
            }
        }

        private string ResolveTokenUser(IntPtr tokenHandle)
        {
            nativeMethodsService.GetTokenInformation(tokenHandle, TokenUser, IntPtr.Zero, 0, out uint tokenInfoLength);
            IntPtr tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);

            try
            {
                if (nativeMethodsService.GetTokenInformation(tokenHandle, TokenUser, tokenInfo, tokenInfoLength, out tokenInfoLength))
                {
                    var tokenUser = (NativeMethods.TOKEN_USER)Marshal.PtrToStructure(tokenInfo, typeof(NativeMethods.TOKEN_USER));
                    return nativeMethodsService.LookupAccountName(tokenUser.User.Sid);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInfo);
            }

            return " ";
        }
    }
}
