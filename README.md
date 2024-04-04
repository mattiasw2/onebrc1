# 1brc using C#

A good example how to use span and memory mapped files in C#.

You need to create the measurement file first. More information at

https://github.com/gunnarmorling/1brc#faq


Todo: 
- [ ] Currently, I just sum up the values. I need to implement the calculation of the average value.
- [ ] Write the correct format of the output file
- [x] Experiment with a dictionary where I only have to place the dictionary keys on the heap once.
- [ ] Does memory mapped file improve performance? Parallel processing is possible without it, just seek to the starting position.

## Results

The execution time is 23.1 seconds on my 8 core Ryzen 7040 laptop under Windows 11. CPU load is 50%.

Remember that this cannot be compared with the Java implementation since the Java implementations since they use a ram disk. 
Most likely, the result will be twice as fast if I had used a ram disk.

## Other implementations is magnitudes faster

However, there is another C# that is much faster than mine, almost 10 times faster.

Completed in 00:00:03.2792809 at https://github.com/yurvon-screamo/1brc/tree/main

The key differences are:
- Does proper avg/sum calcuation
- Does proper output
- Uses int instead of decimal (where assume 1 decimal)
- hand-crafted number parsing
- uses plain streamreader instead of memory mapped files

### Why is it so slow?

float.TryParseFloat takes 5% of the cpu time, so not the reason.

ToArray takes 20%

FramedAllocateString takes 18%

40% is something else, maybe the merging and presenting of the dictionaries.

Or could it be that the Parallel.ForEach confuses the profiler?

Most likely, the memory mapped is one a time. Managing memory mapping might be 80% of the time according to proiler.

So, next step, remove memory mapped file and use a plain streamreader.

Tried to remove overlapping of memory mapped files, but it didn't make any difference.

### TryParseFloat is half of the time inside the program, but what are the remaining 70%?

```
  100%   All Calls  •  81,543 ms
    32.2%   Main  •  26,285 ms  •  onebrc1.Program.Main()
      32.2%   <Main>b__0  •  26,241 ms  •  onebrc1.Program+<>c__DisplayClass0_0.<Main>b__0(Int64)
        32.2%   ProcessChunk  •  26,241 ms  •  onebrc1.Program.ProcessChunk(String, Int64, Int64, Int64, Int64, CustomByteDictionary)
          32.2%   ProcessChunkInternal  •  26,235 ms  •  onebrc1.Program.ProcessChunkInternal(Int64, Int64, CustomByteDictionary, Span)
            13.7%   TryParseFloat  •  11,158 ms  •  System.Number.TryParseFloat(ReadOnlySpan, NumberStyles, NumberFormatInfo, out TFloat)
            1.66%   ToArray  •  1,353 ms  •  System.ReadOnlySpan`1.ToArray()
            1.11%   GetPointerToFirstInvalidByte  •  906 ms  •  System.Text.Unicode.Utf8Utility.GetPointerToFirstInvalidByte(Byte*, Int32, out Int32, out Int32)
            0.68%   NonPackedIndexOfValueType  •  558 ms  •  System.SpanHelpers.NonPackedIndexOfValueType(ref TValue, TValue, Int32)
            0.60%   TranscodeToUtf16  •  491 ms  •  System.Text.Unicode.Utf8Utility.TranscodeToUtf16(Byte*, Int32, Char*, Int32, out Byte*, out Char*)
            0.17%   [Garbage collection]  •  142 ms
            0.02%   TryParse  •  13 ms  •  System.Single.TryParse(String, out Single)
            <0.01%   GetString  •  6.4 ms  •  System.Text.Encoding.GetString(ReadOnlySpan)
          <0.01%   FileStream..ctor  •  6.0 ms  •  System.IO.FileStream..ctor(String, FileMode, FileAccess, FileShare)
      0.02%   get_Now  •  13 ms  •  System.DateTime.get_Now()
    ► 0.02%   PrintResult  •  13 ms  •  onebrc1.Program.PrintResult(Dictionary)
    ► 0.01%   CombineDictionaries  •  11 ms  •  onebrc1.Program.CombineDictionaries(CustomByteDictionary[])
      <0.01%   WriteLine  •  6.5 ms  •  System.Console.WriteLine(String)
```

### Test 1, using standard dictionary

Time taken for 1000000 entries: 00:00:00.1249419
Time taken for 250000000 entries: 00:00:06.1007100
Time taken for 1000000000 entries: 00:00:25.4956317

### Test 2, using a CustomByteDictionary with minumum heap allocation 

Only few percent faster.

Time taken for 1000000 entries: 00:00:00.0888413
Time taken for 250000000 entries: 00:00:05.8251759
Time taken for 1000000000 entries: 00:00:24.4375167

### Test 3, 32768 buckets in hash table

Time taken for 1000000 entries: 00:00:00.1076111
Time taken for 250000000 entries: 00:00:06.0816273
Time taken for 1000000000 entries: 00:00:23.8675923

### Test 4, 2048 buckets in hash table (there are only cities)

Time taken for 1000000 entries: 00:00:00.0952760
Time taken for 250000000 entries: 00:00:06.1811727
Time taken for 1000000000 entries: 00:00:23.5878592

### Test 5, DJDB hashing

Time taken for 1000000 entries: 00:00:00.0922354
Time taken for 250000000 entries: 00:00:05.8683649
Time taken for 1000000000 entries: 00:00:23.8294834

### Test 6, replacing decimal by float didn't make any difference

Time taken for 1000000 entries: 00:00:00.1170295
Time taken for 250000000 entries: 00:00:05.4428153
Time taken for 1000000000 entries: 00:00:24.3926523

## Is this really a good performance test? No!

It was fun to test, but since the processing is so small, the OS and the NVM disk are the bottlenecks. 
The amount of processing needed by the OS+disk is at least 10 times larger than it takes to parse and sum the values.

So, if you have a lousy result on this test, I wonder what programming language you are using.

