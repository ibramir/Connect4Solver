namespace Connect4Solver.Model
{
    public class Solver
    {
        private readonly int[] _columnOrder;
        private readonly TranspositionTable _table;

        public Solver()
        {
            _columnOrder = new int[Board.W];
            // A prime number, about 256 MB of entries
            _table = new TranspositionTable(33554371);

            Reset();
            for (int i = 0; i < Board.W; i++)
            {
                _columnOrder[i] = Board.W / 2 + (1 - 2 * (i % 2)) * (i + 1) / 2;
            }
        }

        public void Reset()
        {
            _table.Reset();
        }

        private int Solve(Board board, bool weak = false)
        {
            if (board.CanWinNext()) // Check if there's a win in one move as the NegaMax function does not support this case
                return (Board.W * Board.H + 1 - board.PlayedMoves) / 2;
            int min, max;
            if (weak)
            {
                min = -1;
                max = 1;
            }
            else
            {
                int numStones = Board.W * Board.H;
                min = -(numStones - board.PlayedMoves) / 2;
                max = (numStones + 1 - board.PlayedMoves) / 2;
            }

            while (min < max) // Iteratively narrow the min-max exploration window
            {
                int med = min + (max - min) / 2;
                if (med <= 0 && min / 2 < med)
                    med = min / 2;
                else if (med >= 0 && max / 2 > med)
                    med = max / 2;
                int r = NegaMax(board, med, med + 1); // Use a null depth window to know if the actual score is greater or smaller than med
                if (r <= med)
                    max = r;
                else
                    min = r;
            }

            return min;
        }

        public int?[] Analyze(Board board, bool weak = false)
        {
            int?[] ret = new int?[Board.W];
            for (int i = 0; i < Board.W; i++)
            {
                if (board.CanPlay(i))
                {
                    if (board.IsWinningMove(i))
                        ret[i] = (Board.W * Board.H + 1 - board.PlayedMoves) / 2;
                    else
                    {
                        Board b = new Board(board);
                        b.Play(i);
                        ret[i] = -Solve(b, weak);
                    }
                }
                else
                    ret[i] = null;
            }

            return ret;
        }

        /**
         * Recursively score Connect 4 state using negamax variant of alpha-beta minmax algorithm.
         * @param: State to evaluate. This function assumes nobody already won and
         *         current player cannot win with one move. This has to be checked before calling.
         * @param: (alpha, beta) A score window within which we are evaluating the position.
         *
         * @return The exact score, or an upper or lower bound score depending of the case:
         * - If the actual score of the state <= alpha, then the actual score <= return value <= alpha
         * - If the actual score of the state >= beta then beta <= return value <= actual score
         * - If alpha <= actual score <= beta then return value = actual score
         */
        private int NegaMax(Board board, int alpha, int beta)
        {
            const int totalStones = Board.H * Board.W;

            ulong possible = board.PossibleNonLoosingMoves();
            if (possible == 0) // If no possible non losing moves, the opponent wins next move
            {
                return -(totalStones - board.PlayedMoves) / 2;
            }

            if (board.PlayedMoves >= totalStones - 2) // Check for a draw
                return 0;

            // Lower bound of the score as the opponent cannot win next move
            int min = -(totalStones - 2 - board.PlayedMoves) / 2;
            if (alpha < min)
            {
                alpha = min; // There is no need to keep alpha below our min possible score
                if (alpha >= beta) return alpha; // Prune the exploration if the [alpha,beta] window is empty.
            }

            // Upper bound of our score as we cannot win immediately
            int max = (totalStones - 1 - board.PlayedMoves) / 2;
            if (beta > max)
            {
                beta = max; // There is no need to keep beta above our max possible score
                if (alpha >= beta) return beta; // Prune the exploration if the [alpha;beta] window is empty
            }
            
            int val;
            // Check for the current state or its mirror in the transposition table
            if ((val = _table.Get(board.Key())) != 0 || (val = _table.Get(board.MirrorKey())) != 0)
            {
                if (val > Board.MaxScore - Board.MinScore + 1) // We have a lower bound
                {
                    min = val + 2 * Board.MinScore - Board.MaxScore - 2;
                    if (alpha < min)
                    {
                        alpha = min; // There is no need to keep alpha below our min possible score
                        if (alpha >= beta) return alpha; // Prune the exploration if the [alpha;beta] window is empty
                    }
                }
                else // We have an upper bound
                {
                    max = val + Board.MinScore - 1;
                    if (beta > max)
                    {
                        beta = max; // There is no need to keep beta above our max possible score
                        if (alpha >= beta) return beta; // Prune the exploration if the [alpha;beta] window is empty.
                    }
                }
            }

            MoveSorter moves = new MoveSorter();
            for (int i = Board.W; i-- != 0;)
            {
                ulong move;
                if ((move = possible & Board.ColumnMask(_columnOrder[i])) != 0)
                    moves.Add(move, board.MoveScore(move));
            }

            ulong next;
            while ((next = moves.GetNext()) != 0)
            {
                Board b = new Board(board);
                b.Play(next); // It's the opponent's turn in Board b after current player plays next column
                // Explore opponent's score within [-beta;-alpha] window
                int score = -NegaMax(b, -beta, -alpha);

                if (score >= beta)
                {
                    // Save the lower bound of the position
                    _table.Put(board.Key(), (byte) (score + Board.MaxScore - 2 * Board.MinScore + 2));
                    return score; // Prune the exploration if we find a possible move better than what we were looking for
                }

                if (score > alpha)
                    // Reduce the [alpha,beta] window for the next exploration as we only
                    // need to search for a position that is better than the best so far
                    alpha = score;
            }

            // Save the upper bound of the position
            _table.Put(board.Key(), (byte) (alpha - Board.MinScore + 1));
            return alpha;
        }
    }
}