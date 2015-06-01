﻿using System;
using System.IO;

namespace ISukces.SolutionDoctor.Logic
{
    public static class Extensions
    {
        #region Static Methods

        // Public Methods 

        public static FileInfo GetAppConfigFile(this FileInfo projectFile)
        {
            return projectFile.GetRelativeFile("app.config");
        }

        public static FileInfo GetPackagesConfigFile(this FileInfo projectFile)
        {
            return projectFile.GetRelativeFile("packages.config");
        }
        // Private Methods 

        private static FileInfo GetRelativeFile(this FileInfo projectFile, string name)
        {
            // ReSharper disable once PossibleNullReferenceException
            var configFileInfo = new FileInfo(Path.Combine(projectFile.Directory.FullName, name));
            return configFileInfo;
        }

        #endregion Static Methods

        public static void CheckValidForRead(this FileInfo file)
        {
            if (file == null) 
                throw new ArgumentNullException("file");
            if (!file.Exists)
                throw new FileNotFoundException(string.Format("File {0} doesn't exist", file.FullName));
        }
    }
}
