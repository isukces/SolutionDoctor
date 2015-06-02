using System;
using System.Collections.Generic;
using System.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    public class NugetPackageVersionChcecker
    {
        #region Static Methods

        // Private Methods 

        public static IEnumerable<Problem> Check(IList<Project> projects)
        {
            var a = new NugetPackageVersionChcecker()
            {
                _projects = projects
            };
            return a.Check();
        }

        #endregion Static Methods

        #region Methods

        // Private Methods 

        private void Add(FileName projectFile, NugetPackage nugetPackage)
        {
            PackageUsages ee;
            if (!_packages.TryGetValue(nugetPackage.Id, out ee))
                _packages[nugetPackage.Id] = ee = new PackageUsages(nugetPackage.Id);
            ee.Add(projectFile, nugetPackage);
        }

        private IEnumerable<Problem> Check()
        {
            foreach (var project in _projects)
                foreach (var p in project.NugetPackages)
                    Add(project.Location, p);
            
            foreach (var usages in _packages.Values)
            {
                var tmp = usages.Versions
                    .GroupBy(a => a.Value)
                    .Select(a => new { PackageVersion = a.Key, Projects = a.ToArray() })
                    .OrderBy(a => a.PackageVersion)
                    .ToArray();
                if (tmp.Length < 2) continue;
                var acceptedVersion = tmp.Last().PackageVersion;
                var newest = acceptedVersion.Version;
                var wrong = tmp.Where(a => a.PackageVersion.Version != newest).ToArray();
                foreach (var i in wrong)
                {
                    foreach (var j in i.Projects)
                        yield return new OldNugetVersionProblem
                        {
                            ProjectFilename = j.Key,
                            ReferencedVersion = j.Value ,
                            NewestVersion = acceptedVersion   ,
                            PackageId = usages.PackageId                            
                        };
                }

            }
           
        }

        #endregion Methods

        #region Fields

        IList<Project> _projects;
        readonly Dictionary<string, PackageUsages> _packages = new Dictionary<string, PackageUsages>(StringComparer.OrdinalIgnoreCase);

        #endregion Fields

        #region Nested Classes


        class PackageUsages
        {
            #region Constructors


            public PackageUsages(string packageId)
            {
                Versions = new Dictionary<FileName, NugetVersion>( );
                PackageId = packageId;
            }

            #endregion Constructors

            #region Methods

            // Public Methods 

            public void Add(FileName projectFullFilename, NugetPackage nugetPackage)
            {
                Versions[projectFullFilename] = nugetPackage.Version;
            }

            #endregion Methods

            #region Properties

            public string PackageId { get; set; }

            public Dictionary<FileName, NugetVersion> Versions { get; private set; }

            #endregion Properties
        }
        #endregion Nested Classes
    }
}