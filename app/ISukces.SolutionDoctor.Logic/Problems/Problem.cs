﻿using System;
using System.Collections.Generic;
using System.IO;
using iSukces.Code.VsSolutions;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public abstract class Problem
    {
        public abstract void Describe(Action<RichString> writeLine);

        public abstract ProblemFix GetFix();

        [CanBeNull]
        public abstract FixScript GetFixScript();

        protected abstract bool GetIsBigProblem();

        #region properties

        public bool IsBigProblem => GetIsBigProblem();

        public FileName ProjectFilename { get; set; }

        #endregion
    }

    public class FixScript
    {
        public FixScript(params RichString[] commands)
        {
            Commands.AddRange(commands);
        }

        public FixScript(DirectoryInfo workingDirectory, params RichString[] commands)
        {
            WorkingDirectory = workingDirectory;
            Commands.AddRange(commands);
        }

        public static FixScript CoreNugetInstall(FileName project, INuspec nuspec, string verb)
        {
            var c = new MessageColorer().WithProjectAt(1).WithPackageAt(2).WithVersionAt(3);
            var commandToRun = RichString.RichFormat(c.Color,
                "dotnet {0} {1} package -v {2} {3}",
                verb, project.Name,
                nuspec.PackageVersion, nuspec.Id);
            return new FixScript(project.Directory, commandToRun);
        }

        public static FixScript FullFxNugetInstall(FileName project, INuspec nuspec, string verb)
        {
            var c = new MessageColorer().WithProjectAt(1).WithPackageAt(2);
            var commandToRun = RichString.RichFormat(c.Color,
                "nuget {0} {1} -id {2}",
                verb, project.Name, nuspec.Id);
            return new FixScript(project.Directory, commandToRun);
        }

        #region properties

        public DirectoryInfo    WorkingDirectory { get; set; }
        public List<RichString> Commands         { get; } = new List<RichString>();

        #endregion
    }

    public enum ScriptKind
    {
        Console,
        Powershell
    }
}
