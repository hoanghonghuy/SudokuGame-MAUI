using SudokuGame.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SudokuGame
{
    public partial class StatsPage : ContentPage
    {
        private PlayerStats _playerStats = new PlayerStats();
        private readonly string _statsFilePath;

        public StatsPage()
        {
            InitializeComponent();
            _statsFilePath = Path.Combine(FileSystem.AppDataDirectory, "sudokugame_stats.json");
        }

        // Dùng OnAppearing để đảm bảo dữ liệu luôn được làm mới khi người dùng quay lại tab này
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPlayerStatsAsync();
            UpdateDisplayForSelectedDifficulty();
        }

        private async Task LoadPlayerStatsAsync()
        {
            if (!File.Exists(_statsFilePath))
            {
                _playerStats = new PlayerStats();
                return;
            }
            try
            {
                string jsonStats = await File.ReadAllTextAsync(_statsFilePath);
                _playerStats = JsonConvert.DeserializeObject<PlayerStats>(jsonStats) ?? new PlayerStats();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading player stats: {ex.Message}");
                _playerStats = new PlayerStats();
            }
        }

        // Sự kiện này được gọi mỗi khi người dùng chọn một độ khó khác
        private void Difficulty_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value) // Chỉ thực hiện khi RadioButton được chọn
            {
                UpdateDisplayForSelectedDifficulty();
            }
        }

        private void UpdateDisplayForSelectedDifficulty()
        {
            StatsData selectedStats;

            if (MediumRadio.IsChecked)
                selectedStats = _playerStats.Medium;
            else if (HardRadio.IsChecked)
                selectedStats = _playerStats.Hard;
            else // Mặc định là Dễ
                selectedStats = _playerStats.Easy;

            // Cập nhật các Label trên giao diện bằng dữ liệu đã được tính toán
            GamesPlayedLabel.Text = selectedStats.GamesPlayed.ToString();
            GamesWonLabel.Text = selectedStats.GamesWon.ToString();
            WinRateLabel.Text = selectedStats.WinRateDisplay;

            BestTimeLabel.Text = selectedStats.BestTimeDisplay;
            AverageTimeLabel.Text = selectedStats.AverageTimeDisplay;

            CurrentStreakLabel.Text = selectedStats.CurrentWinStreak.ToString();
            LongestStreakLabel.Text = selectedStats.LongestWinStreak.ToString();
        }
    }
}