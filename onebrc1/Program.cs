// #define SINGLE_THREAD

namespace onebrc1
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    class Program
    {
        private static int bufferSize;


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

                var dictionaries = new CustomByteDictionary<Station>[noOfChunks];

                Action<long> f = (i) =>
                {
                    // Console.WriteLine($"Processing chunk {i} of {noOfChunks}");
                    dictionaries[i] = new CustomByteDictionary<Station>();
                    ProcessChunk(fileName, i * chunkSize, Math.Min((i + 1) * chunkSize, fileSize + 1), 
                        chunkSize, (i == noOfChunks - 1 ? 0 : overlapChunkSize), dictionaries[i]);
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

        private static void PrintResult(Dictionary<ReadOnlyMemory<byte>, Station> finalDictionary)
        {
            // Convert keys to strings, sort, and print
            var sorted = finalDictionary.Select(kvp => new { Name = kvp.Value._name, Sum = kvp.Value })
                .OrderBy(x => x.Name);


            using (StreamWriter writer = new StreamWriter("result.txt"))
            {
                writer.WriteLine("Result:");
                int maxResultsShown = 1000;
                foreach (var item in sorted)
                {
                    if (maxResultsShown-- == 0) break;
                    writer.WriteLine(item.Sum.ToString());
                }
            }
        }

        private static Dictionary<ReadOnlyMemory<byte>, Station> CombineDictionaries(CustomByteDictionary<Station>[] dictionaries)
        {
            var finalDictionary = new Dictionary<ReadOnlyMemory<byte>, Station>(new ReadOnlyMemoryComparer());

            foreach (var dictionary in dictionaries)
            {
                foreach (var kvp in dictionary)
                {
                    if (finalDictionary.TryGetValue(kvp.Key, out Station currentValue))
                    {
                        currentValue.Combine(kvp.Value);
                    }
                    else
                    {
                        finalDictionary.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return finalDictionary;
        }

        static void ProcessChunk(string nameOfFile, long start, long end, long chunkSize, long overlapChunkSize, 
            CustomByteDictionary<Station> dictionary)
        {
            using var fs = File.OpenRead(nameOfFile); //  new FileStream(nameOfFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            ProcessChunkInternal(fs, start, end, chunkSize, dictionary);
        }

        private static void ProcessChunkInternal(Stream bs, long start, long end, long chunkSize, 
            CustomByteDictionary<Station> dictionary)
        {
            bufferSize = 65536;  // 1024;
            byte[] buffer = new byte[bufferSize + 100]; // Adjust the size as needed, +100 for maximum length of a row
            int bytesRead;
            int lineStart = 0;

            bool skipFirstRow = start != 0;
            long totalBytesRead = 0;

            bs.Position = start; // Set the position to start

            while ((bytesRead = bs.Read(buffer, lineStart, Math.Min(buffer.Length - lineStart, (int)(chunkSize - totalBytesRead)))) > 0)
            {
                bytesRead += lineStart;
                totalBytesRead += bytesRead;

                lineStart = 0;

                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == '\n')
                    {
                        if (skipFirstRow)
                        {
                            skipFirstRow = false;
                            lineStart = i + 1;
                        }
                        else
                        {
                            ReadOnlySpan<byte> line = new ReadOnlySpan<byte>(buffer, lineStart, i - lineStart);
                            ProcessLine(line, dictionary);
                            lineStart = i + 1;
                        }
                    }
                }

                // If we didn't find a newline, move the remaining bytes to the start of the buffer
                if (lineStart < bytesRead)
                {
                    Array.Copy(buffer, lineStart, buffer, 0, bytesRead - lineStart);
                }

                lineStart = bytesRead - lineStart;

                // Stop reading if we've reached the end of the chunk
                if (totalBytesRead >= chunkSize)
                {
                    break;
                }
            }
        }

        private static void ProcessLine(ReadOnlySpan<byte> line, CustomByteDictionary<Station> dictionary)
        {
            // Find the separator (semicolon) position
            int separatorPos = line.IndexOf((byte)';');
            if (separatorPos != -1)
            {
                ReadOnlySpan<byte> keySpan = line.Slice(0, separatorPos);
                ReadOnlySpan<byte> valueSpan = line.Slice(separatorPos + 1);

                if (float.TryParse(valueSpan, out float value))
                {
                    if (dictionary.TryGetValue(keySpan, out Station currentValue))
                    {
                        currentValue.Append(value);
                    }
                    else
                    {
                        dictionary.AddOrUpdate(keySpan, new Station(keySpan, value));
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"Invalid line format, didn't find ';' in '{Encoding.UTF8.GetString(line)}'");
            }
        }

    }
}
