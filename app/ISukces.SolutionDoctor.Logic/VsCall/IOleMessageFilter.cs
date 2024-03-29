using System.Runtime.InteropServices;

namespace ISukces.SolutionDoctor.Logic.VsCall
{
    /// <summary>
    ///     Copied wholesale from: https://msdn.microsoft.com/en-us/library/ms228772.aspx
    /// </summary>
    [ComImport]
    [Guid("00000016-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(
            int dwCallType,
            IntPtr hTaskCaller,
            int dwTickCount,
            IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwRejectType);

        [PreserveSig]
        int MessagePending(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwPendingType);
    }
}
