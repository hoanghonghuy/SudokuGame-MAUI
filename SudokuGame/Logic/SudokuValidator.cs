using System.Collections.Generic;

namespace SudokuGame.Logic
{
    public class SudokuValidator
    {
        /// <summary>
        /// Kiểm tra xem toàn bộ bàn cờ có hợp lệ hay không.
        /// </summary>
        /// <param name="board">Mảng 2 chiều chứa trạng thái hiện tại của bàn cờ.</param>
        /// <returns>True nếu hợp lệ, False nếu có lỗi.</returns>
        public bool IsBoardValid(int[,] board)
        {
            // Kiểm tra từng ô một
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int number = board[row, col];
                    // Nếu ô có số, kiểm tra tính hợp lệ của nó
                    if (number != 0 && !IsMoveValid(board, number, row, col))
                    {
                        return false; // Phát hiện lỗi, trả về false ngay lập tức
                    }
                }
            }
            return true; // Nếu mọi thứ đều ổn
        }

        /// <summary>
        /// (NÂNG CẤP) Trả về danh sách các ô do người dùng điền nhưng không hợp lệ.
        /// </summary>
        /// <param name="currentBoard">Bàn cờ hiện tại của người chơi.</param>
        /// <param name="initialPuzzle">Đề bài gốc để xác định ô nào do người dùng điền.</param>
        /// <returns>Một danh sách các tuple (hàng, cột) của các ô không hợp lệ.</returns>
        public List<(int row, int col)> GetInvalidCells(int[,] currentBoard, int[,] initialPuzzle)
        {
            var invalidCells = new List<(int, int)>();
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    // Chỉ kiểm tra các ô do người chơi điền (ô trống trong đề gốc) và có số
                    if (initialPuzzle[r, c] == 0 && currentBoard[r, c] != 0)
                    {
                        if (!IsMoveValid(currentBoard, currentBoard[r, c], r, c))
                        {
                            invalidCells.Add((r, c));
                        }
                    }
                }
            }
            return invalidCells;
        }


        /// <summary>
        /// Kiểm tra xem việc đặt một số vào một ô có hợp lệ không.
        /// (THAY ĐỔI) Chuyển thành public để có thể tái sử dụng.
        /// </summary>
        public bool IsMoveValid(int[,] board, int number, int row, int col)
        {
            // Kiểm tra hàng và cột
            for (int i = 0; i < 9; i++)
            {
                // Kiểm tra xem ở vị trí khác trên cùng hàng, có số nào trùng không
                if (board[row, i] == number && i != col) return false;
                // Kiểm tra xem ở vị trí khác trên cùng cột, có số nào trùng không
                if (board[i, col] == number && i != row) return false;
            }

            // Kiểm tra trong khối 3x3
            int startRow = row - row % 3;
            int startCol = col - col % 3;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    // Kiểm tra xem ở vị trí khác trong cùng khối 3x3, có số nào trùng không
                    if (board[i + startRow, j + startCol] == number && (i + startRow != row || j + startCol != col))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra xem bàn cờ đã được điền đầy đủ hay chưa.
        /// </summary>
        public bool IsBoardComplete(int[,] board)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (board[row, col] == 0)
                    {
                        return false; // Vẫn còn ô trống
                    }
                }
            }
            return true; // Đã điền đầy đủ
        }
    }
}