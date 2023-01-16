namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class ProblemFix
    {
        public ProblemFix(string description, Action fixAction)
        {
            Description = description;
            _fixAction  = fixAction;
        }

        public void Fix()
        {
            _fixAction();
        }

        #region properties

        public string Description { get; }

        #endregion

        #region Fields

        private readonly Action _fixAction;

        #endregion
    }
}
