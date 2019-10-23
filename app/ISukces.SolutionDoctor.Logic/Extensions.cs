﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ISukces.SolutionDoctor.Logic
{
    public static class Extensions
    {
        public static void CheckValidForRead(this FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            if (!file.Exists)
                throw new FileNotFoundException(string.Format("File {0} doesn't exist", file.FullName));
        }

        public static IEnumerable<TOut> GetUnique<TOut, TIn>(this IEnumerable<TIn> src, Func<TIn, string> getKey,
            Func<TIn, TOut> map)
        {
            var x = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in src)
            {
                var key = getKey(i);
                if (x.Contains(key))
                    continue;
                x.Add(key);
                yield return map(i);
            }
        }

        public static void WriteFormat(this Action<string> writeLine, string format, params object[] items)
        {
            writeLine(string.Format(format, items));
        }

        // Public Methods 

        public static FileName GetAppConfigFile(this FileName projectFile)
        {
            return projectFile.GetRelativeFile("app.config");
        }

        public static FileName GetPackagesConfigFile(this FileName projectFile)
        {
            return projectFile.GetRelativeFile("packages.config");
        }

        public static string GetShortNameWithoutExtension(this FileName projectFile)
        {
            var fi   = new FileInfo(projectFile.FullName);
            var ext  = fi.Extension;
            var name = fi.Name;
            return name.Substring(0, name.Length - ext.Length);
        }
        // Private Methods 

        private static FileName GetRelativeFile(this FileName projectFile, string name)
        {
            // ReSharper disable once PossibleNullReferenceException
            var fi             = new FileInfo(projectFile.FullName);
            var configFileInfo = new FileInfo(Path.Combine(fi.Directory.FullName, name));
            return new FileName(configFileInfo);
        }

        static HashSet<char> c= new HashSet<char>(Path.GetInvalidFileNameChars());
        private static Regex fiePathRegex = new Regex(@"^(\w:)?(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public static string Quote(this string text)
        {
            return "\"" + text + "\"";
        }
        
        public static string QuoteFilename(this string fileName)
        {
            var m = fiePathRegex.Match(fileName);
            if (!m.Success) return fileName.Quote();
            var x = m.Groups[2].Value;
            for (var index = 0; index < x.Length; index++)
            {
                var i = x[index];
                if (i == '\\')
                    continue;
                if (c.Contains(i))
                    return fileName.Quote();
            }

            return fileName;
        }
    }
}