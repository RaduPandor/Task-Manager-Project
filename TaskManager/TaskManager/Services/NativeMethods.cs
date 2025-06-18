using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TaskManager.Services
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr tokenHandle, uint tokenInformationClass, IntPtr tokenInformation, uint tokenInformationLength, out uint returnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LookupAccountSid(string systemName, IntPtr lpSid, StringBuilder lpName, ref int cchName, StringBuilder lpDomainName, ref int cchDomainName, out uint peUse);

        public static string LookupAccountName(IntPtr sid)
        {
            var nameLength = 256;
            var domainNameLength = 256;
            var name = new StringBuilder(nameLength);
            var domainName = new StringBuilder(domainNameLength);
            if (LookupAccountSid(null, sid, name, ref nameLength, domainName, ref domainNameLength, out _))
            {
                return $"{name}";
            }

            return string.Empty;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }
    }
}
