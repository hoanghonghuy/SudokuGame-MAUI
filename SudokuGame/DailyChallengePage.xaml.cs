using SudokuGame.Logic;
using SudokuGame.Models;
using System;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace SudokuGame
{
    public partial class DailyChallengePage : ContentPage
    {
        private DateTime _currentDate = DateTime.Today;
        private PlayerStats _playerStats = new PlayerStats();
        private readonly string _statsFilePath;
        private DateTime? _selectedDate = null;

        public DailyChallengePage()
        {
            InitializeComponent();
            _statsFilePath = Path.Combine(FileSystem.AppDataDirectory, "sudokugame_stats.json");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPlayerStatsAsync();
            BuildCalendar();
            UpdateSelectedDayInfo(); // Cập nhật thông tin cho ngày hôm nay (mặc định)
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

        private void BuildCalendar()
        {
            CalendarGrid.Children.Clear();
            MonthYearLabel.Text = _currentDate.ToString("MMMM, yyyy", new CultureInfo("vi-VN"));

            // Thêm các ngày trong tuần (T2, T3, ...)
            string[] weekdays = { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
            for (int i = 0; i < 7; i++)
            {
                var label = new Label { Text = weekdays[i], HorizontalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold };
                Grid.SetRow(label, 0);
                Grid.SetColumn(label, i);
                CalendarGrid.Children.Add(label);
            }

            DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            // DayOfWeek của .NET: Sunday = 0, Monday = 1...
            // Chúng ta muốn Monday = 0, Sunday = 6
            int startDayOffset = (int)firstDayOfMonth.DayOfWeek - 1;
            if (startDayOffset == -1) startDayOffset = 6; // Nếu là Chủ Nhật

            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);

            for (int i = 1; i <= daysInMonth; i++)
            {
                var dayDate = new DateTime(_currentDate.Year, _currentDate.Month, i);
                var dayLabel = new Label
                {
                    Text = i.ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = 16
                };

                var dayBorder = new Border
                {
                    Content = dayLabel,
                    StrokeThickness = 1,
                    Stroke = Colors.Gainsboro,
                    Padding = new Thickness(5),
                    BackgroundColor = Colors.White
                };

                // Thêm sự kiện Tap
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnDayTapped;
                dayBorder.GestureRecognizers.Add(tapGesture);

                // Gắn ngày vào control để lấy lại khi click
                dayBorder.BindingContext = dayDate;

                // Tô màu ngày hôm nay
                if (dayDate.Date == DateTime.Today)
                {
                    dayBorder.BackgroundColor = Colors.LightBlue;
                }

                // Tô màu các ngày đã hoàn thành
                string dateKey = dayDate.ToString("yyyy-MM-dd");
                if (_playerStats.DailyChallengeBestTimes.ContainsKey(dateKey))
                {
                    dayBorder.BackgroundColor = Colors.LightGreen;
                }

                // Tô màu xám các ngày trong tương lai
                if (dayDate.Date > DateTime.Today)
                {
                    dayBorder.BackgroundColor = Colors.Gainsboro;
                    dayLabel.TextColor = Colors.Gray;
                    dayBorder.GestureRecognizers.Clear(); // Vô hiệu hóa click
                }

                int row = (i + startDayOffset - 1) / 7 + 1;
                int col = (i + startDayOffset - 1) % 7;
                Grid.SetRow(dayBorder, row);
                Grid.SetColumn(dayBorder, col);
                CalendarGrid.Children.Add(dayBorder);
            }
        }

        private void OnDayTapped(object sender, TappedEventArgs e)
        {
            var border = sender as Border;
            if (border?.BindingContext is DateTime date)
            {
                _selectedDate = date;
                UpdateSelectedDayInfo();
            }
        }

        private void UpdateSelectedDayInfo()
        {
            if (_selectedDate == null)
            {
                _selectedDate = DateTime.Today; // Mặc định là ngày hôm nay
            }

            SelectedDateLabel.Text = _selectedDate.Value.ToString("'Thử thách ngày' dd/MM/yyyy");
            string dateKey = _selectedDate.Value.ToString("yyyy-MM-dd");

            if (_playerStats.DailyChallengeBestTimes.TryGetValue(dateKey, out int time))
            {
                ChallengeStatusLabel.Text = $"Hoàn thành trong: {TimeSpan.FromSeconds(time):mm\\:ss}";
                PlayChallengeButton.Text = "Chơi Lại";
                PlayChallengeButton.IsVisible = true;
            }
            else
            {
                ChallengeStatusLabel.Text = "Chưa hoàn thành";
                PlayChallengeButton.Text = "Chơi";
                PlayChallengeButton.IsVisible = true;
            }

            if (_selectedDate.Value.Date > DateTime.Today)
            {
                ChallengeStatusLabel.Text = "Thử thách chưa ra mắt";
                PlayChallengeButton.IsVisible = false;
            }
        }

        private async void OnPlayChallengeClicked(object sender, EventArgs e)
        {
            if (!_selectedDate.HasValue) return;

            // Truyền dữ liệu sang MainPage bằng Shell Navigation
            var navigationParameters = new Dictionary<string, object>
            {
                { "ChallengeDate", _selectedDate.Value }
            };
            await Shell.Current.GoToAsync("///MainPage", navigationParameters);
        }

        private void OnPrevMonthButtonClicked(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            BuildCalendar();
        }

        private void OnNextMonthButtonClicked(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            BuildCalendar();
        }
    }
}