using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic.Checkers;
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
            var uniqueProjects = groupedProjects.Select(a => a.Projects.First().Project).ToList();
            var p1 = SolutionsInManyFoldersChecker.Check(groupedProjects);
            var p2 = NugetPackageAssemblyBindingChecker.Check(uniqueProjects);
            var p3 = NugetPackageVersionChcecker.Check(uniqueProjects);  
            var p4 = ReferencesWithoutNugetsChecker.Check(
                uniqueProjects, 
                LocalNugetRepositiories.Values.SelectMany(a=>a.Values));
            return p1.Concat(p2).Concat(p3).Concat(p4);
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

        public Project Project { get; set; }

        public Solution Solution { get; set; }

        #endregion Properties
    }
}
