using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ISukces.SolutionDoctor.Logic;

namespace ISukces.SolutionDoctor
{
    internal class Program
    {
        #region Static Methods

         

        private static void Main(string[] args)
        {
#if DEBUG
         //   args = new[] { "ct" };
            // args = DebugConfigs.SetArgs(args);

#endif
            try
            {
                var options = CommandLineOptions.Parse(args);
                if (options == null)
                {
                    ShowHelp();
                    return;
                }

                if (options.ScanDirectories.Any())
                    Directory.SetCurrentDirectory(options.ScanDirectories.First());
                if (!string.IsNullOrEmpty(options.SaveConfigFileName))
                {
                    options.Save(new FileInfo(options.SaveConfigFileName));
                    Console.WriteLine("Config saved into file {0}.", options.SaveConfigFileName);
                }

                if (options.ScanDirectories.Count < 1)
                {
                    ShowHelp();
                    return;
                }

                foreach (var i in options.ScanDirectories)
                {
                    var directory = new DirectoryInfo(i);
                    if (!directory.Exists)
                    {
                        Console.WriteLine("Directory {0} doesn't exist", directory.FullName);
                        ShowHelp();
                        return;
                    }
                }

                Manager.ProcessX(options.ScanDirectories, options);
            }
            catch (Exception e)
            {
                DisplayException(e);
                ShowHelp();
            }
            LogicDisposer.Flush();
            Console.ReadLine();
        }

        private static void DisplayException(Exception e)
        {
            switch (e)
            {
                case null:
                    break;
                case AggregateException ae:
                    foreach (var i in ae.InnerExceptions)
                        DisplayException(i);
                    break;
                default:
                    Console.WriteLine("       " + e.StackTrace);
                    Console.Write("Error: ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                    DisplayException(e.InnerException);
                    break;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("use:");
            Console.WriteLine("    SolutionDoctor {options} {folder name}");
            Console.WriteLine("    options:");
            Console.WriteLine("    -fix                     Try to fix errors if possible");
            Console.WriteLine("    -onlyBig                 Show only big problems");
            Console.WriteLine("    -runExternalFix          Run fixes by calling external programm");
            Console.WriteLine("    -exclude {solution name} Exclude solution. This option can be used multiple times.");
            Console.WriteLine("    -saveOptions {file name} Save parsed command line options into file");
            Console.WriteLine("    -cfg {file name}         Load options from file");
        }

        #endregion Static Methods
    }
}