using System;
using System.Collections.Generic;
using System.Linq;
using isukces.code.vssolutions;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    public class NugetPackageVersionChcecker
    {
        public static IEnumerable<Problem> Check(IList<SolutionProject> projects)
        {
            var a = new NugetPackageVersionChcecker
            {
                _projects = projects
            };
            return a.Check().ToList();
        }


        private void Add(FileName projectFile, NugetPackage nugetPackage)
        {
            PackageUsages packageUsages;
            if (!_packages.TryGetValue(nugetPackage.Id, out packageUsages))
                _packages[nugetPackage.Id] = packageUsages = new PackageUsages(nugetPackage.Id);
            packageUsages.Add(projectFile, nugetPackage);
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
                    .Select(a => new {PackageVersion = a.Key, Projects = a.ToArray()})
                    .OrderBy(a => a.PackageVersion)
                    .ToArray();
                if (tmp.Length < 2) continue;
                var acceptedVersion = tmp.Last().PackageVersion;
                var newest          = acceptedVersion.Version;
                var wrong           = tmp.Where(a => a.PackageVersion.Version != newest).ToArray();
                foreach (var i in wrong)
                foreach (var j in i.Projects)
                    yield return new OldNugetVersionProblem
                    {
                        ProjectFilename   = j.Key,
                        ReferencedVersion = j.Value,
                        NewestVersion     = acceptedVersion,
                        PackageId         = usages.PackageId
                    };
            }
        }

        private readonly Dictionary<string, PackageUsages> _packages =
            new Dictionary<string, PackageUsages>(StringComparer.OrdinalIgnoreCase);

        private IList<SolutionProject> _projects;


        private class PackageUsages
        {
            public PackageUsages(string packageId)
            {
                Versions  = new Dictionary<FileName, NugetVersion>();
                PackageId = packageId;
            }

            public void Add(FileName projectFullFilename, NugetPackage nugetPackage)
            {
                Versions[projectFullFilename] = nugetPackage.Version;
            }

            public string PackageId { get; }

            public Dictionary<FileName, NugetVersion> Versions { get; }
        }
    }
}