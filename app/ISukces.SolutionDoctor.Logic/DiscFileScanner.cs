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

        public DiscFileScanner(string filter, Func<FileInfo, bool> accept)
        {
            _filter = filter;
            _accept = accept;
        }

        #endregion Constructors

        #region Static Methods

        // Public Methods 

        public static IObservable<FileInfo> MakeObservable(DirectoryInfo dir, string filter, Func<FileInfo, bool> accept)
        {
            return Observable.Create<FileInfo>(
                observer =>
                {
                    try
                    {
                        var tmp = new DiscFileScanner(filter, accept);
                        tmp.ScanToObservable(dir, observer);
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

        private void ScanToObservable(DirectoryInfo directory, IObserver<FileInfo> observer)
        {
            if (!directory.Exists)
                return;
            var files = directory
                .GetFiles(_filter)
                .Where(fileInfo => _accept(fileInfo));
            foreach (var fileInfo in files)
            {
                observer.OnNext(fileInfo);
            }
            foreach (var directoryInfo in directory.GetDirectories())
                ScanToObservable(directoryInfo, observer);
        }

        #endregion Methods

        #region Fields

        readonly Func<FileInfo, bool> _accept;
        readonly string _filter;

        #endregion Fields
    }
}