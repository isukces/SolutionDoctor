using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace ISukces.SolutionDoctor.Logic
{
    public class DiscFileScanner
    {
        #region Constructors

        public DiscFileScanner(string filter, Func<FileInfo, bool> accept, IReadOnlyList<string> excludeDirs)
        {
            _filter = filter;
            _accept = accept;
            _excludeDirs = excludeDirs;
        }

        #endregion Constructors

        #region Static Methods

        // Public Methods 

        public static IObservable<FileInfo> MakeObservable(IReadOnlyList<DirectoryInfo> dirs, string filter, Func<FileInfo, bool> accept, IReadOnlyList<string> excludeDirs)
        {
            return Observable.Create<FileInfo>(
                observer =>
                {
                    try
                    {
                        var tmp = new DiscFileScanner(filter, accept, excludeDirs);
                        tmp.ScanToObservable(dirs, observer);
                        observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        observer.OnError(e);
                    }
                    return Disposable.Empty;
                });

        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public IEnumerable<FileInfo> Scan(DirectoryInfo directory)
        {
            if (!directory.Exists)
                return new FileInfo[0];
            var files = directory
                .GetFiles(_filter)
                .Where(fileInfo => _accept(fileInfo)).ToList();
            var fromSubfolders = directory.GetDirectories().SelectMany(Scan);
            return files.Count == 0
                ? fromSubfolders
                : files.Concat(fromSubfolders);
        }
        // Private Methods 

        bool ExD(DirectoryInfo di)
        {
            if (di.Name.ToLower() == ".git")
                return true;
            var n = di.FullName.ToLower() + "\\";
            foreach (var i in _excludeDirs)
                if (i == n)
                    return true;
            return false;
        }

        private void ScanToObservable(IReadOnlyList<DirectoryInfo> directories, IObserver<FileInfo> observer)
        {
            if (directories == null || directories.Count == 0)
                return;
            directories = directories.Where(a => a.Exists && !ExD(a)).ToArray();
            if (directories.Count == 0)
                return;


            var files = directories.SelectMany(a => GetFiles2(a, _filter))
                //.SelectMany<DirectoryInfo, DirectoryInfo>(a => a)
                //.GetFiles(_filter)
                .Where(fileInfo => _accept(fileInfo));
            foreach (var fileInfo in files)
            {
                observer.OnNext(fileInfo);
            }
            foreach (var directoryInfo in directories.SelectMany(GetDirectories2))
                ScanToObservable(new[] { directoryInfo }, observer);
        }

        private static DirectoryInfo[] GetDirectories2(DirectoryInfo dir)
        {
            try
            {
                return dir.GetDirectories();
            }
            catch (System.IO.PathTooLongException e)
            {
                return new DirectoryInfo[0];
            }
        }

        private FileInfo[] GetFiles2(DirectoryInfo dir, string filter)
        {

            try
            {
                return dir.GetFiles(_filter);
            }
            catch (System.IO.PathTooLongException e)
            {
                return new FileInfo[0];
            }
        }

        #endregion Methods

        #region Fields

        readonly Func<FileInfo, bool> _accept;
        private readonly IReadOnlyList<string> _excludeDirs;
        readonly string _filter;

        #endregion Fields
    }
}