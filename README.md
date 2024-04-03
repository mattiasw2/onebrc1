# 1brc using C#

A good example how to use span and memory mapped files in C#.

You need to create the measurement file first. More information at

https://github.com/gunnarmorling/1brc#faq


Todo: 
- [ ] Currently, I just sum up the values. I need to implement the calculation of the average value.
- [ ] Write the correct format of the output file
- [ ] Experiment with a dictionary where I only have to place the dictionary keys on the heap once.
- [ ] Does memory mapped file improve performance? Parallel processing is possible without it, just seek to the starting position.

## Results

The execution time is 23.1 seconds on my 8 core Ryzen 7040 laptop under Windows 11. CPU load is 50%.

Remember that this cannot be compared with the Java implementation since the Java implementations since they use a ram disk. 
Most likely, the result will be twice as fast if I had used a ram disk.

## Is this really a good performance test? No!

It was fun to test, but since the processing is so small, the OS and the NVM disk are the bottlenecks. 
The amount of processing needed by the OS+disk at least 10 times larger than it takes to parse and sum the values.

So, if you have a lousy result on this test, I wonder what programming language you are using.

