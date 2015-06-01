using System;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public class NugetVersion : IEquatable<NugetVersion>
    {
        #region Static Methods

        // Public Methods 

        public static bool operator !=(NugetVersion left, NugetVersion right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(NugetVersion left, NugetVersion right)
        {
            return Equals(left, right);
        }

        public static NugetVersion Parse(string ver)
        {
            if (ver == null) throw new ArgumentNullException("ver");
            var i = ver.IndexOf("-");
            var result = new NugetVersion();
            if (i >= 0)
            {
                result.Version = Version.Parse(ver.Substring(0, i));
                result.Suffix = ver.Substring(i + 1).Trim();
            }
            else
            {
                result.Version = Version.Parse(ver);
                result.Suffix = "";
            }
            return result;

        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public bool Equals(NugetVersion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(NormalizedVersion, other.NormalizedVersion) && string.Equals(Suffix, other.Suffix);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((NugetVersion)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((NormalizedVersion != null ? NormalizedVersion.GetHashCode() : 0) * 397)
                    ^ (Suffix != null ? Suffix.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Suffix) ? Version.ToString() : Version + "-" + Suffix;
        }

        #endregion Methods

        #region Properties

        public Version NormalizedVersion
        {
            get
            {
                return new Version(
                           Math.Max(0, Version.Major),
                           Math.Max(0, Version.Minor),
                           Math.Max(0, Version.Build),
                           Math.Max(0, Version.Revision)
                        );
            }
        }

        public Version Version { get; set; }

        public string Suffix { get; set; }

        #endregion Properties
    }
}