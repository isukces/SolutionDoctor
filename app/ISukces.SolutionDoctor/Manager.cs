using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic;
using ISukces.SolutionDoctor.Logic.Problems;
using JetBrains.Annotations;

internal static class Manager
{
    public static void  ProcessX(IEnumerable<string> dirs, CommandLineOptions options)
    {
        var doctor = new Doctor();
        //doctor.ScanSolutions(new DirectoryInfo(dir), options.ExcludeSolutions);
        var tmp = dirs.Select(dir => new DirectoryInfo(dir)).ToArray();
        doctor.ScanSolutions(tmp, options.ExcludeSolutions, options);
        doctor.ExcludeDll            = options.ExcludeDll;
        doctor.RemoveBindingRedirect = options.RemoveBindingRedirect;
        doctor.ForceBindingRedirects = options.ForceBindingRedirects;
        var problems = doctor.CheckAll(options).ToList();
        if (problems.Count == 0)
        {
            Console.WriteLine("No problems found. Congratulations.");
            return;
        }

        var fixes = ShowProblems(options, problems);
        FixProblems(options, fixes);
    }

    #region Static Methods

     

    private static void FixProblems([NotNull] CommandLineOptions options, [NotNull] List<ProblemFix> fixes)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (fixes == null) throw new ArgumentNullException(nameof(fixes));
        Console.WriteLine("{0} fix(es) found.", fixes.Count);
        if (!options.Fix) return;
        foreach (var problemFix in fixes)
            try
            {
                Console.Write(problemFix.Description);
                problemFix.Fix();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" OK");
                Console.ResetColor();
            }
            catch (Exception ee)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ee.Message);
                Console.ResetColor();
            }
    }

    private static List<ProblemFix> ShowProblems(CommandLineOptions options, IEnumerable<Problem> problems1)
    {
        var problems          = problems1.ToArray();
        var problemsByProject = problems.GroupBy(a => a.ProjectFilename);
        var i                 = 0;
        var fixes             = new List<ProblemFix>();
        foreach (var projectProblems in problemsByProject.OrderBy(a => a.Key))
        {
            var tmp            = projectProblems.ToList();
            var hasBigProblems = tmp.Any(x => x.IsBigProblem);
            if (options.ShowOnlyBigProblems && !hasBigProblems)
                continue;
            Console.ForegroundColor = hasBigProblems ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.WriteLine("{0}. {1}", ++i, projectProblems.Key);
            Console.ResetColor();

            foreach (var problem in tmp)
            {
                problem.Describe(text => WriteRichText(text, true));
                var s = problem.GetFixScript();
                if (s != null)
                {
                    WriteRichText(new RichString(ConsoleColor.Green, "Run"), false);
                    string fullName = s.WorkingDirectory.FullName;
                    var batch = new StringBuilder();
                    if (s.WorkingDirectory != null)
                    {
                        var drive = fullName.GetDrive();
                        if (!string.IsNullOrEmpty(drive))
                        {
                            WriteRichText(new RichString(ConsoleColor.Green, drive + ":"), true);
                            batch.AppendLine(drive + ":");
                        }

                        WriteRichText( new RichString(ConsoleColor.Green, "cd " + fullName.QuoteFilename()),true);
                        batch.AppendLine("cd "+fullName.QuoteFilename());
                    }

                    foreach (var ii in s.Commands)
                    {
                        WriteRichText(ii, true);
                        batch.AppendLine(ii.GetPureText());
                    }

                    if (options.RunExternalFix)
                    {
                        var f = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bat");
                        var fi = new FileInfo(f);
                        File.WriteAllText(f, batch.ToString());
                        ProcessStartInfo startInfo = new ProcessStartInfo(fi.FullName)
                        {
                            WindowStyle            = ProcessWindowStyle.Minimized,
                            WorkingDirectory       = fi.Directory.FullName,
                            CreateNoWindow         = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                            UseShellExecute        = false,
                        };
                        try
                        {
                            var ii = Process.Start(startInfo);
                            if (ii != null)
                            {
                                ii.OutputDataReceived += (a, b)=>
                                {
                                    Console.WriteLine(b.Data);
                                };
                                ii.ErrorDataReceived    += (a, b)=>
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(b.Data);
                                    Console.ResetColor();
                                };
                                ii.WaitForExit();
                            }
 
                        }
                        finally
                        {
                            File.Delete(f);
                        }
                        
                       
                             
                        
                    }
                }

                var fix = problem.GetFix();
                if (fix != null)
                    fixes.Add(fix);
            }

            Console.WriteLine();
        }

        var packagesToUpdate = problems.OfType<IConsiderUpdatePackage>().Select(a => a.GetPackageId()).Distinct()
            .ToArray();
        if (packagesToUpdate.Any())
        {
            Console.WriteLine("Consider update package(s)");
            foreach (var ii in packagesToUpdate)
                Console.WriteLine("    " + ii);
        }

        return fixes;
    }

    private static void WriteRichText(RichString text, bool intended)
    {
        if (intended)
            Console.Write("    ");
        foreach (var x in text.Items)
        {
            if (x.ResetColors)
                Console.ResetColor();
            if (x.TextColor.HasValue)
                Console.ForegroundColor = x.TextColor.Value;
            if (x.BackgroundColor.HasValue)
                Console.BackgroundColor = x.BackgroundColor.Value;

            Console.Write(x.Text);
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    #endregion Static Methods
}