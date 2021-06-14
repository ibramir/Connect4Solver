namespace Connect4Solver.Model
{
    /**
     * TranspositionTable is a simple hash map with a fixed storage size.
     * In case of collisions we keep the last entry and override the previous one.
     * 
     * We use 32-bit partial keys (least significant 32 bits) of the 49 bits key
     * and 8-bit non-null values
     * */
    public class TranspositionTable
    {
        private struct Entry
        {
            internal uint Key; // 32 bits integer
            internal byte Val; // 8 bits for the score
        }

        private readonly Entry[] _entries;

        internal TranspositionTable(int size)
        {
            _entries = new Entry[size];
        }

        private int Index(ulong key)
        {
            return (int) (key % (ulong) _entries.Length);
        }

        internal void Reset()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                _entries[i].Key = 0;
                _entries[i].Val = 0;
            }
        }

        /**
         * Store a value for a given key
         * @param key: 64-bit key
         * @param value: non-null 8-bit value
         */
        internal void Put(ulong key, byte val)
        {
            int i = Index(key);
            _entries[i].Key = (uint) key;
            _entries[i].Val = val;
        }

        /**
         * Get the value of a key
         * @param key: 64-bit key
         * @return 
         */
        internal byte  Get(ulong key)
        {
            int i = Index(key);
            return (byte) (_entries[i].Key == (uint) key ? _entries[i].Val : 0);
        }
    }
}