using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic;
using ISukces.SolutionDoctor.Logic.Problems;
using JetBrains.Annotations;

static internal class Manager
{
    #region Static Methods

    // Private Methods 

    private static void FixProblems([NotNull] CommandLineOptions options, [NotNull] List<ProblemFix> fixes)
    {
        if (options == null) throw new ArgumentNullException("options");
        if (fixes == null) throw new ArgumentNullException("fixes");
        Console.WriteLine("{0} fix(es) found.", fixes.Count);
        if (!options.Fix) return;
        foreach (var problemFix in fixes)
        {
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
    }

    private static List<ProblemFix> ShowProblems(CommandLineOptions options, IEnumerable<Problem> problems1)
    {
        var problems = problems1.ToArray();
        var problemsByProject = problems.GroupBy(a => a.ProjectFilename);
        var i = 0;
        var fixes = new List<ProblemFix>();
        foreach (var projectProblems in problemsByProject.OrderBy(a => a.Key))
        {
            var tmp = projectProblems.ToList();
            var hasBigProblems = tmp.Any(x => x.IsBigProblem);
            if (options.ShowOnlyBigProblems && !hasBigProblems)
                continue;
            Console.ForegroundColor = hasBigProblems ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.WriteLine("{0}. {1}", ++i, projectProblems.Key);
            Console.ResetColor();

            foreach (var problem in tmp)
            {
                problem.Describe(text => Console.WriteLine("    " + text));
                var fix = problem.GetFix();
                if (fix != null)
                    fixes.Add(fix);
            }
            Console.WriteLine();
        }
        var packagesToUpdate = problems.OfType<IConsiderUpdatePackage>().Select(a => a.GetPackageId()).Distinct().ToArray();
        if (packagesToUpdate.Any())
        {
            Console.WriteLine("Consider update package(s)");
            foreach (var ii in packagesToUpdate)
                Console.WriteLine("    " + ii);
        }
        return fixes;
    }

    #endregion Static Methods

    public static async Task Process(IEnumerable<string> dirs, CommandLineOptions options)
    {
        var doctor = new Doctor();
        //doctor.ScanSolutions(new DirectoryInfo(dir), options.ExcludeSolutions);
        var tmp = dirs.Select(dir => new DirectoryInfo(dir)).ToArray();
        await Task.Run(
            () => doctor.ScanSolutions(tmp, options.ExcludeSolutions, options));
        doctor.ExcludeDll = options.ExcludeDll;
        doctor.RemoveBindingRedirect = options.RemoveBindingRedirect;
        var problems = doctor.CheckAll(options).ToList();
        if (problems.Count == 0)
        {
            Console.WriteLine("No problems found. Congratulations.");
            return;
        }
        var fixes = ShowProblems(options, problems);
        FixProblems(options, fixes);
    }
}