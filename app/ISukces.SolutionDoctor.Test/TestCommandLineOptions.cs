using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic;
using Xunit;

namespace ISukces.SolutionDoctor.Test
{

    public class TestCommandLineOptions
    {
        [Fact]
        public void Empty()
        {
            var q = CommandLineOptions.Parse(null);
            Assert.Null(q);
            q = CommandLineOptions.Parse(new string[0]);
            Assert.Null(q);
        }

        [Fact]
        public void Fix()
        {
            var q = CommandLineOptions.Parse(new [] { "-fix"});
            Assert.NotNull(q);
            Assert.True(q.Fix);
            Assert.False(q.ShowOnlyBigProblems);
        }

        [Fact]
        public void ShowOnlyBigProblems()
        {
            var q = CommandLineOptions.Parse(new[] { "-onlyBig" });
            Assert.NotNull(q);
            Assert.False(q.Fix);
            Assert.True(q.ShowOnlyBigProblems);
        }
    }
}
