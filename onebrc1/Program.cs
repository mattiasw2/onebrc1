#define SINGLE_THREAD

namespace onebrc1
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Text;

    class Program
    {


        static void Main()
        {
            var fileNames = new List<(string FileName, int RecordCount)>
            {
                // ("c:/data6/cs/onebrc1/onebrc1/small.txt", 8),
                // ("c:/data6/cs/onebrc1/onebrc1/medium.txt", 1000),
                ("c:/data6/cs/onebrc1/onebrc1/measurements_1000000.txt", 1000000),
                ("c:/data6/cs/onebrc1/onebrc1/measurements_250000000.txt", 250000000),
                // ("c:/data6/cs/onebrc1/onebrc1/measurements_1000000000.txt", 1000000000)
            };

            foreach (var (fileName, recordCount) in fileNames)
            {
                long start = Stopwatch.GetTimestamp();

                long fileSize = new FileInfo(fileName).Length;
                long noOfChunks = 28;
                long chunkSize = fileSize / noOfChunks + 1;
                long overlapChunkSize = Math.Min(1000, fileSize / noOfChunks);

                var dictionaries = new CustomByteDictionary<float>[noOfChunks];

                Action<long> f = (i) =>
                {
                    // Console.WriteLine($"Processing chunk {i} of {noOfChunks}");
                    dictionaries[i] = new CustomByteDictionary<float>();
                    ProcessChunk(fileName, i * chunkSize, Math.Min((i + 1) * chunkSize, fileSize + 1), chunkSize, (i == noOfChunks - 1 ? 0 : overlapChunkSize), dictionaries[i]);
                };

#if SINGLE_THREAD
                for (long i = 0; i < noOfChunks; i++)
                {
                    f(i);
                }
#else
                // Parallel.For(0, noOfChunks, f);

                // every 2nd to check if overlapping is the problem.
                Parallel.For(0, noOfChunks/2, i => f(i*2));
                Parallel.For(0, noOfChunks/2, i => f(i*2+1));
#endif

                var finalDictionary = CombineDictionaries(dictionaries);

                PrintResult(finalDictionary);

                var endtime = DateTime.Now;
                Console.WriteLine($"Time taken for {recordCount} entries: {Stopwatch.GetElapsedTime(start)}");
            }
        }

        private static void PrintResult(Dictionary<ReadOnlyMemory<byte>, float> finalDictionary)
        {
            // Convert keys to strings, sort, and print
            var sorted = finalDictionary.Select(kvp => new { Name = Encoding.UTF8.GetString(kvp.Key.ToArray()), Sum = kvp.Value })
                .OrderBy(x => x.Name);


            using (StreamWriter writer = new StreamWriter("result.txt"))
            {
                writer.WriteLine("Result:");
                int maxResultsShown = 1000;
                foreach (var item in sorted)
                {
                    if (maxResultsShown-- == 0) break;
                    writer.WriteLine($"{item.Name}: {item.Sum}");
                }
            }
        }

        private static Dictionary<ReadOnlyMemory<byte>, float> CombineDictionaries(CustomByteDictionary<float>[] dictionaries)
        {
            var finalDictionary = new Dictionary<ReadOnlyMemory<byte>, float>(new ReadOnlyMemoryComparer());

            foreach (var dictionary in dictionaries)
            {
                foreach (var kvp in dictionary)
                {
                    if (finalDictionary.TryGetValue(kvp.Key, out float currentValue))
                    {
                        finalDictionary[kvp.Key] = currentValue + kvp.Value;
                    }
                    else
                    {
                        finalDictionary.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return finalDictionary;
        }

        static void ProcessChunk(string nameOfFile, long start, long end, long chunkSize, long overlapChunkSize, CustomByteDictionary<float> dictionary)
        {
            using var fs = new FileStream(nameOfFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Position = start;
            using var sr = new StreamReader(fs);
            ProcessChunkInternal(sr, start, end, chunkSize, dictionary);
        }

        private static void ProcessChunkInternal(StreamReader sr, long start, long end, long chunkSize, CustomByteDictionary<float> dictionary)
        {
            string? line;
            long bytesRead = 0;
            if (start != 0)
            {
                // Skip the first line if not the first chunk
                sr.ReadLine();
            }

            while ((line = sr.ReadLine()) != null && bytesRead <= chunkSize)
            {
                // Find the separator (semicolon) position
                if (line.Length > 0)
                {
                    int separatorPos = line.IndexOf(';');
                    if (separatorPos != -1)
                    {
                        ReadOnlySpan<byte> keySpan = Encoding.UTF8.GetBytes(line.Substring(0, separatorPos));
                        ReadOnlySpan<byte> valueSpan = Encoding.UTF8.GetBytes(line.Substring(separatorPos + 1));

                        if (float.TryParse(Encoding.UTF8.GetString(valueSpan), out float value))
                        {
                            if (dictionary.TryGetValue(keySpan, out float currentValue))
                            {
                                dictionary[keySpan] = currentValue + value;
                            }
                            else
                            {
                                dictionary.AddOrUpdate(keySpan, value);
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid line format, didn't find ';' in '{line}'");
                    }
                }

                bytesRead += Encoding.UTF8.GetByteCount(line) + 2; // +2 for \r\n
            }
        }
    }
}
