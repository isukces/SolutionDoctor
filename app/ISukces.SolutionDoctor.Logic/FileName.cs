using System;
using System.IO;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic
{
    public class FileName : IEquatable<FileName>
    {
        #region Constructors
        private FileName(string fullName)
        {
            _fullName = fullName;
#if PLATFORM_UNIX
            _hash = _fullName.GetHashCode();
#else
            _nameForCompare = _fullName.ToLowerInvariant();
            _hash = _nameForCompare.GetHashCode();
#endif
        }
        public FileName([NotNull] FileInfo fileInfo)
            : this(fileInfo.FullName)
        {
        }

        #endregion Constructors

        #region Static Methods

        // Public Methods 

        public static bool operator !=(FileName left, FileName right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(FileName left, FileName right)
        {
            return Equals(left, right);
        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public bool Equals(FileName other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
#if PLATFORM_UNIX
            return string.Equals(_fullName, other._fullName);
#else
            return string.Equals(_nameForCompare, other._nameForCompare);
#endif
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FileName)obj);
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return _fullName;
        }

        #endregion Methods

        #region Fields

        readonly string _fullName;
        readonly int _hash;

        #endregion Fields

        #region Properties

        public string FullName
        {
            get
            {
                return _fullName;
            }
        }

        public string Name
        {
            get
            {
                return new FileInfo(_fullName).Name;
            }
        }

        public bool Exists
        {
            get
            {
                return File.Exists(_fullName);
            }
        }

        public DirectoryInfo Directory
        {
            get
            {
                return new FileInfo(_fullName).Directory;

            }
        }

        #endregion Properties

#if !PLATFORM_UNIX
        readonly string _nameForCompare;
#endif
    }
}
