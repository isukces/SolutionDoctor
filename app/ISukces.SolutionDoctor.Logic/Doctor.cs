using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        }

        #endregion Constructors

        #region Methods

        // Public Methods 

        public IEnumerable<Problem> CheckAll()
        {
            var groupedProjects = GetGroupedProjects();
            var p1 = Check1(groupedProjects);
            var p2 = CheckNugetPackageAssemblyBinding(groupedProjects);
            return p1.Concat(p2);
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
                        yield return new WrongBindingRedirectProblem(
                            )
                        {
                            ProjectFilename = sampleProject.File.FullName,
                            Redirect = redirect,
                            Package = package
                        };
                    }
                }
            }
        }

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
                    Folders = unique,
                    ProjectHasNugetPackages = projectGroup.Projects.First().Project.NugetPackages.Any()
                };
            }
        }

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

        #endregion Properties

        public async Task ScanSolutionsAsync(DirectoryInfo di)
        {
            if (di.Exists)
            {
                foreach (var i in di.GetFiles("*.sln"))
                    Solutions.Add(new Solution(i));
                foreach (var i in di.GetDirectories())
                    await ScanSolutionsAsync(i);
            }
        }
    }

    internal class ProjectGroup
    {
        #region Properties

        public string Filename { get; set; }

        public ProjectPlusSolution[] Projects { get; set; }

        #endregion Properties
    }

    internal class ProjectPlusSolution
    {
        #region Properties

        public Project Project { get; set; }

        public Solution Solution { get; set; }

        #endregion Properties
    }
}
