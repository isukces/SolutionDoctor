using System.Xml.Linq;
using isukces.code.vssolutions;

namespace ISukces.SolutionDoctor.Logic
{
    public static class FileUtils
    {
        public static XDocument Load(string fileName)
        {
            lock(Locking.Lock)
            {
                return XDocument.Load(fileName);
            }
        }

        public static XDocument Load(FileName fileName)
        {
            lock(Locking.Lock)
            {
                return XDocument.Load(fileName.FullName);
            }
        }

        public static void Save2(this XDocument xml, FileName fileName)
        {
            lock(Locking.Lock)
            {
                xml.Save(fileName.FullName);
            }
        }
 
    }
}