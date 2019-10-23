using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public struct RichString
    {
        public RichString(string s)
            : this(new ConsoleControl {Text = s})
        {
        }

        public RichString(ConsoleColor color, string x)
        {
            _items = new[]
            {
                new ConsoleControl {TextColor = color, Text = x}
            };
        }

        public RichString(params ConsoleControl[] items)
        {
            _items = items;
        }


        public static RichString operator +(RichString a, RichString b)
        {
            if (a.IsEmpty) return b;
            if (b.IsEmpty) return a;
            var items = new List<ConsoleControl>(a._items.Length + b._items.Length);
            items.AddRange(a._items);
            items.AddRange(b._items);
            return new RichString(items.ToArray());
        }

        public static RichString operator +(RichString a, ConsoleControl b)
        {
            if (a.IsEmpty) return new RichString(b);
            var items = new List<ConsoleControl>(a._items.Length + 1);
            items.AddRange(a._items);
            items.Add(b);
            return new RichString(items.ToArray());
        }

        public static implicit operator RichString(string x)
        {
            return new RichString(x);
        }

        public static RichString RichFormat(Action<FormatContext> action, string format, params object[] args)
        {
            var m = r.Match(format);
            if (!m.Success)
                return string.Format(format);
            var ctx = new FormatContext
            {
                Sink      = new List<ConsoleControl>(),
                Args      = args,
                Iteration = -1
            };
            var from = 0;
            while (m.Success)
            {
                var idx  = m.Groups[0].Index;
                var len  = m.Groups[0].Length;
                var rest = m.Groups[2].Value;

                var copy = idx - from;
                if (copy > 0)
                    ctx.Add(format.Substring(from, copy));

                ctx.Iteration++;
                ctx.ParameterIndex = int.Parse(m.Groups[1].Value);

                ctx.Before               = true;
                ctx.AutoResetColorsAfter = false;
                action(ctx);

                var text = string.Format("{0" + rest + "}", args[ctx.ParameterIndex]);
                ctx.Add(text);

                if (ctx.AutoResetColorsAfter)
                    ctx.ResetColors();

                ctx.Before = false;
                action(ctx);
                from = idx + len;
                m    = m.NextMatch();
            }

            ctx.Add(format.Substring(from));
            return new RichString(ctx.Sink.ToArray());
        }

        public string GetPureText()
        {
            return string.Join("", Items.Select(a => a.Text));
        }

        public RichString WithColors(ConsoleColor? textColor, ConsoleColor? backgroundColor)
        {
            if (textColor is null && backgroundColor is null)
                return this;
            return this + new ConsoleControl
            {
                BackgroundColor = backgroundColor,
                TextColor       = textColor
            };
        }

        public bool IsEmpty
        {
            get { return _items is null || _items.Length == 0; }
        }

        public IReadOnlyList<ConsoleControl> Items
        {
            get { return _items; }
        }

        private static readonly Regex r = new Regex(f, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ConsoleControl[] _items;

        public struct ConsoleControl
        {
            public ConsoleControl(string text) : this()
            {
                Text = text;
            }

            public string DebugString()
            {
                return JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling    = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter()
                    }
                });
            }

            public string        Text            { get; set; }
            public ConsoleColor? TextColor       { get; set; }
            public ConsoleColor? BackgroundColor { get; set; }
            public bool          ResetColors     { get; set; }
        }

        private const string f = @"\{\s*(\d+)([^}]*)\}";

        public class FormatContext
        {
            public void Add(string text)
            {
                if (string.IsNullOrEmpty(text)) return;
                Sink.Add(new ConsoleControl(text));
            }

            public void ResetColors()
            {
                Sink.Add(new ConsoleControl {ResetColors = true});
            }

            public void SetTextColor(ConsoleColor textColor)
            {
                Sink.Add(new ConsoleControl
                {
                    TextColor = textColor
                });
            }

            public List<ConsoleControl> Sink                 { get; set; }
            public int                  ParameterIndex       { get; set; }
            public int                  Iteration            { get; set; }
            public object[]             Args                 { get; set; }
            public bool                 Before               { get; set; }
            public bool                 AutoResetColorsAfter { get; set; }
        }
    }
}