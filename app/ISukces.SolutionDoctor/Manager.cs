using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    private static List<ProblemFix> ShowProblems(CommandLineOptions options, IEnumerable<Problem> problems)
    {
        var problemsByProject = problems.GroupBy(a => a.ProjectFilename.ToLower());
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
        return fixes;
    }

    #endregion Static Methods

    public static async Task Process(string dir, CommandLineOptions options)
    {
        var doctor = new Doctor();
        await doctor.ScanSolutionsAsync(new DirectoryInfo(dir), options.Exclude);
        var problems = doctor.CheckAll().ToList();
        if (problems.Count == 0)
        {
            Console.WriteLine("No problems found. Congratulations.");
            return;
        }
        var fixes = ShowProblems(options, problems);
        FixProblems(options, fixes);
    }
}