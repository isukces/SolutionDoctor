using System;
using System.IO;
using System.Linq;
using ISukces.SolutionDoctor.Logic;

namespace ISukces.SolutionDoctor
{
    internal class Program
    {
        #region Static Methods

        // Private Methods 

        private static void Main(string[] args)
        {
#if DEBUGxxx
            args = new[] {"-cfg", "SolutionDoctor.json"};
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

                var task = Manager.Process(options.ScanDirectories, options);
                task.Wait();
            }
            catch (Exception e)
            {
                DisplayException(e);
                ShowHelp();
            }

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
                    Console.WriteLine("Error: " + e.Message);
                    Console.WriteLine("       " + e.StackTrace);
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
            Console.WriteLine("    -exclude {solution name} Exclude solution. This option can be used multiple times.");
            Console.WriteLine("    -saveOptions {file name} Save parsed command line options into file");
            Console.WriteLine("    -cfg {file name}         Load options from file");
        }

        #endregion Static Methods
    }
}