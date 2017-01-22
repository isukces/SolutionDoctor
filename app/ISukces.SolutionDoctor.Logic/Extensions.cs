using System;
using System.Collections.Generic;
using System.IO;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic
{
    public static class Extensions
    {
        #region Static Methods

        // Public Methods 

        public static FileName GetAppConfigFile(this FileName projectFile)
        {
            return projectFile.GetRelativeFile("app.config");
        }

        public static FileName GetPackagesConfigFile(this FileName projectFile)
        {
            return projectFile.GetRelativeFile("packages.config");
        }
        // Private Methods 

        private static FileName GetRelativeFile(this FileName projectFile, string name)
        {
            // ReSharper disable once PossibleNullReferenceException
            var fi = new FileInfo(projectFile.FullName);
            var configFileInfo = new FileInfo(Path.Combine(fi.Directory.FullName, name));
            return new FileName(configFileInfo);
        }

        #endregion Static Methods

        public static void CheckValidForRead(this FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            if (!file.Exists)
                throw new FileNotFoundException(string.Format("File {0} doesn't exist", file.FullName));
        }

        public static IEnumerable<TOut> GetUnique<TOut, TIn>(this IEnumerable<TIn> src, Func<TIn, string> getKey, Func<TIn, TOut> map)
        {
            HashSet<string> x = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in src)
            {
                string key = getKey(i);
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
    }
}
