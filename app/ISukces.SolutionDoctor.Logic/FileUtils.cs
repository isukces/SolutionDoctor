using System.Xml.Linq;

namespace ISukces.SolutionDoctor.Logic
{
    public static class FileUtils
    {
        public static XDocument Load(string fileName)
        {
            lock(SyncObj)
            {
                return XDocument.Load(fileName);
            }
        }

        public static XDocument Load(FileName fileName)
        {
            lock(SyncObj)
            {
                return XDocument.Load(fileName.FullName);
            }
        }

        public static void Save2(this XDocument xml, FileName fileName)
        {
            lock(SyncObj)
            {
                xml.Save(fileName.FullName);
            }
        }

        public static object SyncObj = new object();
    }
}