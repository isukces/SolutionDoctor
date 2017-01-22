#region using

using System;
using Newtonsoft.Json;

#endregion

namespace ISukces.SolutionDoctor.Logic.NuGet
{
    public class NugetVersion : IEquatable<NugetVersion>, IComparable<NugetVersion>
    {
        #region Static Methods

        // Public Methods 

        public static NugetVersion Parse(string ver)
        {
            if (ver == null) throw new ArgumentNullException("ver");
            ver = ver.Trim();
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

        #endregion

        #region Instance Methods

        public bool ShouldSerializeSuffix()
        {
            return !string.IsNullOrEmpty(Suffix);
        }


        #endregion

        #region Properties

        [JsonIgnore]
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

        #endregion

        #region Methods

        // Public Methods 

        public int CompareTo(NugetVersion other)
        {
            if (other == null)
                return 1;
            var a = NormalizedVersion.CompareTo(other.NormalizedVersion);
            if (a != 0) return a;
            var tmp1 = !string.IsNullOrEmpty(Suffix);
            var tmp2 = !string.IsNullOrEmpty(other.Suffix);
            a = tmp1.CompareTo(tmp2);
            if (a != 0) return a;
            return (Suffix ?? "").CompareTo(other.Suffix ?? "");
        }

        public bool Equals(NugetVersion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(NormalizedVersion, other.NormalizedVersion)
                && string.Equals(Suffix?.Trim() ?? "", other.Suffix?.Trim() ?? "");
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

        #region Operators

        public static bool operator !=(NugetVersion left, NugetVersion right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(NugetVersion left, NugetVersion right)
        {
            return Equals(left, right);
        }

        public static bool operator >(NugetVersion a, NugetVersion b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator >=(NugetVersion a, NugetVersion b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <(NugetVersion a, NugetVersion b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(NugetVersion a, NugetVersion b)
        {
            return a.CompareTo(b) <= 0;
        }

        #endregion
    }
}