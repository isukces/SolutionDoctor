using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    class ReferencesWithoutNugetsChecker
    {
        #region Static Methods

        // Public Methods 

        public static IList<Problem> Check(IEnumerable<Project> projects, IEnumerable<Nuspec> localNugetRepositiories)
        {
            // var aa = localNugetRepositiories.GetUnique(a => a.Location.FullName.ToLower(), a => a);

            var checker = new ReferencesWithoutNugetsChecker
            {
                _projects = projects.ToList(), // .Select(a => a.Projects.First()).ToList()
                _nuspecs = (from nuspec in localNugetRepositiories
                            let dir =
                                nuspec.Location.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar
                            select Tuple.Create(dir.ToLower(), nuspec)).ToArray(),
            };
            
            checker._nuspecs = checker._nuspecs.GetUnique(a => a.Item1, a => a)
#if DEBUG
                .OrderBy(a=>a.Item1)
#endif
                .ToArray(); 
            
            return checker.Check();

        }

        #endregion Static Methods

        #region Methods

        // Private Methods 

        private IList<Problem> Check()
        {
            return _projects.SelectMany(CheckProj).ToList();
        }

        private IEnumerable<Problem> CheckProj(Project project)
        {
            var projectReferences = project.References.Where(a => a.HintPath != null).ToArray();
            if (!projectReferences.Any()) 
                yield break;
            foreach (var dep in projectReferences)
            {
                var nuspec = FindNuspecByFile(dep.HintPath);
                if (nuspec == null) continue;
                var nugetPackage = project.NugetPackages
                    .FirstOrDefault(a => a.Id == nuspec.Id && a.Version == nuspec.PackageVersion);
                if (nugetPackage != null) continue;
                yield return new NuGetPackageShouldBeReferencedProblem
                {
                    ProjectFilename = project.Location,
                    PackageToReference = nuspec,
                    ReferencedLibrary = dep
                };
            }
        }

        private Nuspec FindNuspecByFile([NotNull] FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            var hintToLower = file.FullName.ToLower();
            var query = from nuspec in _nuspecs
                        where hintToLower.StartsWith(nuspec.Item1)
                        select nuspec.Item2;
            return query.FirstOrDefault();
        }

        #endregion Methods

        #region Fields

        List<Project> _projects;
        private Tuple<string, Nuspec>[] _nuspecs;

        #endregion Fields
    }
}