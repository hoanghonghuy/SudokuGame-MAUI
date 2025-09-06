using System.Collections.Generic;
using System; // Cần cho TimeSpan

namespace SudokuGame.Models
{
    public class StatsData
    {
        public int GamesPlayed { get; set; } // Ván chơi đã bắt đầu
        public int GamesWon { get; set; }

        public int BestTimeInSeconds { get; set; } = -1;

        // Các trường dữ liệu để tính toán
        public long TotalSecondsPlayedInWins { get; set; } = 0;
        public int CurrentWinStreak { get; set; }
        public int LongestWinStreak { get; set; }

        // Các thuộc tính chỉ đọc để tính toán và hiển thị
        // Giúp cho logic ở phần giao diện đơn giản hơn

        /// <summary>
        /// Tính toán và trả về tỉ lệ chiến thắng dưới dạng chuỗi ("0.00%")
        /// </summary>
        public string WinRateDisplay => GamesPlayed > 0
            ? $"{(double)GamesWon / GamesPlayed:P2}" // P2 là định dạng phần trăm với 2 chữ số thập phân
            : "0.00%";

        /// <summary>
        /// Tính toán và trả về thời gian trung bình dưới dạng chuỗi ("mm:ss")
        /// </summary>
        public string AverageTimeDisplay => GamesWon > 0
            ? TimeSpan.FromSeconds((double)TotalSecondsPlayedInWins / GamesWon).ToString(@"mm\:ss")
            : "--:--";

        /// <summary>
        /// Trả về thời gian ngắn nhất dưới dạng chuỗi ("mm:ss")
        /// </summary>
        public string BestTimeDisplay => BestTimeInSeconds != -1
            ? TimeSpan.FromSeconds(BestTimeInSeconds).ToString(@"mm\:ss")
            : "--:--";
    }

    public class PlayerStats
    {
        public StatsData Easy { get; set; } = new StatsData();
        public StatsData Medium { get; set; } = new StatsData();
        public StatsData Hard { get; set; } = new StatsData();

        // public StatsData Expert { get; set; } = new StatsData();
        // public StatsData Nightmare { get; set; } = new StatsData();

        public Dictionary<string, int> DailyChallengeBestTimes { get; set; } = new Dictionary<string, int>();
    }
}