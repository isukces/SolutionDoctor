using System;
using System.Linq;
using ISukces.SolutionDoctor.Logic.Problems;
using Xunit;

namespace ISukces.SolutionDoctor.Test
{
    public class RichStringTests
    {
        [Fact]
        public void T01_Should_create_RichString()
        {
            var x = RichString.RichFormat(ctx => { }, "bla {1} {0}", "#", "!");
            var items = x.Items;
            Assert.Equal(4, items.Count);
            var allDebug = string.Join("\r\n", items.Select(a => a.DebugString()));
            Assert.Equal(@"{""Text"":""bla ""}
{""Text"":""!""}
{""Text"":"" ""}
{""Text"":""#""}", allDebug);

        }
        
        [Fact]
        public void T02_Should_create_RichString()
        {
            var x     = RichString.RichFormat(ctx =>
            {
                if (ctx.Before)
                    ctx.Sink.Add(new RichString.ConsoleControl
                    {
                        TextColor = (ConsoleColor)ctx.Iteration
                    });
                else
                    ctx.ResetColors();
            }, "bla {1} {0}", "#", "!");
            var items = x.Items;
            Assert.Equal(8, items.Count);
            var allDebug = string.Join("\r\n", items.Select(a => a.DebugString()));
            Assert.Equal(@"{""Text"":""bla ""}
{""TextColor"":""Black""}
{""Text"":""!""}
{""ResetColors"":true}
{""Text"":"" ""}
{""TextColor"":""DarkBlue""}
{""Text"":""#""}
{""ResetColors"":true}", allDebug);

        }
        
    }
}