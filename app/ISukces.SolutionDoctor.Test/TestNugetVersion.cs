using System;
using iSukces.Code.vssolutions;
using Xunit;

namespace ISukces.SolutionDoctor.Test
{
    public class TestNugetVersion
    {
        #region Static Methods

        // Public Methods 

        [Fact]
        public static void Compare()
        {
            var ver11 = new NugetVersion()
            {
                Version = new Version(1, 1)
            };
            var ver13 = new NugetVersion()
            {
                Version = new Version(1, 3)
            };
            var ver1300 = new NugetVersion()
            {
                Version = new Version(1, 3, 0, 0)
            };


            var ver12 = new NugetVersion()
            {
                Version = new Version(1, 2)
            };

            var ver12beta = new NugetVersion()
            {
                Version = new Version(1, 2),
                Suffix = "beta"
            };






            EqualTest(ver13, ver1300);

            CompareTest(ver11, ver12, ver12beta, ver13, ver1300);
            CompareTest(ver12, ver12beta, ver13, ver1300);
            CompareTest(ver12beta, ver13, ver1300);
        }
        // Private Methods 

        private static void CompareTest(NugetVersion lower, params NugetVersion[] highers)
        {
            foreach (var higher in highers)
                CompareTest(lower, higher);
        }

        private static void CompareTest(NugetVersion lower, NugetVersion higher)
        {
            Assert.Equal(-1, lower.CompareTo(higher));
            Assert.Equal(1, higher.CompareTo(lower));

            Assert.Equal(0, lower.CompareTo(lower));
            Assert.Equal(0, higher.CompareTo(higher));


            Assert.Equal(1, lower.CompareTo(null));
            Assert.Equal(1, higher.CompareTo(null));
        }

        private static void EqualTest(NugetVersion a, NugetVersion b)
        {
            Assert.Equal(0, a.CompareTo(b));
            Assert.Equal(0, b.CompareTo(a));
        }

        #endregion Static Methods
    }

    
}
