namespace Connect4Solver.Model
{
    internal class MoveSorter
    { 
        /*
         * This class sorts the next moves to play
         *
         * You have to add moves first with their score 
         * then you can get them back in decreasing score
         *
         * This class implements an insertion sort that is in practice very
         * efficient for a small number of moves to sort (max is Board.W)
         * and also efficient if the moves are pushed in approximately increasing 
         * order which can be achieved by using a simpler column ordering heuristic.
         * For the reasons stated above, this implementation is more efficient than a
         * max heap for this particular use case.
         */
        
        private struct Entry
        {
            internal ulong Move;
            internal byte Score;
        }

        // Contains moves with their score ordered by score
        private Entry[] _entries = new Entry[Board.W];
        // Number of stored moves
        private int _size;

        /*
         * Add a move in the container with its score
         * You cannot add more than Board.W moves
         */
        internal void Add(ulong move, byte score)
        {
            int pos = _size++;
            for (; pos != 0 && _entries[pos - 1].Score > score; --pos)
                _entries[pos] = _entries[pos - 1];
            _entries[pos].Move = move;
            _entries[pos].Score = score;
        }

        /*
         * Get the next move and remove it from the container
         *
         * @return next remaining move with the max score
         * If there are no more moves available, return 0
         */
        internal ulong GetNext()
        {
            if (_size != 0)
                return _entries[--_size].Move;
            return 0;
        }
    }
}