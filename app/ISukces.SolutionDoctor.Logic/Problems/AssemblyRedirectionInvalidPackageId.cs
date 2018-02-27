﻿using System;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class AssemblyRedirectionInvalidPackageId : Problem
    {
        public AssemblyRedirectionInvalidPackageId(string packageId, Project project)
        {
            _packageId      = packageId;
            _project        = project;
            ProjectFilename = _project.Location;
        }

        public override void Describe(Action<string> writeLine)
        {
            writeLine("Invalid assembly redirection to " + _packageId);
        }

        public override ProblemFix GetFix()
        {
            return new ProblemFix($"change assembly redirection to {_packageId} for {_project.Location}", Fix);
        }


        protected override bool GetIsBigProblem()
        {
            return true;
        }

        private void Fix()
        {
            var fn  = ProjectFilename.GetAppConfigFile();
            var xml = new AppConfig(fn);
            if (!xml.Exists)
                throw new Exception(string.Format("Config file {0} doesn't exist", fn.FullName));
            var node = xml.FindByAssemblyIdentity(_packageId);
            if (node == null)
                throw new Exception(string.Format("Redirection for '{0}' not found", _packageId));
            node.SetPackageId(_packageId);
            xml.Save();
        }

        private readonly string  _packageId;
        private readonly Project _project;
    }
}