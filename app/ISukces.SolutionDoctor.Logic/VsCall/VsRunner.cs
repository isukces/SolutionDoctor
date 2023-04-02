using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Thread = System.Threading.Thread;

namespace ISukces.SolutionDoctor.Logic.VsCall
{
    /// <summary>
    ///     This is the least hacky way I could find for running commands in the PMC from the command line
    ///     This program will open an instance of VisualStudio and execute the passed in commands into the PMC directly
    ///     From all that I could find, there was nothing that allowed this without passing through VisualStudio, which is
    ///     depressing
    /// </summary>
    public class VsRunner
    {
        private static string LockProcessing()
        {
            var lockFile = Path.GetTempFileName();
            using(var sw = new StreamWriter(lockFile))
            {
                sw.WriteLine(true);
            }

            return lockFile;
        }

        private void CleanUp()
        {
            if (DTE != null)
            {
                if (DTE.Solution != null) DTE.Solution.Close(true);

                DTE.Quit();
            }
        }

        private void CommandEvents_AfterExecute(string guid, int id, object customIn, object customOut)
        {
            PrintDebugLog(string.Format("Command Executed: GUID: {0}; ID: {1}; CustomIn: {2}; CustomOut: {3}", guid, id,
                customIn, customOut));

            // This means that PMC has loaded and loaded its sources
            if (guid == GuidsAndIds.GuidNuGetConsoleCmdSet && id == GuidsAndIds.CmdidNuGetSources)
                TransitionState(ExecutionStates.PROJECT_OPENED, ExecutionStates.NUGET_OPENED);
            else
                TransitionState(ExecutionStates.NOT_STARTED, ExecutionStates.VS_OPENED);
        }

        private void CommandEvents_BeforeExecute(string guid, int id, object customIn, object customOut,
            ref bool cancelDefault)
        {
            PrintDebugLog(string.Format("Command Sent: GUID: {0}; ID: {1}; CustomIn: {2}; CustomOut: {3}", guid, id,
                customIn, customOut));
        }

        private DTE GetDTE2()
        {
            // Get the ProgID for DTE 14.0.
            var t = Type.GetTypeFromProgID("VisualStudio.DTE." + VSVersion, true);

            // Create a new instance of the IDE.
            var obj = Activator.CreateInstance(t, true);

            // Cast the instance to DTE2 and assign to variable dte.
            var dte2 = (DTE2)obj;

            // We want to make sure that the devenv is killed when we quit();
            dte2.UserControl = false;
            return dte2.DTE;
        }

        private string GetValue(string key, string defaultValue)
        {
            var result                               = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(result)) result = defaultValue;

            return result;
        }

        private void PrintDebugLog(string message)
        {
            if (Debug)
                Console.WriteLine("{0}: {1}",
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff", CultureInfo.InvariantCulture), message);
        }

        public void Run()
        {
            if (NuGetCmds is null || NuGetCmds.Length == 0)
                return;
            if (string.IsNullOrWhiteSpace(ProjectOrSolutionPath))
                throw new Exception("project parameter cannot be empty.");

            if (!File.Exists(ProjectOrSolutionPath))
                throw new FileNotFoundException(string.Format("{0} was not found.", ProjectOrSolutionPath));

            NuGetOutputFile = Path.GetTempFileName();
            DTE             = GetDTE2();
            MessageFilter.Register();

            /*Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                DTE.Quit();

                // turn off the IOleMessageFilter
                MessageFilter.Revoke();
            };*/

            SetDelegatesForDTE();
            DTE.MainWindow.Activate();

            SpinWait(ExecutionStates.VS_OPENED);
            DTE.ExecuteCommand("File.OpenProject", ProjectOrSolutionPath);
            Console.WriteLine("...project opened");

            SpinWait(ExecutionStates.PROJECT_OPENED);
            DTE.ExecuteCommand(CmdNameForPMC);
            Console.WriteLine("...PMC opened");

            SpinWait(ExecutionStates.NUGET_OPENED);
            foreach (var nuGetCmd in NuGetCmds)
            {
                var lockFile = LockProcessing();

                try
                {
                    var cmd = string.Format("{0}; $error > {1} ; \"False\" > {2}", nuGetCmd, NuGetOutputFile, lockFile);
                    DTE.ExecuteCommand(CmdNameForPMC, cmd);
                    Console.WriteLine("... command sent");
                    Console.WriteLine(cmd);

                    var stillRunning = true;
                    while (stillRunning)
                    {
                        Thread.Sleep(500);
                        using(var sr = new StreamReader(lockFile))
                        {
                            stillRunning = Convert.ToBoolean(sr.ReadToEnd());
                        }
                    }
                }
                finally
                {
                    File.Delete(lockFile);
                }
            }

            Console.WriteLine("Completed");
            Console.WriteLine(File.ReadAllText(NuGetOutputFile));
            CleanUp();

            // turn off the IOleMessageFilter
            MessageFilter.Revoke();
        }

        /// <summary>
        ///     Failures can occure when accessing the newly created DTE2 instance.
        ///     This is due to COM Interop errors in Windows.
        ///     The MessageFilter should catch failures, but if a particular one leaks through, restart this initial setting
        /// </summary>
        private void SetDelegatesForDTE()
        {
            try
            {
                DTE.Events.SolutionEvents.Opened       += SolutionEvents_Opened;
                DTE.Events.CommandEvents.AfterExecute  += CommandEvents_AfterExecute;
                DTE.Events.CommandEvents.BeforeExecute += CommandEvents_BeforeExecute;
            }
            catch (COMException ex)
            {
                Console.WriteLine("Exception encountered: " + ex.Message);
                if (retry)
                {
                    retry = false;
                    Thread.Sleep(500);
                    SetDelegatesForDTE();
                }
            }
        }

        private void SolutionEvents_Opened()
        {
            TransitionState(ExecutionStates.VS_OPENED, ExecutionStates.PROJECT_OPENED);
        }

        /// <summary>
        ///     We need to insure that particular parts of the main STAThread do not continue until feed back of other actions is
        ///     received
        ///     Since we could potentially get a failure from the MessageFilter and a retry.
        ///     So, using a Monitor would not work as potentially the `Pulse` would occur before the `Wait`
        ///     Hence, the use of a naive state machine
        /// </summary>
        /// <param name="expectedState">The state desired necessary to continue.</param>
        private void SpinWait(string expectedState)
        {
            while (state != expectedState)
                Thread.Sleep(500);
        }

        private void TransitionState(string expectedOldState, string newState)
        {
            lock(stateMutex)
            {
                if (state == expectedOldState)
                {
                    state = newState;
                    PrintDebugLog(string.Format("Transitioned from {0} to {1}", expectedOldState, newState));
                }
            }
        }

        #region properties

        private string VSVersion { get; set; }

        private string ProjectOrSolutionPath { get; set; }

        private string[] NuGetCmds { get; set; }

        private bool Debug { get; set; }

        private DTE DTE { get; set; }

        private string NuGetOutputFile { get; set; }

        #endregion

        #region Fields

        private const string CmdNameForPMC = "View.PackageManagerConsole";
        private bool retry = true;
        private string state = ExecutionStates.NOT_STARTED;
        private readonly object stateMutex = new object();

        #endregion

        private class ExecutionStates
        {
            #region Fields

            public static readonly string NOT_STARTED = "NOT_STARTED";
            public static readonly string VS_OPENED = "VS_OPENED";
            public static readonly string PROJECT_OPENED = "PROJECT_OPENED";
            public static readonly string NUGET_OPENED = "NUGET_OPENED";

            #endregion
        }
    }
}
