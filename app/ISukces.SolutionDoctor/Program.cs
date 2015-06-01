using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor
{
    class Program
    {
        #region Static Methods

        // Private Methods 

        static void Main(string[] args)
        {
            try
            {
                var options = CommandLineOptions.Parse(args);
                if (options == null || options.Directories.Count < 1)
                {
                    ShowHelp();
                    return;
                }
                var directory = new DirectoryInfo(options.Directories.First());
                if (!directory.Exists)
                {
                    Console.WriteLine("Directory {0} doesn't exist", directory.FullName);
                    ShowHelp();
                    return;
                }
                var task = Manager.Process(directory.FullName, options);
                task.Wait();

            }
            catch (AggregateException e)
            {
                foreach (var i in e.InnerExceptions)
                {
                    Console.WriteLine("Error: " + i.Message);
                }
                ShowHelp();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                ShowHelp();
            }
            Console.ReadLine();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("use:");
            Console.WriteLine("    SolutionDoctor {options} {folder name}");
            Console.WriteLine("    options:");
            Console.WriteLine("    -fix      Try to fix errors if possible");
            Console.WriteLine("    -onlyBig  Show only big problems");
            
        }

        #endregion Static Methods
    }
}
