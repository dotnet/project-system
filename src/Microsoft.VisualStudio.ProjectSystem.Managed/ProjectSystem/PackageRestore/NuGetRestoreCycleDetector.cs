// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    // This algorithm is to detect the pattern A -> B -> A -> B -> A in the most recent N values.
    // To keep track of the most N recent values we are using a queue of fixed size, where:
    //   The most recent value is inserted at the front in O(1) time, and the oldest value is removed at the 
    // back in O(1) time.
    //   The counter will keep track of how many times values have appeared in the queue consecutively.
    // i.e.
    // A -> B -> C -> A -> B -> D -> X -> Y -> X -> Y -> X -> Y -> X
    //                                        |---cycle detected----|
    internal sealed class NuGetRestoreCycleDetector
    {
        // The fixed size of the queue
        private readonly int _size = 5;

        private readonly object _lock;
        private readonly Queue<byte[]> _values;
        private readonly Dictionary<byte[], int> _lookupTable;
        private int _counter;

        /// <summary>
        ///     Fixed size of the numbers of values to store.
        /// </summary>
        /// <remarks>
        ///     This represents how deep to search for hash cycle.
        /// </remarks>
        public int Size { get; private set; }

        public NuGetRestoreCycleDetector()
        {
            _lock = new object();
            _values = new Queue<byte[]>();
            _lookupTable = new Dictionary<byte[], int>();
            Size = _size;
        }

        /// <summary>
        ///     Validate if hash1 cycle exist. 
        ///     If no cycle exist, then the hashValue is stored.
        /// </summary>
        /// <param name="hashValue">This is hashValue that is used to verify if it exists in previous restores</param>
        /// <returns>True if it contains hash1 cycle, otherwise false</returns>
        public bool ComputeCycleDetection(byte[] hashValue)
        {
            lock (_lock)
            {
                if (QueueContainsValue(hashValue))
                {
                    _counter++;
                    
                    // Verify that a hashValue has repeated in almost all cases
                    if (_counter > Size)
                    {
                        Clear();
                        return true;
                    }
                }
                else
                {
                    _counter = 0;
                }

                Add(hashValue);
            }

            return false;

            bool QueueContainsValue(byte[] hashValue)
            {
                if (_lookupTable.TryGetValue(hashValue, out int hashCounter) && hashCounter > 0)
                {
                    return true;
                }
                return false;
            }
        }

        private void Add(byte[] newHashValue)
        {
            if (_values.Count >= Size)
            {
                var oldestHashValue = _values.Dequeue();
                _lookupTable[oldestHashValue]--;
            }

            _values.Enqueue(newHashValue);
            if (_lookupTable.ContainsKey(newHashValue))
            {
                _lookupTable[newHashValue]++;
            }
            else
            {
                _lookupTable.Add(newHashValue, 1);
            }
        }

        public void Clear()
        {
            _counter = 0;
            _values.Clear();
            _lookupTable.Clear();
        }
    }
}
