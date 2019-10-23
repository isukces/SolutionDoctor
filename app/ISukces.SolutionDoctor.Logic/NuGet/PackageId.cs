using System;
using System.Collections.Generic;

namespace ISukces.SolutionDoctor.Logic.NuGet
{
    public class PackageId : IEquatable<PackageId>, IComparable<PackageId>, IComparable, INuspec
    {
        public PackageId(string id, NugetVersion packageVersion, string fullId)
        {
            Id             = id;
            PackageVersion = packageVersion;
            FullId         = fullId;
        }

        public static bool operator ==(PackageId left, PackageId right)
        {
            return Equals(left, right);
        }

        public static bool operator >(PackageId left, PackageId right)
        {
            return Comparer<PackageId>.Default.Compare(left, right) > 0;
        }

        public static bool operator >=(PackageId left, PackageId right)
        {
            return Comparer<PackageId>.Default.Compare(left, right) >= 0;
        }

        public static bool operator !=(PackageId left, PackageId right)
        {
            return !Equals(left, right);
        }

        public static bool operator <(PackageId left, PackageId right)
        {
            return Comparer<PackageId>.Default.Compare(left, right) < 0;
        }

        public static bool operator <=(PackageId left, PackageId right)
        {
            return Comparer<PackageId>.Default.Compare(left, right) <= 0;
        }

        public int CompareTo(PackageId other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
            if (idComparison != 0) return idComparison;
            return Comparer<NugetVersion>.Default.Compare(PackageVersion, other.PackageVersion);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is PackageId other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(PackageId)}");
        }

        public bool Equals(PackageId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && Equals(PackageVersion, other.PackageVersion) &&
                   string.Equals(FullId, other.FullId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PackageId)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id != null ? Id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (PackageVersion != null ? PackageVersion.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FullId != null ? FullId.GetHashCode() : 0);
                return hashCode;
            }
        }

        public string       Id             { get; }
        public NugetVersion PackageVersion { get; }
        public string       FullId         { get; }
    }
}