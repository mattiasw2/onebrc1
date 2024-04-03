namespace onebrc1
{
    public class CustomByteDictionary<T> : IEnumerable<KeyValuePair<ReadOnlyMemory<byte>, T>>
    {

        public IEnumerator<KeyValuePair<ReadOnlyMemory<byte>, T>> GetEnumerator()
        {
            for (int i = 0; i < _buckets.Length; i++)
            {
                Entry? current = _buckets[i];
                while (current != null)
                {
                    yield return new KeyValuePair<ReadOnlyMemory<byte>, T>(current.Key, current.Value);
                    current = current.Next;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        private class Entry
        {
            public required byte[] Key;
            public required T Value;
            public Entry? Next; // For collision resolution via chaining
        }

        private readonly int _capacity;
        private readonly Entry[] _buckets;

        public CustomByteDictionary(int capacity = 2048)
        {
            _capacity = capacity;
            _buckets = new Entry[capacity];
        }

        private int GetBucketIndex(ReadOnlySpan<byte> keySpan)
        {
            unchecked
            {
                int hash = 17;
                foreach (byte b in keySpan)
                {
                    hash = hash * 31 + b;
                }
                return Math.Abs(hash % _capacity);

                // GetDjb2HashCode
                //int hash = 5381;
                //foreach (byte b in keySpan)
                //{
                //    hash = ((hash << 5) + hash) + b; /* hash * 33 + b */
                //}
                //return Math.Abs(hash % _capacity);
            }
        }

        private bool KeyEquals(byte[] array, ReadOnlySpan<byte> span)
        {
            if (array.Length != span.Length) return false;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != span[i]) return false;
            }
            return true;
        }

        public bool TryGetValue(ReadOnlySpan<byte> keySpan, out T? value)
        {
            int bucketIndex = GetBucketIndex(keySpan);
            Entry? current = _buckets[bucketIndex];
            while (current != null)
            {
                if (KeyEquals(current.Key, keySpan))
                {
                    value = current.Value;
                    return true;
                }
                current = current.Next;
            }
            value = default;
            return false;
        }

        public void AddOrUpdate(ReadOnlySpan<byte> keySpan, T value)
        {
            int bucketIndex = GetBucketIndex(keySpan);
            Entry? current = _buckets[bucketIndex];

            while (current != null)
            {
                if (KeyEquals(current.Key, keySpan))
                {
                    current.Value = value; // Update existing
                    return;
                }
                current = current.Next;
            }

            // New key, allocate and add
            byte[] newKey = keySpan.ToArray();
            Entry newEntry = new Entry { Key = newKey, Value = value, Next = _buckets[bucketIndex] };
            _buckets[bucketIndex] = newEntry;
        }


        public T? this[ReadOnlySpan<byte> key]
        {
            get
            {
                if (TryGetValue(key, out T? value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException("The given key was not present in the dictionary.");
                }
            }
            set
            {
                AddOrUpdate(key, value!);
            }
        }

    }

}
