using System.Diagnostics;

namespace Connect4Solver.Model
{
    public class Board
    {
        /**
        * A class storing a Connect 4 board position
        * Functions are relative to the current player whose turn is to play
        * Boards containing winning alignments are not supported by this class
        *
        * A binary bitboard representation is used.
        * Each column is encoded in H+1 bits.
        *
        * Example of bit order to encode for a 7x6 board
        * .  .  .  .  .  .  .
        * 5 12 19 26 33 40 47
        * 4 11 18 25 32 39 46
        * 3 10 17 24 31 38 45
        * 2  9 16 23 30 37 44
        * 1  8 15 22 29 36 43
        * 0  7 14 21 28 35 42
        *
        * Position is stored as
        * - a bitboard "mask" with 1 on any stone
        * - a bitboard "position" with 1 on the stones of the current player
        *
        * These two bitboard can be transformed into a compact and non ambiguous key
        * by adding an extra bit on top of the last non empty cell of each column.
        * This allows us to identify all the empty cells without needing the "mask" bitboard
        *
        * current player "x" = 1, opponent "o" = 0
        * board     position  mask      key       bottom
        *           0000000   0000000   0000000   0000000
        * .......   0000000   0000000   0001000   0000000
        * ...o...   0000000   0001000   0010000   0000000
        * ..xx...   0011000   0011000   0011000   0000000
        * ..ox...   0001000   0011000   0001100   0000000
        * ..oox..   0000100   0011100   0000110   0000000
        * ..oxxo.   0001100   0011110   1101101   1111111
        *
        * current player "o" = 1, opponent "x" = 0
        * board     position  mask      key       bottom
        *           0000000   0000000   0001000   0000000
        * ...x...   0000000   0001000   0000000   0000000
        * ...o...   0001000   0001000   0011000   0000000
        * ..xx...   0000000   0011000   0000000   0000000
        * ..ox...   0010000   0011000   0010100   0000000
        * ..oox..   0011000   0011100   0011010   0000000
        * ..oxxo.   0010010   0011110   1110011   1111111
        *
        * Key is an unique representation of a board key = position + mask + bottom
        * In practice, as bottom is constant, key = position + mask is also a
        * non-ambiguous representation of the position.
        */
        
        internal const int W = 7, H = 6; // Width and height of the board
        internal const int MinScore = -(W * H) / 2 + 3;
        internal const int MaxScore = (W * H + 1) / 2 - 3;

        /*
        * Generate a bitmask containing one for the bottom slot of each column
        */
        private static ulong Bottom(int width, int height)
        {
            return width == 0 ? 0 : Bottom(width - 1, height) | 1UL << (width - 1) * (height + 1);
        }

        // Static bitmaps
        private static readonly ulong BottomMask = Bottom(W, H);
        private static readonly ulong BoardMask = BottomMask * ((1UL << H) - 1);

        // Return a bitmask containing a single 1 corresponding to the top cell of a given column
        private static ulong TopMaskCol(int col)
        {
            return 1UL << (H - 1 + col * (H + 1));
        }

        // Return a bitmask containing a single 1 corresponding to the bottom cell of a given column
        private static ulong BottomMaskCol(int col)
        {
            return 1UL << col * (H + 1);
        }

        // Return a bitmask with 1 on all the cells of a given column
        internal static ulong ColumnMask(int col)
        {
            return ((1UL << H) - 1) << col * (H + 1);
        }

        // Number of moves played since the start of the game
        internal byte PlayedMoves { get; private set; }

        private ulong _position; // Bitmap of the current player's stones
        private ulong _mask; // Bitmap of all the played stones

        // Copy constructor
        internal Board(Board b)
        {
            _position = b._position;
            _mask = b._mask;
            PlayedMoves = b.PlayedMoves;
        }

        public Board()
        {
        }

        /** 
         * Indicates whether a column is playable
         * @param col: 0-based index of the column to play
         * @return true if the column is playable, false if the column is full
         */
        public bool CanPlay(int col)
        {
            return (_mask & TopMaskCol(col)) == 0;
        }

        /**
         * Plays a playable column
         * This function should not be called on a non-playable column or a column making a winning alignment
         *
         * @param col: 0-based index of a playable column
         */
        public void Play(int col)
        {
            _position ^= _mask;
            _mask |= _mask + BottomMaskCol(col);
            PlayedMoves++;
        }

        /**
         * Plays a possible move given by its bitmap representation
         *
         * @param move: a possible move given by its bitmap representation
         *        only one bit of the bitmap should be set to 1
         *        the move should be a valid possible move for the current player
         */
        internal void Play(ulong move)
        {
            _position ^= _mask;
            _mask |= move;
            PlayedMoves++;
        }

