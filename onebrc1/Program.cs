namespace onebrc1
{

    using System;
    using System.Collections.Generic;
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
                ("c:/data6/cs/onebrc1/onebrc1/measurements_1000000000.txt", 1000000000)
            };

            foreach (var (fileName, recordCount) in fileNames)
            {
                var start = DateTime.Now;

                long fileSize = new FileInfo(fileName).Length;
                long noOfChunks = 28;
                long chunkSize = fileSize / noOfChunks + 1;
                long overlapChunkSize = Math.Min(1000, fileSize / noOfChunks);

                var dictionaries = new CustomByteDictionary<float>[noOfChunks];

                Parallel.For(0, noOfChunks, i =>
                {
                    // Console.WriteLine($"Processing chunk {i} of {noOfChunks}");
                    dictionaries[i] = new CustomByteDictionary<float>();
                    ProcessChunk(fileName, i * chunkSize, Math.Min((i + 1) * chunkSize, fileSize + 1), chunkSize, (i == noOfChunks - 1 ? 0 : overlapChunkSize), dictionaries[i]);
                });

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

                var endtime = DateTime.Now;
                Console.WriteLine($"Time taken for {recordCount} entries: {endtime - start}");
            }
        }


        static void ProcessChunk(string nameOfFile, long start, long end, long chunkSize, long overlapChunkSize, CustomByteDictionary<float> dictionary)
        {
            // Adjust start and end to make sure we're not starting or ending in the middle of a record
            // This adjustment is not shown here but would involve seeking to the nearest newline character

            using var fs = new FileStream(nameOfFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);
            using var accessor = mmf.CreateViewAccessor(start, end - start - 1 + overlapChunkSize, MemoryMappedFileAccess.Read);


            //byte[] buffer = new byte[end - start];
            //accessor.ReadArray(0, buffer, 0, buffer.Length);
            //ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(buffer);
            //ReadOnlySpan<byte> span = memory.Span;

            // Unsafe code might be required to directly create a Span<byte> from a MemoryMappedViewAccessor
            unsafe
            {
                byte* ptr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                var span = new Span<byte>(ptr, (int)accessor.Capacity);
                // Process the span directly...



                int lineStart = 0;

                if (start != 0)
                {
                    lineStart = span.IndexOf((byte)'\n') + 1; // Skip the first partial line incl \n
                }

                int length = span.Length;
                for (int i = lineStart; i < length; i++)
                {
                    // Find the end of the line, we assume file has ending \n


                    if (span[i] == (byte)'\n')
                    {
                        // Process the line
                        int spanLength = i - lineStart;
                        if (spanLength == 0) break; // Empty line, we're done

                        ReadOnlySpan<byte> line = span.Slice(lineStart, spanLength);

                        // Find the separator (semicolon) position
                        int separatorPos = line.IndexOf((byte)';');
                        if (separatorPos != -1)
                        {
                            ReadOnlySpan<byte> keySpan = line.Slice(0, separatorPos);
                            ReadOnlySpan<byte> valueSpan = line.Slice(separatorPos + 1, line.Length - separatorPos - 1); // Exclude '\n'

                            // Console.WriteLine($"keySpan: {Encoding.UTF8.GetString(keySpan.ToArray())}");
                            // Console.WriteLine($"valueSpan: {Encoding.UTF8.GetString(valueSpan.ToArray())}");

                            if (float.TryParse(Encoding.UTF8.GetString(valueSpan), out float value))
                            {
                                // todo: possible optimization, only copy memory if key is not already in dictionary

                                // ReadOnlyMemory<byte> keyMemory = span.Slice(lineStart, separatorPos);
                                byte[] keyMemory = keySpan.ToArray();

                                if (dictionary.TryGetValue(keyMemory, out float currentValue))
                                {
                                    dictionary[keyMemory] = currentValue + value;
                                }
                                else
                                {
                                    dictionary.AddOrUpdate(keyMemory, value);
                                }
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Invalid line format, didn't find ';' in '{Encoding.UTF8.GetString(line.ToArray())}'");
                        }

                        lineStart = i + 1; // Start of the next line

                        if (i >= chunkSize)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
