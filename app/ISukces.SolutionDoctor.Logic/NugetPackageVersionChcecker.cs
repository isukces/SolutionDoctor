using System;
using System.Collections.Generic;
using System.Linq;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic
{
    public class NugetPackageVersionChcecker
    {
        #region Static Methods

        // Private Methods 

        public static IEnumerable<Problem> Check(IList<ProjectGroup> groupedProjects)
        {
            var a = new NugetPackageVersionChcecker()
            {
                _groupedProjects = groupedProjects
            };
            return a.Check();
        }

        #endregion Static Methods

        #region Methods

        // Private Methods 

        private void Add(NugetPackage nugetPackage, string projectFullFilename)
        {
            PackageUsages ee;
            if (!_packages.TryGetValue(nugetPackage.Id, out ee))
                _packages[nugetPackage.Id] = ee = new PackageUsages(nugetPackage.Id);
            ee.Add(projectFullFilename, nugetPackage);
        }

        private IEnumerable<Problem> Check()
        {
            foreach (var i in _groupedProjects)
            {
                var project = i.Projects.First().Project;
                foreach (var p in project.NugetPackages)
                {
                    Add(p, project.File.FullName);
                }
            }
            foreach (var usages in _packages.Values)
            {
                var tmp = usages.Versions
                    .GroupBy(a => a.Value)
                    .Select(a => new { Ver = a.Key, Projects = a.ToArray() })
                    .OrderBy(a => a.Ver)
                    .ToArray();
                if (tmp.Length < 2) continue;
                var t = tmp.Last().Ver;
                var newest = t.Version;
                var wrong = tmp.Where(a => a.Ver.Version != newest).ToArray();
                foreach (var i in wrong)
                {
                    foreach (var j in i.Projects)
                        yield return new OldNugetVersionProblem
                        {
                            ProjectFilename = j.Key,
                            ReferencedVersion = j.Value ,
                            NewestVersion = t   ,
                            PackageId = usages.PackageId                            
                        };
                }

            }
           
        }

        #endregion Methods

        #region Fields

        IList<ProjectGroup> _groupedProjects;
        readonly Dictionary<string, PackageUsages> _packages = new Dictionary<string, PackageUsages>(StringComparer.OrdinalIgnoreCase);

        #endregion Fields

        #region Nested Classes


        class PackageUsages
        {
            #region Constructors


            public PackageUsages(string packageId)
            {
                Versions = new Dictionary<string, NugetVersion>(StringComparer.OrdinalIgnoreCase);
                PackageId = packageId;
            }

            #endregion Constructors

            #region Methods

            // Public Methods 

            public void Add(string projectFullFilename, NugetPackage nugetPackage)
            {
                Versions[projectFullFilename] = nugetPackage.Version;
            }

            #endregion Methods

            #region Properties

            public string PackageId { get; set; }

            public Dictionary<string, NugetVersion> Versions { get; private set; }

            #endregion Properties
        }
        #endregion Nested Classes
    }
}