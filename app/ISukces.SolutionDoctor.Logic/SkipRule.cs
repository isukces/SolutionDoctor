namespace ISukces.SolutionDoctor.Logic
{
    public class SkipRule : IEquatable<SkipRule>
    {
        public static bool operator ==(SkipRule left, SkipRule right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SkipRule left, SkipRule right)
        {
            return !left.Equals(right);
        }

        public bool Equals(SkipRule other)
        {
            return Project == other.Project && Rule == other.Rule;
        }

        public override bool Equals(object obj)
        {
            return obj is SkipRule other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Project != null ? Project.GetHashCode() : 0) * 397) ^ (Rule != null ? Rule.GetHashCode() : 0);
            }
        }

        #region properties

        public string Rule    { get; set; }
        public string Project { get; set; }

        #endregion
    }
}
