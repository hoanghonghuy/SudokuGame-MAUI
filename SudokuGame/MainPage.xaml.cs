#region Using Directives
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SudokuGame.Logic;
using SudokuGame.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
#endregion

namespace SudokuGame
{
    // Struct để lưu thông tin một nước đi cho tính năng Undo
    internal readonly struct Move
    {
        public readonly int Row;
        public readonly int Col;
        public readonly int OldValue;
        public readonly int NewValue;

        public Move(int row, int col, int oldValue, int newValue)
        {
            Row = row;
            Col = col;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    [QueryProperty(nameof(ChallengeDate), "ChallengeDate")]
    public partial class MainPage : ContentPage
    {
        #region Variables and Properties
        // UI Elements
        private readonly Label[,] _cellLabels = new Label[9, 9];
        private readonly Border[,] _cellBorders = new Border[9, 9];
        private Label _mistakesLabel; // Gán từ XAML

        // Game Logic & State
        private readonly SudokuGenerator _generator;
        private IDispatcherTimer? _timer;
        private int _secondsElapsed;
        private int[,]? _initialPuzzle;
        private int[,]? _solutionBoard;
        private bool[,] _hintedCells = new bool[9, 9];
        private Border? _selectedCellBorder = null;
        private readonly List<Border> _highlightedBorders = new List<Border>();
        private readonly List<Border> _errorBorders = new List<Border>();
        private Stack<Move> _moveHistory;
        private int _mistakesCount;
        private const int MAX_MISTAKES = 3;

        // Player Data & File Paths
        private PlayerStats _playerStats;
        private readonly string _saveFilePath;
        private readonly string _statsFilePath;

        // Daily Challenge State
        private bool _isDailyChallengeMode = false;
        private DateTime? _challengeDate;
        public DateTime ChallengeDate
        {
            set
            {
                _challengeDate = value;
                if (_challengeDate.HasValue)
                {
                    _isDailyChallengeMode = true;
                    StartGameForChallenge(_challengeDate.Value);
                }
            }
        }
        #endregion

        public MainPage()
        {
            InitializeComponent();
            _generator = new SudokuGenerator();
            _playerStats = new PlayerStats();
            _moveHistory = new Stack<Move>();

            _saveFilePath = Path.Combine(FileSystem.AppDataDirectory, "sudokugame_save.json");
            _statsFilePath = Path.Combine(FileSystem.AppDataDirectory, "sudokugame_stats.json");

            _mistakesLabel = this.MistakesLabel; // Gán label Lỗi từ XAML

            CreateSudokuGrid();
            CreateNumberPad();
            SetupTimer();
            SetupActionButtons(); // Tạo các nút chức năng bằng code
        }

        #region Page Lifecycle & Navigation
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPlayerStatsAsync();
            if (!_isDailyChallengeMode)
            {
                if (!await LoadGameStateAsync())
                {
                    StartNewGame(true);
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_initialPuzzle != null && !_isDailyChallengeMode)
            {
                SaveGameStateAsync();
                SavePlayerStatsAsync();
            }
        }

        private async void OnDailyChallengeButtonClicked(object sender, EventArgs e)
        {
            _isDailyChallengeMode = false;
            await Shell.Current.GoToAsync(nameof(DailyChallengePage));
        }
        #endregion

        #region Game Initialization
        private void StartNewGame(bool forceNewGame)
        {
            if (!forceNewGame && _initialPuzzle != null) return;

            _isDailyChallengeMode = false;
            SetDifficultySelection(true);
            UpdateDifficultyLabel();
            ResetGameBoard();

            Difficulty selectedDifficulty;
            if (MediumRadioButton.IsChecked) selectedDifficulty = Difficulty.Medium;
            else if (HardRadioButton.IsChecked) selectedDifficulty = Difficulty.Hard;
            else selectedDifficulty = Difficulty.Easy;

            UpdateGamesPlayedStat(selectedDifficulty);

            var localGenerator = new SudokuGenerator();
            _initialPuzzle = localGenerator.GenerateRandomPuzzle(selectedDifficulty);

            _solutionBoard = (int[,])_initialPuzzle.Clone();
            new SudokuSolver().Solve(_solutionBoard);

            DisplayBoard(_initialPuzzle, _initialPuzzle);
        }

        private void StartGameForChallenge(DateTime date)
        {
            SetDifficultySelection(false);
            DifficultyLabel.Text = "Thử thách ngày";
            ResetGameBoard();

            int seed = date.Year * 10000 + date.Month * 100 + date.Day;
            var dailyGenerator = new SudokuGenerator(seed);
            _initialPuzzle = dailyGenerator.GenerateRandomPuzzle(Difficulty.Medium);

            _solutionBoard = (int[,])_initialPuzzle.Clone();
            new SudokuSolver().Solve(_solutionBoard);

            DisplayBoard(_initialPuzzle, _initialPuzzle);
        }

        private void ResetGameBoard()
        {
            DeleteSaveFile();
            ClearHighlights();
            ClearErrorHighlights();
            _selectedCellBorder = null;
            _secondsElapsed = 0;
            TimerLabel.Text = "00:00";
            _timer?.Start();
            _hintedCells = new bool[9, 9];
            _mistakesCount = 0;
            UpdateMistakesDisplay();
            _moveHistory = new Stack<Move>();
        }
        #endregion

        #region UI Interaction & Game Actions
        private async void OnNumberButtonClicked(object? sender, EventArgs e)
        {
            if (_selectedCellBorder == null || _solutionBoard == null) return;
            var button = sender as Button; if (button == null) return;

            int row = Grid.GetRow(_selectedCellBorder);
            int col = Grid.GetColumn(_selectedCellBorder);

            if (_hintedCells[row, col]) return;

            var label = _selectedCellBorder.Content as Label;
            if (label == null) return;

            int number = int.Parse(button.Text);
            int oldValue = int.TryParse(label.Text, out int val) ? val : 0;
            if (oldValue == number) return;

            _moveHistory.Push(new Move(row, col, oldValue, number));

            if (number == _solutionBoard[row, col])
            {
                label.Text = button.Text;
                label.TextColor = Colors.DodgerBlue;
                ClearErrorHighlights();

                var currentBoard = GetCurrentBoardState();
                if (new SudokuValidator().IsBoardComplete(currentBoard))
                {
                    OnCheckSolutionClicked(this, EventArgs.Empty);
                }
            }
            else
            {
                _mistakesCount++;
                UpdateMistakesDisplay();

                if (_mistakesCount >= MAX_MISTAKES)
                {
                    _timer?.Stop();
                    await DisplayAlert("Thất bại", "Bạn đã mắc quá số lỗi cho phép. Hãy thử lại ván mới!", "Chơi Ván Mới");
                    StartNewGame(true);
                }
                else
                {
                    await DisplayAlert("Sai rồi!", $"Bạn đã mắc {_mistakesCount}/{MAX_MISTAKES} lỗi.", "OK");
                }
            }
        }

        private void OnEraseButtonClicked(object? sender, EventArgs e)
        {
            if (_selectedCellBorder == null) return;

            int row = Grid.GetRow(_selectedCellBorder);
            int col = Grid.GetColumn(_selectedCellBorder);
            if (_hintedCells[row, col]) return;

            var label = _selectedCellBorder.Content as Label;
            if (label != null && label.FontAttributes != FontAttributes.Bold && !string.IsNullOrEmpty(label.Text))
            {
                int oldValue = int.Parse(label.Text);
                _moveHistory.Push(new Move(row, col, oldValue, 0));
                label.Text = string.Empty;
            }

            ClearErrorHighlights();
        }

        private void OnUndoButtonClicked(object? sender, EventArgs e)
        {
            if (_moveHistory.Count > 0)
            {
                Move lastMove = _moveHistory.Pop();
                var label = _cellLabels[lastMove.Row, lastMove.Col];

                label.Text = lastMove.OldValue == 0 ? string.Empty : lastMove.OldValue.ToString();

                if (lastMove.OldValue != 0)
                {
                    label.TextColor = Colors.DodgerBlue;
                }
            }
        }

        private async void OnCheckSolutionClicked(object? sender, EventArgs e)
        {
            if (_initialPuzzle == null) return;
            _timer?.Stop();

            if (_isDailyChallengeMode && _challengeDate.HasValue)
            {
                await UpdateDailyChallengeStats(_challengeDate.Value);
                await DisplayAlert("Chúc Mừng!", $"Bạn đã hoàn thành thử thách trong {TimerLabel.Text}!", "OK");
                _isDailyChallengeMode = false;
                _challengeDate = null;
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await UpdateStatsOnWin();
                DeleteSaveFile();
                bool playAgain = await DisplayAlert("Chúc Mừng!", $"Bạn đã giải thành công trong {TimerLabel.Text}!", "Chơi Ván Mới", "Thoát");
                if (playAgain) StartNewGame(true);
            }
        }

        #endregion

        #region Helper & UI Update Methods
        private void UpdateMistakesDisplay()
        {
            if (_mistakesLabel != null)
            {
                _mistakesLabel.Text = $"Lỗi: {_mistakesCount}/{MAX_MISTAKES}";
            }
        }

        private void OnDifficultyChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value) UpdateDifficultyLabel();
        }

        private void UpdateDifficultyLabel()
        {
            if (EasyRadioButton.IsChecked) DifficultyLabel.Text = "Dễ";
            else if (MediumRadioButton.IsChecked) DifficultyLabel.Text = "Trung bình";
            else if (HardRadioButton.IsChecked) DifficultyLabel.Text = "Khó";
        }

        private void SetupActionButtons()
        {
            MainButtonLayout.Clear();

            var undoButton = new Button { Text = "Hoàn tác", WidthRequest = 85 };
            undoButton.Clicked += OnUndoButtonClicked;

            var eraseButton = new Button { Text = "Xóa", WidthRequest = 85 };
            eraseButton.Clicked += OnEraseButtonClicked;

            var hintButton = new Button { Text = "Gợi ý", WidthRequest = 85 };
            hintButton.Clicked += OnHintButtonClicked;

            var dailyChallengeButton = new Button { Text = "Hàng ngày", WidthRequest = 100 };
            dailyChallengeButton.Clicked += OnDailyChallengeButtonClicked;

            var newGameButton = new Button { Text = "Ván mới", WidthRequest = 85 };
            newGameButton.Clicked += (s, e) => StartNewGame(true);

            MainButtonLayout.Children.Add(undoButton);
            MainButtonLayout.Children.Add(eraseButton);
            MainButtonLayout.Children.Add(hintButton);
            MainButtonLayout.Children.Add(dailyChallengeButton);
            MainButtonLayout.Children.Add(newGameButton);
        }
        #endregion

        #region Unchanged Methods (Highlighting, Data Persistence, etc.)
        private async void OnHintButtonClicked(object? sender, EventArgs e)
        {
            if (_selectedCellBorder == null) { await DisplayAlert("Gợi ý", "Hãy chọn một ô trống để nhận gợi ý.", "OK"); return; }
            var labelInside = _selectedCellBorder.Content as Label;
            if (labelInside != null && !string.IsNullOrEmpty(labelInside.Text)) { await DisplayAlert("Gợi ý", "Ô này đã được điền số.", "OK"); return; }
            int row = Grid.GetRow(_selectedCellBorder); int col = Grid.GetColumn(_selectedCellBorder);
            if (_hintedCells[row, col]) return;
            int[,] currentBoard = GetCurrentBoardState();
            var solver = new SudokuSolver(); int? hint = solver.GetHintForCell(currentBoard, row, col);
            if (hint.HasValue) { labelInside.Text = hint.Value.ToString(); labelInside.TextColor = Colors.Green; _hintedCells[row, col] = true; }
            else { await DisplayAlert("Không thể tìm thấy gợi ý", "Không tìm thấy nước đi hợp lệ từ trạng thái hiện tại của bàn cờ.", "OK"); }
        }

        private void OnCellTapped(object? sender, TappedEventArgs e)
        {
            var tappedBorder = sender as Border; if (tappedBorder == null) return;
            ClearErrorHighlights(); ClearHighlights();
            if (_selectedCellBorder == tappedBorder) { _selectedCellBorder = null; return; }
            var labelInside = tappedBorder.Content as Label;
            if (labelInside != null && labelInside.FontAttributes == FontAttributes.Bold) { _selectedCellBorder = null; return; }
            _selectedCellBorder = tappedBorder;
            int tappedRow = Grid.GetRow(tappedBorder); int tappedCol = Grid.GetColumn(tappedBorder);
            HighlightRelatedCells(tappedRow, tappedCol);
            _selectedCellBorder.BackgroundColor = Colors.LightBlue;
        }

        private void SetDifficultySelection(bool isEnabled)
        {
            DifficultyRadioButtons.IsEnabled = isEnabled;
            DifficultyRadioButtons.Opacity = isEnabled ? 1.0 : 0.5;
        }

        private void HighlightRelatedCells(int row, int col)
        {
            for (int i = 0; i < 9; i++) { _cellBorders[row, i].BackgroundColor = Colors.Gainsboro; _highlightedBorders.Add(_cellBorders[row, i]); }
            for (int i = 0; i < 9; i++) { if (!_highlightedBorders.Contains(_cellBorders[i, col])) { _cellBorders[i, col].BackgroundColor = Colors.Gainsboro; _highlightedBorders.Add(_cellBorders[i, col]); } }
            int startRow = row - row % 3, startCol = col - col % 3;
            for (int i = startRow; i < startRow + 3; i++) for (int j = startCol; j < startCol + 3; j++) { if (!_highlightedBorders.Contains(_cellBorders[i, j])) { _cellBorders[i, j].BackgroundColor = Colors.Gainsboro; _highlightedBorders.Add(_cellBorders[i, j]); } }
        }

        private void ClearHighlights()
        {
            if (_selectedCellBorder != null) { _selectedCellBorder.BackgroundColor = Colors.White; }
            foreach (var border in _highlightedBorders) { border.BackgroundColor = Colors.White; }
            _highlightedBorders.Clear();
        }

        private void HighlightInvalidCells(List<(int row, int col)> invalidCells)
        {
            foreach (var cell in invalidCells)
            {
                var border = _cellBorders[cell.row, cell.col];
                border.BackgroundColor = Colors.LightPink;
                _errorBorders.Add(border);
            }
        }

        private void ClearErrorHighlights()
        {
            foreach (var border in _errorBorders) { border.BackgroundColor = Colors.White; }
            _errorBorders.Clear();
        }

        private async Task LoadPlayerStatsAsync()
        {
            if (!File.Exists(_statsFilePath)) { _playerStats = new PlayerStats(); return; }
            try { string jsonStats = await File.ReadAllTextAsync(_statsFilePath); _playerStats = JsonConvert.DeserializeObject<PlayerStats>(jsonStats) ?? new PlayerStats(); }
            catch (Exception ex) { Console.WriteLine($"Error loading stats: {ex.Message}"); _playerStats = new PlayerStats(); }
        }

        private async Task SavePlayerStatsAsync()
        {
            try { string jsonStats = JsonConvert.SerializeObject(_playerStats, Formatting.Indented); await File.WriteAllTextAsync(_statsFilePath, jsonStats); }
            catch (Exception ex) { Console.WriteLine($"Error saving stats: {ex.Message}"); }
        }

        private void UpdateGamesPlayedStat(Difficulty difficulty)
        {
            StatsData stats = difficulty switch { Difficulty.Medium => _playerStats.Medium, Difficulty.Hard => _playerStats.Hard, _ => _playerStats.Easy };
            stats.GamesPlayed++;
            stats.CurrentWinStreak = 0;
        }

        private async Task UpdateStatsOnWin()
        {
            Difficulty difficulty;
            if (MediumRadioButton.IsChecked) difficulty = Difficulty.Medium; else if (HardRadioButton.IsChecked) difficulty = Difficulty.Hard; else difficulty = Difficulty.Easy;
            StatsData stats = difficulty switch { Difficulty.Medium => _playerStats.Medium, Difficulty.Hard => _playerStats.Hard, _ => _playerStats.Easy };
            stats.GamesWon++;
            if (stats.BestTimeInSeconds == -1 || _secondsElapsed < stats.BestTimeInSeconds) { stats.BestTimeInSeconds = _secondsElapsed; }
            stats.TotalSecondsPlayedInWins += _secondsElapsed;
            stats.CurrentWinStreak++;
            if (stats.CurrentWinStreak > stats.LongestWinStreak) { stats.LongestWinStreak = stats.CurrentWinStreak; }
            await SavePlayerStatsAsync();
        }

        private async Task UpdateDailyChallengeStats(DateTime date)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            if (_playerStats.DailyChallengeBestTimes.TryGetValue(dateKey, out int bestTime)) { if (_secondsElapsed < bestTime) { _playerStats.DailyChallengeBestTimes[dateKey] = _secondsElapsed; } }
            else { _playerStats.DailyChallengeBestTimes.Add(dateKey, _secondsElapsed); }
            await SavePlayerStatsAsync();
        }

