using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Checkers;
using ISukces.SolutionDoctor.Logic.Checkers.Xaml;
using ISukces.SolutionDoctor.Logic.Problems;
using Thread = System.Threading.Thread;

namespace ISukces.SolutionDoctor.Logic
{
    public class Doctor
    {
        static Doctor()
        {
#if PLATFORM_UNIX
            c = StringComparer.Ordinal;
#else
            c = StringComparer.OrdinalIgnoreCase;
#endif
        }


        public Doctor()
        {
            Solutions = new List<Solution>();
            LocalNugetRepositiories =
                new Dictionary<string, Dictionary<string, Nuspec>>(StringComparer.OrdinalIgnoreCase);
        }

        // Private Methods 

        private static bool Exlude(FileInfo fileInfo, IReadOnlyList<string> excludeItems,
            IReadOnlyList<string> excludeDirs)
        {
            var n = fileInfo.FullName.ToLower();
            if (excludeDirs != null)
                foreach (var i in excludeDirs)
                    if (n.StartsWith(i))
                        return true;
            return excludeItems.Any(i => n.EndsWith(i));
        }

        // Public Methods 

        public IEnumerable<Problem> CheckAll(CommandLineOptions options)
        {
            var groupedProjects = GetGroupedProjects();
            var uniqueProjects  = groupedProjects.Select(a => a.Projects.First().Project).ToList();
            var p1              = Task.Run(() => SolutionsInManyFoldersChecker.Check(groupedProjects));
            var p2              = Task.Run(() => NugetPackageAssemblyBindingChecker.Check(uniqueProjects, RemoveBindingRedirect,ForceBindingRedirects));
            var p3              = Task.Run(() => NugetPackageVersionChcecker.Check(uniqueProjects));
            var p4 = Task.Run(() => ReferencesWithoutNugetsChecker.Check(
                uniqueProjects,
                LocalNugetRepositiories.Values.SelectMany(a => a.Values),
                ExcludeDll));
            var p5  = Task.Run(() => NugetRepositoryDependencies.Check(LocalNugetRepositiories, uniqueProjects));
            var p6  = Task.Run(() => XamlInCsProjChecker.Check(uniqueProjects, options));
            var p7  = Task.Run(() => WarningsChecker.Check(uniqueProjects, options));
            var p8  = Task.Run(() => LangVersionChecker.Check(uniqueProjects, options));
            var all = new[] {p1, p2, p3, p4, p5, p6, p7, p8};
            Task.WaitAll(all);
            return all.SelectMany(a => a.Result).ToArray();
        }

        public void ScanSolutions(IReadOnlyList<DirectoryInfo> dirs, IReadOnlyList<string> excludeItems,
            IDoctorConfig cfg)
        {
            if (dirs == null)
                return;
            dirs = dirs.Where(a => a.Exists).ToArray();
            if (!dirs.Any())
                return;
            var excludeDirs = cfg.ExcludeDirectories?
                                  .Select(a => new DirectoryInfo(a).FullName.ToLower() + "\\")
                                  .ToList() ?? new List<string>();
            var filesStream = DiscFileScanner.MakeObservable(dirs,
                "*.sln",
                i => !Exlude(i, excludeItems, excludeDirs),
                excludeDirs);
            // .Publish();

            var tmp = filesStream.ToEnumerable().ToArray();

            var solutions =
                filesStream
                    .ObserveOn(NewThreadScheduler.Default)
                    .Select(
                        i =>
                        {
                            Solution s = null;
                            try
                            {
                                s = new Solution(i);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine(
                                    Thread.CurrentThread.ManagedThreadId + " solution {0} can't be parsed", i.FullName);
                            }

                            return s;
                        }
                    )
                    .Where(i => i != null)
                    .Publish();

            var scanPackagesEventSlim       = new ManualResetEventSlim(false);
            var addSolutionsToListEventSlim = new ManualResetEventSlim(false);

            solutions
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(
                    solution => Solutions.Add(solution),
                    () => addSolutionsToListEventSlim.Set());

            solutions
                .Select(a => a.SolutionFile.Directory.FullName)
                .Distinct(c)
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(
                    dir => ScanLocalNugets(new DirectoryInfo(dir)),
                    () => scanPackagesEventSlim.Set());

            solutions.Connect();
            scanPackagesEventSlim.Wait();
            addSolutionsToListEventSlim.Wait();
        }


        // Private Methods 

        private List<ProjectGroup> GetGroupedProjects()
        {
            var liqQuery = from solution in Solutions
                from project in solution.Projects
                group new ProjectPlusSolution
                    {
                        Project  = project,
                        Solution = solution
                    }
                    by project.Location
                into projectGroup
                select new ProjectGroup
                {
                    Filename = projectGroup.Key,
                    Projects = projectGroup.ToArray()
                };
            var groupedProjects = liqQuery.ToList();
            return groupedProjects;
        }

        private void ScanLocalNugets(DirectoryInfo directory)
        {
            lock(LocalNugetRepositiories)
            {
                directory = new DirectoryInfo(Path.Combine(directory.FullName, "packages"));
                if (LocalNugetRepositiories.ContainsKey(directory.FullName))
                    return;
                var repositories = Nuspec.GetRepositories(directory);
                LocalNugetRepositiories[directory.FullName] = repositories.ToDictionary(
                    nuspec => nuspec.FullId, nuspec => nuspec);
            }
        }

        public IList<Solution> Solutions { get; }

        public Dictionary<string, Dictionary<string, Nuspec>> LocalNugetRepositiories { get; }
        public HashSet<string>                                ExcludeDll              { get; set; }
        public HashSet<string>                                RemoveBindingRedirect   { get; set; }
        public Dictionary<string, string>                     ForceBindingRedirects   { get; set; }

        private static readonly StringComparer c;
    }

    public class ProjectGroup
    {
        public override string ToString()
        {
            return string.Format("project {0} in {1} solution(s)", Filename.Name, Projects.Length);
        }

        #region Properties

        public FileName Filename { get; set; }

        public ProjectPlusSolution[] Projects { get; set; }

        #endregion Properties
    }

    public class ProjectPlusSolution
    {
        #region Properties

        public SolutionProject Project { get; set; }

        public Solution Solution { get; set; }

        #endregion Properties
    }
}