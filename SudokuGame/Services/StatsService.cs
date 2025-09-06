using SudokuGame.Logic;
using SudokuGame.Models;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace SudokuGame.Services
{
    public class StatsService
    {
        public static StatsService Instance { get; } = new StatsService();
        private readonly string _statsFilePath;
        public PlayerStats Stats { get; private set; }

        private StatsService()
        {
            _statsFilePath = Path.Combine(FileSystem.AppDataDirectory, "sudokugame_stats.json");
            Stats = new PlayerStats();
        }

        public async Task LoadStatsAsync()
        {
            if (!File.Exists(_statsFilePath)) return;
            try
            {
                string json = await File.ReadAllTextAsync(_statsFilePath);
                var loadedStats = JsonConvert.DeserializeObject<PlayerStats>(json);
                if (loadedStats != null) Stats = loadedStats;
            }
            catch { /* Ignored */ }
        }

        public async Task SaveStatsAsync()
        {
            string json = JsonConvert.SerializeObject(Stats);
            await File.WriteAllTextAsync(_statsFilePath, json);
        }

        public void IncrementGamesPlayed(Difficulty difficulty)
        {
            GetStatsForDifficulty(difficulty).GamesPlayed++;
        }

        public void RecordWin(Difficulty difficulty, int timeInSeconds)
        {
            var statsData = GetStatsForDifficulty(difficulty);
            statsData.GamesWon++;
            if (statsData.BestTimeInSeconds == -1 || timeInSeconds < statsData.BestTimeInSeconds)
            {
                statsData.BestTimeInSeconds = timeInSeconds;
            }
        }

        private StatsData GetStatsForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => Stats.Easy,
                Difficulty.Medium => Stats.Medium,
                Difficulty.Hard => Stats.Hard,
                _ => Stats.Easy,
            };
        }
    }
}