using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public class Solution
    {
        #region Constructors

        public Solution(FileInfo solutionFile)
        {
            Projects = new List<Project>();
            SolutionFile = solutionFile;
            var lines = File.ReadAllLines(SolutionFile.FullName);

            var inProject = false;
            foreach (var i in lines)
            {
                var ii = i.Trim();
                if (inProject)
                {
                    if (ii == "EndProject")
                    {
                        inProject = false;
                    }
                    continue;
                }
                var a = TryParseProject(ii);
                if (a == null)
                    continue;
                Projects.Add(a);
                inProject = true;


            }
        }

        #endregion Constructors

        #region Methods

        // Private Methods 

        private Project TryParseProject(string line)
        {
            var match = ProjectRegex.Match(line);
            if (!match.Success)
                return null;
            var project = new Project
            {
                LocationUid = Guid.Parse(match.Groups[1].Value),
                Name = match.Groups[2].Value,
                // ReSharper disable once PossibleNullReferenceException
                File = new FileInfo(Path.Combine(SolutionFile.Directory.FullName, match.Groups[3].Value)),
                ProjectUid = Guid.Parse(match.Groups[4].Value)
            };
            return project.File.Exists
                ? project
                : null;
        }

        #endregion Methods

        #region Static Fields

        static readonly Regex ProjectRegex = new Regex(ProjectRegexFilter, RegexOptions.Compiled);

        #endregion Static Fields

        #region Fields

        public FileInfo SolutionFile { get; private set; }
        const string ProjectRegexFilter = @"^Project\(\s*\""*{([^}]+)\}\""\s*\)\s*=\s*\""([^\""]+)\""\s*,\s*\""([^\""]+)\""\s*,\s*\s*\""*{([^}]+)\}\""\s*(.*)$";

        #endregion Fields

        #region Properties

        public List<Project> Projects { get; private set; }

        #endregion Properties
    }
}