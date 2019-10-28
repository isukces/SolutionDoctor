using System.IO;
using System.Linq;
using isukces.code.vssolutions;
using ISukces.SolutionDoctor.Logic;
using Xunit;

namespace ISukces.SolutionDoctor.Test
{
    public class TestFileName
    {
        #region Static Methods

        // Public Methods 

        [Fact]
        public static void Equality()
        {
            var fn1 = new FileName(new FileInfo("c:\\command.com"));
            var fn2 = new FileName(new FileInfo("C:\\command.com"));
            var fn3 = new FileName(new FileInfo("d:\\command.com"));
#if PLATFORM_UNIX
            Assert.NotEqual(fn1, fn2);
#else
            Assert.Equal(fn1, fn2);
#endif
            Assert.NotEqual(fn2, fn3);
        }

        [Fact]
        public static void Grouping()
        {
            var fn1 = new FileName(new FileInfo("c:\\command.com"));
            var fn2 = new FileName(new FileInfo("C:\\command.com"));
            var array = new[] { fn1, fn2 };
            var distinct = array.Distinct().ToArray();
#if PLATFORM_UNIX
            Assert.Equal(2, distinct.Length);
#else
            Assert.Equal(1, distinct.Length);
#endif
        }

        [Fact]
        public static void ShortName()
        {
            var fn1 = new FileName(new FileInfo("c:\\command.com"));
            Assert.Equal("command.com", fn1.Name);
        }

        #endregion Static Methods
    }
}