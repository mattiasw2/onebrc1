namespace onebrc1
{

    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ReadOnlyMemoryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            // Implement a hashing function that computes a hash code based on the byte content.
            // A simple approach (not highly optimized):
            ReadOnlySpan<byte> span = obj.Span;
            unchecked
            {
                int hash = 17;
                foreach (byte b in span)
                {
                    hash = hash * 31 + b;
                }
                return hash;
            }
        }
    }

}
