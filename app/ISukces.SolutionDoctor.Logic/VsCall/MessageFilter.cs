using System.Runtime.InteropServices;

namespace ISukces.SolutionDoctor.Logic.VsCall
{
    /// <summary>
    ///     Copied wholesale from: https://msdn.microsoft.com/en-us/library/ms228772.aspx
    /// </summary>
    public class MessageFilter : IOleMessageFilter
    {
        // Implement the IOleMessageFilter interface.
        [DllImport("Ole32.dll")]
        private static extern int CoRegisterMessageFilter(
            IOleMessageFilter newFilter,
            out IOleMessageFilter oldFilter);

        // Class containing the IOleMessageFilter
        // thread error-handling functions.

        // Start the filter.
        public static void Register()
        {
            if (!Registered)
            {
                Console.WriteLine("Registering message filter");
                IOleMessageFilter newFilter = new MessageFilter();
                IOleMessageFilter oldFilter = null;
                CoRegisterMessageFilter(newFilter, out oldFilter);
                Registered = true;
            }
        }

        // Done with the filter, close it.
        public static void Revoke()
        {
            IOleMessageFilter oldFilter = null;
            CoRegisterMessageFilter(null, out oldFilter);
            Registered = false;
        }

        // IOleMessageFilter functions.
        // Handle incoming thread requests.
        int IOleMessageFilter.HandleInComingCall(
            int dwCallType,
            IntPtr hTaskCaller,
            int dwTickCount,
            IntPtr lpInterfaceInfo)
        {
            // Return the flag SERVERCALL_ISHANDLED.
            return 0;
        }

        int IOleMessageFilter.MessagePending(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwPendingType)
        {
            // Return the flag PENDINGMSG_WAITDEFPROCESS.
            return 2;
        }

        // Thread call was rejected, so try again.
        int IOleMessageFilter.RetryRejectedCall(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwRejectType)
        {
            Console.WriteLine("Got rejected call: " + dwRejectType);

            // flag = SERVERCALL_RETRYLATER.
            if (dwRejectType == 2)
            {
                // We don't want to retry IMMEDIATELY, instead, sleep for 500 ms
                return 500;
            }

            // Too busy; cancel call.
            return -1;
        }

        #region properties

        private static bool Registered { get; set; }

        #endregion
    }
}
