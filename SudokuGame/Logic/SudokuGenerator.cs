#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace SudokuGame.Logic
{
    public class SudokuGenerator
    {
        private readonly Random _random;
        private readonly SudokuSolver _solver;

        public SudokuGenerator()
        {
            _random = new Random();
            _solver = new SudokuSolver();
        }

        // Constructor cho Daily Challenge
        public SudokuGenerator(int seed)
        {
            _random = new Random(seed); // Dùng seed để kết quả luôn giống nhau
            _solver = new SudokuSolver();
        }

        /// <summary>
        /// Lấy ngẫu nhiên một đề bài được tạo ra theo độ khó.
        /// </summary>
        public int[,] GenerateRandomPuzzle(Difficulty difficulty)
        {
            var board = new int[9, 9];

            // Để tạo sự ngẫu nhiên, chúng ta cần giải một bàn cờ trống
            // nhưng với một bộ số đã được xáo trộn
            FillDiagonalBlocks(board);
            _solver.Solve(board);

            RemoveNumbers(board, difficulty);

            return board;
        }

        // Giúp tạo ra các bàn cờ đã giải đa dạng hơn
        private void FillDiagonalBlocks(int[,] board)
        {
            for (int i = 0; i < 9; i += 3)
            {
                FillBlock(board, i, i);
            }
        }

        private void FillBlock(int[,] board, int row, int col)
        {
            var values = Enumerable.Range(1, 9).ToList();
            Shuffle(values);
            int index = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    board[row + i, col + j] = values[index++];
                }
            }
        }

        private void RemoveNumbers(int[,] board, Difficulty difficulty)
        {
            int cellsToRemove = difficulty switch
            {
                Difficulty.Medium => 50,
                Difficulty.Hard => 58,
                _ => 42, // Easy
            };

            var cells = new List<(int r, int c)>();
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    cells.Add((r, c));
                }
            }

            Shuffle(cells);

            int cellsRemoved = 0;
            foreach (var cell in cells)
            {
                if (cellsRemoved >= cellsToRemove) break;
                board[cell.r, cell.c] = 0;
                cellsRemoved++;
            }
        }

        private void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}