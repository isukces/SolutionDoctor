using System.Collections.Generic;
using System.IO;
using System.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    internal class NugetRepositoryDependencies
    {
        public static IEnumerable<Problem> Check(Dictionary<string, Dictionary<string, Nuspec>> localNugetRepositiories,
            List<Project> uniqueProjects)
        {
            var instance = new NugetRepositoryDependencies
            {
                _localNugetRepositiories = localNugetRepositiories,
                _uniqueProjects          = uniqueProjects
            };
            return instance.CheckInternal().ToList();
        }

        private IEnumerable<Problem> CheckInternal()
        {
            var packagesRelations = (from pair in _localNugetRepositiories
                from specPair in pair.Value
                from nugetDependency in specPair.Value.Dependencies
                select new Data
                {
                    CheckedPackageLocation              = specPair.Value.Location,
                    CheckedPackageId                    = specPair.Value.FullId,
                    ReferencedPackageName               = nugetDependency.Id,
                    ReferencedPackageAcceptableVersions = nugetDependency.Versions
                }).ToArray();

            var nugetVersionsForProjects = _uniqueProjects
                .SelectMany(project => project.NugetPackages)
                .GroupBy(nugetPackage => nugetPackage.Id)
                .Select(grouping => new
                {
                    grouping.Key,
                    Items = grouping.OrderByDescending(b => b.Version).First()
                })
                .ToDictionary(a => a.Key, a => a.Items);

            foreach (var packageRelation in packagesRelations)
            {
                if (packageRelation.ReferencedPackageName == null)
                    continue;
                if (!nugetVersionsForProjects.TryGetValue(packageRelation.ReferencedPackageName,
                    out var packageReferencedByProject)) continue;
                var result =
                    packageRelation.ReferencedPackageAcceptableVersions.CheckVersion(packageReferencedByProject
                        .Version);
                if (result == VersionCheckResult.Ok) continue;
                yield return new NugetRepositoryDependencyProblem
                {
                    ProjectFilename =
                        new FileName(new FileInfo(packageRelation.CheckedPackageLocation.FullName)),
                    ReferencedPackageAcceptableVersions = packageRelation.ReferencedPackageAcceptableVersions,
                    CheckedPackageId                    = packageRelation.CheckedPackageId,
                    CheckedPackageLocation              = packageRelation.CheckedPackageLocation,
                    PackageReferencedByProject          = packageReferencedByProject
                };
            }
        }

        private Dictionary<string, Dictionary<string, Nuspec>> _localNugetRepositiories;
        private List<Project> _uniqueProjects;
    }

    internal class Data
    {
        public override string ToString()
        {
            return string.Format("{2} => {0} {1}", ReferencedPackageName, ReferencedPackageAcceptableVersions,
                CheckedPackageLocation);
        }

        public string            CheckedPackageId                    { get; set; }
        public string            ReferencedPackageName               { get; set; }
        public NugetVersionRange ReferencedPackageAcceptableVersions { get; set; }
        public DirectoryInfo     CheckedPackageLocation              { get; set; }
    }
}