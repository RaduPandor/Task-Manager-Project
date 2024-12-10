using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    public class PerformanceMetricsService : IPerformanceMetricsService
    {
        private const uint Token = 0x0008;
        private const uint TokenUser = 1;
        private readonly INativeMethodsService nativeMethodsService;

        public PerformanceMetricsService(INativeMethodsService nativeMethods)
        {
            nativeMethodsService = nativeMethods;
        }

        public async Task<double> GetCpuUsageAsync(Process process)
        {
            try
            {
                if (process?.HasExited != false)
                {
                    return 0;
                }

                long initialCpuTime = process.TotalProcessorTime.Ticks;
                long initialSystemCpuTime = Process.GetCurrentProcess().TotalProcessorTime.Ticks;
                long initialUserCpuTime = Process.GetCurrentProcess().UserProcessorTime.Ticks;
                var stopwatch = Stopwatch.StartNew();
                await Task.Delay(500);
                stopwatch.Stop();
                long totalCpuTime = process.TotalProcessorTime.Ticks - initialCpuTime;
                long totalSystemCpuTime = Process.GetCurrentProcess().TotalProcessorTime.Ticks - initialSystemCpuTime;
                long totalUserCpuTime = Process.GetCurrentProcess().UserProcessorTime.Ticks - initialUserCpuTime;
                long elapsedMs = stopwatch.ElapsedMilliseconds;

                double cpuUsage = (double)totalCpuTime / (double)(elapsedMs * TimeSpan.TicksPerMillisecond) / Environment.ProcessorCount * 100;
                return Math.Round(cpuUsage, 1);
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return 0;
            }
        }

        public async Task<double> GetDiskUsageAsync(Process process)
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return 0;
            }
        }

        public async Task<double> GetNetworkUsageAsync()
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return 0;
            }
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return " ";
            }
            finally
            {
                nativeMethodsService.CloseHandle(processHandle);
            }
        }

        private TimeSpan GetTotalProcessorTime(Process process)
        {
            try
            {
                return process.TotalProcessorTime;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is Win32Exception || ex is InvalidOperationException)
            {
                return TimeSpan.Zero;
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return " ";
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
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                return " ";
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInfo);
            }

            return " ";
        }
    }
}
