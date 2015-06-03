using System.Collections.Generic;
using System.Linq;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    static class SolutionsInManyFoldersChecker
    {
        public static IList<Problem> Check(IList<ProjectGroup> groupedProjects)
        {
            return groupedProjects.SelectMany(CheckProject).ToList();
        }

        private static IEnumerable<Problem> CheckProject(ProjectGroup projectGroup)
        {
            var tmp = projectGroup.Projects
#if PLATFORM_UNIX
                .Select(a => a.Solution.SolutionFile.Directory.FullName)
#else
                .Select(a => a.Solution.SolutionFile.Directory.FullName.ToLowerInvariant())
#endif
                .ToArray();
            var unique = tmp.Distinct().ToArray();
            if (unique.Length < 2) yield break;
            yield return new SolutionsInManyFoldersProblem
            {
                ProjectFilename = projectGroup.Filename,
                Solutions = projectGroup.Projects.Select(a => a.Solution.SolutionFile).ToArray(),
                ProjectHasNugetPackages = projectGroup.Projects.First().Project.NugetPackages.Any()
            };
        }
    }
}