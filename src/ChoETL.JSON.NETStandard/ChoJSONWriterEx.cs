using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJSONWriterEx
    {
        public static void ParallelWrite<T>(string jsonFilePath, IEnumerable<T> values, int chunkSize = 1000,
            Action<ChoJSONRecordConfiguration> config = null)
        {
            if (chunkSize <= 0) throw new ArgumentException(nameof(chunkSize));

            var chunks = values.ChunkEx(chunkSize).ToArray();
            //chunks.Length.Print();

            List<string> outFilePathList = new List<string>();
            Parallel.ForEach(chunks, (data, state, index) =>
            {
                var chunk = data.ToArray();
                var outFilePath = ChoFile.GetFileName(jsonFilePath, index);
                outFilePathList.Add(outFilePath);
                //outFilePath.Print();

                using (var w = new ChoJSONWriter(outFilePath)
                    .UseJsonSerialization(true)
                    )
                {
                    w.Configure(config);
                    //w.UseJsonSerialization();

                    w.Write(data.Select(rec => rec.ConvertToNestedObject()));
                }
            });
            ChoFile.ConcatFiles(outFilePathList.ToArray(), jsonFilePath);
            ChoFile.DeleteFiles(outFilePathList.ToArray());
        }
    }
}