        public bool IsDraw()
        {
            return _mask == BoardMask;
        }

        /**
         * @return a compact representation of a board position in W*(H+1) bits
         */
        internal ulong Key()
        {
            return _position + _mask;
        }

        /**
         * @return the key of the mirror position of the current board that would have the same score
         */
        internal ulong MirrorKey()
        {
            ulong n = Key();
            ulong rev = 0;
            for (byte i = 0; i < W; i++)
            {
                ulong col = ((1UL << (H + 1)) - 1) << (i * (H + 1)) & n;
                if (i <= W / 2)
                    rev |= col << ((H + 1) * (W - 1 - 2 * i));
                else
                    rev |= col >> ((H + 1) * -(W - 1 - 2 * i));
            }
            return rev;
        }
        
        /**
         * Score a possible move
         *
         * @param move, a possible move given in a bitmap format
         *
         * The score we are using is the number of winning spots
         * the current player has after playing the move
         */
        internal byte MoveScore(ulong move)
        {
            return PopCount(ComputeWinningPosition(_position | move, _mask));
        }

        /*
         * @return true if current player can win next move
         */
        internal bool CanWinNext()
        {
            return (WinningPosition() & Possible()) != 0;
        }
        
        /*
         * Return a bitmap of all the possible next moves that do not lose in one turn
         * A losing move is a move leaving the possibility for the opponent to win directly
         *
         * This function is intended to test positions where you cannot win in one turn
         * If you have a winning move, this function can miss it and prefer to prevent the opponent
         * from making an alignment.
         *
         * @return bitmap of moves
         */
        internal ulong PossibleNonLoosingMoves() {
            Debug.Assert(!CanWinNext());
            ulong possibleMask = Possible();
            ulong opponentWin = OpponentWinningPosition();
            ulong forcedMoves = possibleMask & opponentWin;
            if(forcedMoves != 0) {
                if((forcedMoves & (forcedMoves - 1)) != 0) // Check if there is more than one forced move
                    return 0;                           // The opponent has two winning moves and you cannot stop him
                possibleMask = forcedMoves;    // Enforce to play the single forced move
            }
            return possibleMask & ~(opponentWin >> 1);  // Avoid to play below an opponent winning spot
        }

        /**
         * Indicates whether the current player wins by playing a given column
         * This function should never be called on a non-playable column
         * 
         * @param col: 0-based index of a playable column.
         * @return true if current player makes a winning alignment by playing the corresponding column col
         */
        internal bool IsWinningMove(int col)
        {
            return (WinningPosition() & Possible() & ColumnMask(col)) != 0;
        }
        
        /*
         * Counts the number of bits set to 1 in a 64 bits integer
         */
        private static byte PopCount(ulong m)
        {
            byte c;
            for (c = 0; m != 0; c++)
                m &= m - 1;
            return c;
        }

        /*
         * Bitmap of the next possible valid moves for the current player including losing moves
         */
        private ulong Possible()
        {
            return (_mask + BottomMask) & BoardMask;
        }

        /*
         * Return a bitmask of the possible winning positions for the current player
         */
        private ulong WinningPosition()
        {
            return ComputeWinningPosition(_position, _mask);
        }

        /*
         * Return a bitmask of the possible winning positions for the opponent
         */
        private ulong OpponentWinningPosition()
        {
            return ComputeWinningPosition(_position ^ _mask, _mask);
        }

        /*
         * @param position, a bitmap of the player to evaluate the winning pos
         * @param mask, a mask of the already played spots
         *
         * @return a bitmap of all the winning free spots making an alignment
         */
        private static ulong ComputeWinningPosition(ulong position, ulong mask)
        {
            // vertical
            ulong r = (position << 1) & (position << 2) & (position << 3);

            //horizontal
            ulong p = (position << (H + 1)) & (position << 2 * (H + 1));
            r |= p & (position << 3 * (H + 1));
            r |= p & (position >> (H + 1));
            p = (position >> (H + 1)) & (position >> 2 * (H + 1));
            r |= p & (position << (H + 1));
            r |= p & (position >> 3 * (H + 1));

            //diagonal 1
            p = (position << H) & (position << 2 * H);
            r |= p & (position << 3 * H);
            r |= p & (position >> H);
            p = (position >> H) & (position >> 2 * H);
            r |= p & (position << H);
            r |= p & (position >> 3 * H);

            //diagonal 2
            p = (position << (H + 2)) & (position << 2 * (H + 2));
            r |= p & (position << 3 * (H + 2));
            r |= p & (position >> (H + 2));
            p = (position >> (H + 2)) & (position >> 2 * (H + 2));
            r |= p & (position << (H + 2));
            r |= p & (position >> 3 * (H + 2));

            return r & (BoardMask ^ mask);
        }
    }
}