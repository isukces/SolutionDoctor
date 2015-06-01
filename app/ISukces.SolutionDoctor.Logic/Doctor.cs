using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic
{
    public class Doctor
    {
        #region Constructors

        public Doctor()
        {
            Solutions = new List<Solution>();
            LocalNugetRepositiories = new Dictionary<string, Dictionary<string, Nuspec>>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion Constructors

        #region Static Methods

        // Private Methods 

        private static IEnumerable<Problem> Check1(IList<ProjectGroup> groupedProjects)
        {
            foreach (var projectGroup in groupedProjects)
            {
                var tmp = projectGroup.Projects.Select(a => a.Solution.SolutionFile.Directory.FullName).ToArray();
                var unique = tmp.Select(a => a.ToLowerInvariant()).Distinct().ToArray();
                if (unique.Length < 2) continue;
                yield return new SolutionsInManyFoldersProblem
                {
                    ProjectFilename = projectGroup.Filename,
                    Folders = projectGroup.Projects.Select(a => a.Solution.SolutionFile.FullName).ToArray(),
                    ProjectHasNugetPackages = projectGroup.Projects.First().Project.NugetPackages.Any()
                };
            }
        }

        private static IEnumerable<Problem> CheckNugetPackageAssemblyBinding(IList<ProjectGroup> groupedProjects)
        {
            foreach (var i in groupedProjects)
            {
                var sampleProject = i.Projects.First().Project;
                var assemblyBindings = sampleProject.AssemblyBindings;
                var packageVersion = sampleProject.NugetPackages;
                if (!assemblyBindings.Any() || !packageVersion.Any()) continue;
                foreach (var package in packageVersion)
                {
                    var redirect =
                        assemblyBindings.FirstOrDefault(
                            a => String.Equals(a.Name, package.Id, StringComparison.OrdinalIgnoreCase));
                    if (redirect == null) continue;
                    if (redirect.NewVersion != package.Version)
                    {
                        yield return new WrongBindingRedirectProblem
                        {
                            ProjectFilename = sampleProject.File.FullName,
                            Redirect = redirect,
                            Package = package
                        };
                    }
                }
            }
        }

        private static bool Exlude(FileInfo fileInfo, string[] excludeItems)
        {
            var n = fileInfo.FullName.ToLower();
            return excludeItems.Any(i => n.EndsWith(i));
        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public IEnumerable<Problem> CheckAll()
        {
            var groupedProjects = GetGroupedProjects();
            var p1 = Check1(groupedProjects);
            var p2 = CheckNugetPackageAssemblyBinding(groupedProjects);
            var p3 = NugetPackageVersionChcecker.Check(groupedProjects);
            return p1.Concat(p2).Concat(p3);
        }
        // Private Methods 

        private List<ProjectGroup> GetGroupedProjects()
        {
            var liqQuery = from solution in Solutions
                           from project in solution.Projects
                           group new ProjectPlusSolution
                           {
                               Project = project,
                               Solution = solution
                           }
                               by project.File.FullName.ToLowerInvariant()
                               into projectGroup
                               select new ProjectGroup
                               {
                                   Filename = projectGroup.Key,
                                   Projects = projectGroup.ToArray()
                               };
            var groupedProjects = liqQuery.ToList();
            return groupedProjects;
        }

        #endregion Methods

        #region Properties

        public List<Solution> Solutions { get; set; }

        public Dictionary<string, Dictionary<string, Nuspec>> LocalNugetRepositiories { get; private set; }

        #endregion Properties

        public async Task ScanSolutionsAsync(DirectoryInfo di, string[] excludeItems)
        {
            if (di.Exists)
            {
                foreach (var i in di.GetFiles("*.sln"))
                {
                    if (!Exlude(i, excludeItems))
                    {
                        try
                        {
                            var sol = new Solution(i);
                            Solutions.Add(sol);
                            ScanLocalNugets(i.Directory);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("solution {0} can't be parsed", i.FullName);
                        }

                    }
                }
                foreach (var i in di.GetDirectories())
                    await ScanSolutionsAsync(i, excludeItems);
            }
        }

        private void ScanLocalNugets(DirectoryInfo directory)
        {
            directory = new DirectoryInfo(Path.Combine(directory.FullName, "packages"));
            if (LocalNugetRepositiories.ContainsKey(directory.FullName))
                return;
            var repositories = Nuspec.GetRepositories(directory);
            LocalNugetRepositiories[directory.FullName] = repositories.ToDictionary(
                nuspec => nuspec.FullId, nuspec => nuspec);
        }
    }

    public class ProjectGroup
    {
        #region Properties

        public string Filename { get; set; }

        public ProjectPlusSolution[] Projects { get; set; }

        #endregion Properties
    }

    public class ProjectPlusSolution
    {
        #region Properties

        public Project Project { get; set; }

        public Solution Solution { get; set; }

        #endregion Properties
    }
}
