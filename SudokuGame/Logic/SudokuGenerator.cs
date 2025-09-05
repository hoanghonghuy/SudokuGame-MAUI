#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace SudokuGame.Logic
{
    public class SudokuGenerator
    {
        // dùng Dictionary để lưu nhiều list theo từng độ khó
        private readonly Dictionary<Difficulty, List<int[,]>> _puzzles;
        private readonly Random _random;

        public SudokuGenerator()
        {
            _random = new Random();
            _puzzles = new Dictionary<Difficulty, List<int[,]>>
            {
                // === Danh sách các đề bài DỄ ===
                [Difficulty.Easy] = new List<int[,]>
                {
                    new int[9, 9] // Đề Dễ #1
                    {
                        { 5, 3, 0, 0, 7, 0, 0, 0, 0 },
                        { 6, 0, 0, 1, 9, 5, 0, 0, 0 },
                        { 0, 9, 8, 0, 0, 0, 0, 6, 0 },
                        { 8, 0, 0, 0, 6, 0, 0, 0, 3 },
                        { 4, 0, 0, 8, 0, 3, 0, 0, 1 },
                        { 7, 0, 0, 0, 2, 0, 0, 0, 6 },
                        { 0, 6, 0, 0, 0, 0, 2, 8, 0 },
                        { 0, 0, 0, 4, 1, 9, 0, 0, 5 },
                        { 0, 0, 0, 0, 8, 0, 0, 7, 9 }
                    },
                },

                // === Danh sách các đề bài TRUNG BÌNH ===
                [Difficulty.Medium] = new List<int[,]>
                {
                    new int[9, 9] // Đề Trung Bình #1
                    {
                        { 0, 0, 0, 2, 6, 0, 7, 0, 1 },
                        { 6, 8, 0, 0, 7, 0, 0, 9, 0 },
                        { 1, 9, 0, 0, 0, 4, 5, 0, 0 },
                        { 8, 2, 0, 1, 0, 0, 0, 4, 0 },
                        { 0, 0, 4, 6, 0, 2, 9, 0, 0 },
                        { 0, 5, 0, 0, 0, 3, 0, 2, 8 },
                        { 0, 0, 9, 3, 0, 0, 0, 7, 4 },
                        { 0, 4, 0, 0, 5, 0, 0, 3, 6 },
                        { 7, 0, 3, 0, 1, 8, 0, 0, 0 }
                    },
                },

                // === Danh sách các đề bài KHÓ ===
                [Difficulty.Hard] = new List<int[,]>
                {
                    new int[9, 9] // Đề Khó #1
                    {
                        { 0, 2, 0, 6, 0, 8, 0, 0, 0 },
                        { 5, 8, 0, 0, 0, 9, 7, 0, 0 },
                        { 0, 0, 0, 0, 4, 0, 0, 0, 0 },
                        { 3, 7, 0, 0, 0, 0, 5, 0, 0 },
                        { 6, 0, 0, 0, 0, 0, 0, 0, 4 },
                        { 0, 0, 8, 0, 0, 0, 0, 1, 3 },
                        { 0, 0, 0, 0, 2, 0, 0, 0, 0 },
                        { 0, 0, 9, 8, 0, 0, 0, 3, 6 },
                        { 0, 0, 0, 3, 0, 6, 0, 9, 0 }
                    }
                }
            };
        }

        /// <summary>
        /// Lấy ngẫu nhiên một đề bài từ trong kho theo độ khó được chỉ định.
        /// </summary>
        public int[,] GenerateRandomPuzzle(Difficulty difficulty)
        {
            if (_puzzles.TryGetValue(difficulty, out var puzzleList))
            {
                int index = _random.Next(puzzleList.Count);
                return puzzleList[index];
            }

            // Trường hợp dự phòng: nếu không tìm thấy list cho độ khó đó, trả về đề dễ
            return _puzzles[Difficulty.Easy][0];
        }
    }
}