using System;
using System.Text;

namespace TaskManager.Services
{
    public interface INativeMethodsService
    {
        IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

        bool CloseHandle(IntPtr handle);

        bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        bool GetTokenInformation(IntPtr tokenHandle, uint tokenInformationClass, IntPtr tokenInformation, uint tokenInformationLength, out uint returnLength);

#pragma warning disable SA1305 // Field names should not use Hungarian notation
        bool LookupAccountSid(string systemName, IntPtr lpSid, StringBuilder lpName, ref int cchName, StringBuilder lpDomainName, ref int cchDomainName, out uint peUse);
#pragma warning restore SA1305 // Field names should not use Hungarian notation

        string LookupAccountName(IntPtr sid);
    }
}
