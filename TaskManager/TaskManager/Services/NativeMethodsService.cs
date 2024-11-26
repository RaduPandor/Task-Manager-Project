using System;
using System.Text;

namespace TaskManager.Services
{
    public class NativeMethodsService : INativeMethodsService
    {
        public IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId)
        {
            return NativeMethods.OpenProcess(processAccess, inheritHandle, processId);
        }

        public bool CloseHandle(IntPtr handle)
        {
            return NativeMethods.CloseHandle(handle);
        }

        public bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle)
        {
            return NativeMethods.OpenProcessToken(processHandle, desiredAccess, out tokenHandle);
        }

        public bool GetTokenInformation(IntPtr tokenHandle, uint tokenInformationClass, IntPtr tokenInformation, uint tokenInformationLength, out uint returnLength)
        {
            return NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, tokenInformation, tokenInformationLength, out returnLength);
        }

        public bool LookupAccountSid(string systemName, IntPtr lpSid, StringBuilder lpName, ref int cchName, StringBuilder lpDomainName, ref int cchDomainName, out uint peUse)
        {
            return NativeMethods.LookupAccountSid(systemName, lpSid, lpName, ref cchName, lpDomainName, ref cchDomainName, out peUse);
        }

        public string LookupAccountName(IntPtr sid)
        {
            return NativeMethods.LookupAccountName(sid);
        }
    }
}
