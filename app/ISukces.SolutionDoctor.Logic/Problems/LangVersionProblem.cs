using iSukces.Code.VsSolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class LangVersionProblem : Problem
    {
        public LangVersionProblem(bool found, List<string> anyToFix, SolutionProject project, Action fix,
            string expectedLangVersion)
        {
            _found               = found;
            _project             = project;
            _fix                 = fix;
            _expectedLangVersion = expectedLangVersion;
            _anyToFix            = anyToFix.Distinct().OrderBy(a => a).ToArray();
        }

        public override void Describe(Action<RichString> writeLine)
        {
            if (_anyToFix.Length > 0)
                writeLine($"Project {_project.Location} has invalid lang versions {(string.Join(",", _anyToFix))}");
            if (!_found)
                writeLine($"Project {_project.Location} has no lang version");
        }

        public override ProblemFix GetFix()
        {
            if (_fix is null)
                return null;
            return new ProblemFix($"Set lang version to {_expectedLangVersion}", _fix);
        }

        public override FixScript GetFixScript()
        {
            return null;
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #region Fields

        private readonly bool _found;
        private readonly SolutionProject _project;
        private readonly Action _fix;
        private readonly string _expectedLangVersion;
        private readonly string[] _anyToFix;

        #endregion
    }
}
