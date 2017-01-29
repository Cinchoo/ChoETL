using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoTextFileExternalSorter<T> : ChoExternalSorter<T>
    {
        public ChoTextFileExternalSorter(IComparer<T> comparer, int capacity, int mergeCount)
            : base(comparer, capacity, mergeCount)
        {
        }

        protected override string Write(IEnumerable<T> run)
        {
            var file = Path.GetTempFileName();
            using (var writer = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new BinaryFormatter().Serialize(writer, run.ToArray());
            }
            return file;
        }

        protected override IEnumerable<T> Read(string name)
        {
            T[] arr = null;
            using (var reader = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                arr = (T[])new BinaryFormatter().Deserialize(reader);
            }
            File.Delete(name);
            foreach (T t in arr)
                yield return t;
        }
    }
}
