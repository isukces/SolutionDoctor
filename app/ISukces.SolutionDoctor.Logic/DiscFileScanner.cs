using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ISukces.SolutionDoctor.Logic
{
    public class DiscFileScanner
    {
        public DiscFileScanner(string filter, Func<FileInfo, bool> accept, IReadOnlyList<string> excludeDirs)
        {
            _filter      = filter;
            _accept      = accept;
            _excludeDirs = excludeDirs;
        }

        private static DirectoryInfo[] GetDirectories2(DirectoryInfo dir)
        {
            try
            {
                return dir.GetDirectories();
            }
            catch (PathTooLongException)
            {
                return Array.Empty<DirectoryInfo>();
            }
        }

        private static FileInfo[] GetFilesAndIgnoreError(DirectoryInfo dir, string filter)
        {
            var now = DateTime.Now;
            if ((now - _aaa).TotalSeconds > 5)
            {
                Debug.WriteLine("Scan " + dir.FullName);
                _aaa = now;
            }

            try
            {
                return dir.GetFiles(filter);
            }
            catch (PathTooLongException)
            {
                return Array.Empty<FileInfo>();
            }
        }

        public static IObservable<FileInfo> MakeObservable(IReadOnlyList<DirectoryInfo> dirs, string filter,
            Func<FileInfo, bool> accept, IReadOnlyList<string> excludeDirs)
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


        private bool ExcludeDirectory(DirectoryInfo di)
        {
            var dn = di.Name;
            var skipByName = string.Equals(dn, "bin", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(dn, "obj", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(dn, "packages", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(dn, ".git", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(dn, ".idea", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(dn, ".vs", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(dn, "wwwroot", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(dn, "node_modules", StringComparison.OrdinalIgnoreCase);
            if (skipByName)
                return true;
            var n = di.FullName.ToLower() + "\\";
            foreach (var i in _excludeDirs)
                if (i == n)
                    return true;
            return false;
        }

        //�Public�Methods�

        public IEnumerable<FileInfo> Scan(DirectoryInfo directory)
        {
            if (!directory.Exists)
                return Array.Empty<FileInfo>();
            var files = directory
                .GetFiles(_filter)
                .Where(fileInfo => _accept(fileInfo)).ToList();
            var fromSubfolders = directory.GetDirectories().SelectMany(Scan);
            return files.Count == 0
                ? fromSubfolders
                : files.Concat(fromSubfolders);
        }

        private void ScanToObservable(IReadOnlyList<DirectoryInfo> directories, IObserver<FileInfo> observer)
        {
            if (directories == null || directories.Count == 0)
                return;
            directories = directories.Where(a => a.Exists && !ExcludeDirectory(a)).ToArray();
            if (directories.Count == 0)
                return;

            var files = directories.SelectMany(a => GetFilesAndIgnoreError(a, _filter))
                //.SelectMany<DirectoryInfo, DirectoryInfo>(a => a)
                //.GetFiles(_filter)
                .Where(fileInfo => _accept(fileInfo));
            foreach (var fileInfo in files) observer.OnNext(fileInfo);
            foreach (var directoryInfo in directories.SelectMany(GetDirectories2))
                ScanToObservable(new[] { directoryInfo }, observer);
        }

        #region Fields

        static DateTime _aaa = DateTime.MinValue;


        private readonly Func<FileInfo, bool> _accept;
        private readonly IReadOnlyList<string> _excludeDirs;
        private readonly string _filter;

        #endregion
    }
}
