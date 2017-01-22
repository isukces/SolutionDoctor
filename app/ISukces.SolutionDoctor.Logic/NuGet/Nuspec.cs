﻿#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

#endregion

namespace ISukces.SolutionDoctor.Logic.NuGet
{
    public class Nuspec
    {
        #region Constructors

        private Nuspec()
        {
            // dla serializacji
        }

        private Nuspec(XDocument xml, DirectoryInfo location)
        {
            Location = location;
            var root = xml.Root;
            if (root == null) throw new NullReferenceException("xml.Root");
            _metadata = root.Elements().First(a => a.Name.LocalName == "metadata");
            if (root == null) throw new NullReferenceException("xml.Root.metadata node");

            var id = GetNode("id").Value;
            var ver = GetNode("version").Value;
            FullId = id + "." + ver;
            Id = id;
            PackageVersion = NugetVersion.Parse(ver);

            {
                var dep = GetNode("dependencies");
                if (dep != null)
                    Dependencies = dep
                        .Elements(dep.Name.Namespace + "dependency")
                        .Select(NugetDependency.FromNode)
                        .ToList();
                else
                    Dependencies = new List<NugetDependency>();
            }
        }

        #endregion

        #region Static Methods

        public static IReadOnlyList<Nuspec> GetRepositories(DirectoryInfo directory)
        {
            var result = new List<Nuspec>();
            if (!directory.Exists) return result;
            var cache1 = NuspecCache.GetForDirectory(directory);
            foreach (var i in directory.GetDirectories())
            {
                if (cache1.ContainsKey(i.Name))
                {
                    result.Add(cache1[i.Name]);
                    continue;
                }
                var fn = new FileInfo(Path.Combine(i.FullName, i.Name + ".nupkg"));
                if (!fn.Exists) continue;
                try
                {
                    var nuspec = Load(fn);
                    result.Add(nuspec);
                    cache1[i.Name] = nuspec;
                }
                catch
                {
                    Console.WriteLine($"File {fn.FullName} is broken");
                }
            }
            NuspecCache.Save(directory, cache1);
            return result;
        }

        public static Nuspec Load([NotNull] FileInfo file)
        {
            file.CheckValidForRead();

            using(var ms = new FileStream(file.FullName, FileMode.Open))
            {
                // musi być wpisane do w ten sposób, bo jak zrobimy new MemoryStream(data) to wtedy strumień nie jest "expandable"
                using(var zip = new ZipArchive(ms, ZipArchiveMode.Read, false))
                {
                    var e = zip.Entries
                        .Where(entry =>
                            string.Equals(entry.FullName, entry.Name, StringComparison.OrdinalIgnoreCase)
                            && entry.Name.ToLower().EndsWith(".nuspec"))
                        .ToArray();
                    if (e.Length > 1)
                        throw new Exception(string.Format("Too many nuspec files in {0}", file.FullName));
                    if (e.Length == 0)
                        return null;
                    using(var zippedStream = e.First().Open())
                    {
                        var xml = XDocument.Load(zippedStream);
                        return new Nuspec(xml, file.Directory);
                    }
                }
            }
        }

        #endregion

        #region Instance Methods

        public bool ShouldSerializeDependencies()
        {
            return (Dependencies != null) && Dependencies.Any();
        }

        // Public Methods 
        public override string ToString()
        {
            return string.Format("nuspec: {0}", FullId);
        }

        #region Methods

        // Private Methods 

        private XElement GetNode(string nodeName)
        {
            return _metadata.Element(_metadata.Name.Namespace + nodeName);
        }

        #endregion Methods

        #endregion

        #region Properties

        public string Id { get; set; }

        public string FullId { get; set; }

        public DirectoryInfo Location { get; set; }

        public NugetVersion PackageVersion { get; set; }

        public List<NugetDependency> Dependencies { get; set; } = new List<NugetDependency>();

        #endregion

        #region Fields

        private readonly XElement _metadata;

        #endregion
    }
}