        private async Task SaveGameStateAsync()
        {
            var gameState = new GameState { InitialPuzzle = _initialPuzzle, CurrentBoard = GetCurrentBoardState(), SecondsElapsed = _secondsElapsed };
            string jsonState = JsonConvert.SerializeObject(gameState);
            await File.WriteAllTextAsync(_saveFilePath, jsonState);
        }

        private async Task<bool> LoadGameStateAsync()
        {
            if (!File.Exists(_saveFilePath)) return false;
            try
            {
                string jsonState = await File.ReadAllTextAsync(_saveFilePath);
                var gameState = JsonConvert.DeserializeObject<GameState>(jsonState);
                if (gameState != null && gameState.InitialPuzzle != null && gameState.CurrentBoard != null)
                {
                    _initialPuzzle = gameState.InitialPuzzle;
                    _solutionBoard = (int[,])_initialPuzzle.Clone(); new SudokuSolver().Solve(_solutionBoard);
                    _secondsElapsed = gameState.SecondsElapsed;
                    DisplayBoard(gameState.InitialPuzzle, gameState.CurrentBoard);
                    TimerLabel.Text = TimeSpan.FromSeconds(_secondsElapsed).ToString(@"mm\:ss");
                    _timer?.Start();
                    return true;
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error loading game: {ex.Message}"); DeleteSaveFile(); }
            return false;
        }

        private void DeleteSaveFile()
        {
            if (File.Exists(_saveFilePath)) { File.Delete(_saveFilePath); }
        }

        private void SetupTimer() { _timer = Application.Current.Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(1); _timer.Tick += OnTimerTick; }
        private void OnTimerTick(object? sender, EventArgs e) { _secondsElapsed++; TimerLabel.Text = TimeSpan.FromSeconds(_secondsElapsed).ToString(@"mm\:ss"); }

        private int[,] GetCurrentBoardState()
        {
            var board = new int[9, 9];
            for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++) { board[r, c] = int.TryParse(_cellLabels[r, c].Text, out int num) ? num : 0; }
            return board;
        }

        private void DisplayBoard(int[,] initialBoard, int[,] currentBoard)
        {
            for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++)
                {
                    int initialNumber = initialBoard[r, c]; int currentNumber = currentBoard[r, c]; Label cell = _cellLabels[r, c];
                    if (initialNumber != 0) { cell.Text = initialNumber.ToString(); cell.FontAttributes = FontAttributes.Bold; cell.TextColor = Colors.Black; }
                    else if (currentNumber != 0) { cell.Text = currentNumber.ToString(); cell.FontAttributes = FontAttributes.None; cell.TextColor = Colors.DodgerBlue; }
                    else { cell.Text = string.Empty; }
                }
        }

        private void CreateSudokuGrid()
        {
            for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++)
                {
                    var label = new Label { FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }; _cellLabels[r, c] = label;
                    var border = new Border { Content = label, Stroke = Colors.DarkGray, StrokeThickness = 1, BackgroundColor = Colors.White, Margin = new Thickness((c > 0 && c % 3 == 0) ? 2 : 0.5, (r > 0 && r % 3 == 0) ? 2 : 0.5, 0.5, 0.5) }; _cellBorders[r, c] = border;
                    var tapGesture = new TapGestureRecognizer(); tapGesture.Tapped += OnCellTapped; border.GestureRecognizers.Add(tapGesture);
                    Grid.SetRow(border, r); Grid.SetColumn(border, c); SudokuGrid.Children.Add(border);
                }
        }

        private void CreateNumberPad()
        {
            for (int i = 1; i <= 9; i++)
            {
                var button = new Button { Text = i.ToString(), WidthRequest = 40, HeightRequest = 40 };
                button.Clicked += OnNumberButtonClicked; NumberPad.Children.Add(button);
            }
        }
        #endregion
    }
}