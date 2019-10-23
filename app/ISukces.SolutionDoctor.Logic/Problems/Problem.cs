using System;
using System.Collections.Generic;
using System.IO;
using ISukces.SolutionDoctor.Logic.NuGet;
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

        public bool IsBigProblem
        {
            get { return GetIsBigProblem(); }
        }

        public FileName ProjectFilename { get; set; }
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
            this.Commands.AddRange(commands);
        }

        public static FixScript FullFxNugetInstall(FileName project, INuspec nuspec, string verb)
        {
            var c = new MessageColorer().WithProjectAt(1).WithPackageAt(2);
            var commandToRun = RichString.RichFormat(c.Color, 
                "nuget {0} {1} -id {2}", 
                verb, project.FullName, nuspec.Id);
            return new FixScript
            {
                Commands =
                {
                    commandToRun
                }
            };
            
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

        public DirectoryInfo           WorkingDirectory { get; set; }
        public List<RichString> Commands         { get; } = new List<RichString>();
    }

    public enum ScriptKind
    {
        Console,
        Powershell
    }
}