namespace SudokuGame.Logic
{
    public class SudokuSolver
    {
        // Sử dụng validator từ bên ngoài để tránh trùng lặp code
        private readonly SudokuValidator _validator = new SudokuValidator();

        /// <summary>
        /// Cố gắng giải bàn cờ Sudoku bằng thuật toán backtracking.
        /// </summary>
        /// <param name="board">Bàn cờ cần giải.</param>
        /// <returns>True nếu tìm thấy lời giải, False nếu không.</returns>
        public bool Solve(int[,] board)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    // Tìm một ô trống
                    if (board[row, col] == 0)
                    {
                        // Thử điền các số từ 1 đến 9
                        for (int num = 1; num <= 9; num++)
                        {
                            // (THAY ĐỔI) Gọi phương thức đã được public của validator
                            if (_validator.IsMoveValid(board, num, row, col))
                            {
                                // Nếu số này hợp lệ, điền vào và tiếp tục đệ quy
                                board[row, col] = num;

                                if (Solve(board))
                                {
                                    return true; // Đã tìm thấy lời giải
                                }
                                else
                                {
                                    // Nếu con đường này không dẫn đến lời giải, quay lui
                                    board[row, col] = 0;
                                }
                            }
                        }
                        return false; // Nếu thử hết 9 số mà không được, con đường này sai
                    }
                }
            }
            return true; // Nếu không còn ô trống, bàn cờ đã được giải
        }

        /// <summary>
        /// Tìm gợi ý cho một ô cụ thể.
        /// </summary>
        /// <param name="board">Bàn cờ hiện tại.</param>
        /// <param name="row">Hàng của ô cần gợi ý.</param>
        /// <param name="col">Cột của ô cần gợi ý.</param>
        /// <returns>Số gợi ý, hoặc null nếu không tìm thấy hoặc ô đã có số.</returns>
        public int? GetHintForCell(int[,] board, int row, int col)
        {
            if (board[row, col] != 0) return null; // Ô đã được điền

            // Tạo một bản sao của bàn cờ để không ảnh hưởng đến game của người chơi
            int[,] boardCopy = (int[,])board.Clone();

            if (Solve(boardCopy))
            {
                // Nếu bản sao có thể giải được, số ở vị trí đó chính là gợi ý
                return boardCopy[row, col];
            }

            return null; // Không tìm thấy lời giải (có thể do người chơi điền sai ở đâu đó)
        }

    }
}