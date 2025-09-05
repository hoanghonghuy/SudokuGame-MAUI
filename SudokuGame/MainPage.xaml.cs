#region Using Directives
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SudokuGame.Logic;
#endregion

namespace SudokuGame
{
    public partial class MainPage : ContentPage
    {
        private readonly Label[,] _cellLabels = new Label[9, 9];
        private readonly Border[,] _cellBorders = new Border[9, 9];
        private Border? _selectedCellBorder = null;
        private readonly SudokuGenerator _generator;
        private IDispatcherTimer? _timer;
        private int _secondsElapsed;
        private readonly List<Border> _highlightedBorders = new List<Border>();

        public MainPage()
        {
            InitializeComponent();
            _generator = new SudokuGenerator();
            CreateSudokuGrid();
            CreateNumberPad();
            SetupTimer();

            // Gắn sự kiện cho các nút
            HintButton.Clicked += OnHintButtonClicked;
            NewGameButton.Clicked += (s, e) => StartNewGame();
            EraseButton.Clicked += OnEraseButtonClicked;
            CheckButton.Clicked += OnCheckSolutionClicked;

            StartNewGame();
        }

        private async void OnHintButtonClicked(object? sender, EventArgs e)
        {
            if (_selectedCellBorder == null)
            {
                await DisplayAlert("Gợi ý", "Hãy chọn một ô trống để nhận gợi ý.", "OK");
                return;
            }

            var labelInside = _selectedCellBorder.Content as Label;
            if (labelInside != null && !string.IsNullOrEmpty(labelInside.Text))
            {
                await DisplayAlert("Gợi ý", "Ô này đã được điền số.", "OK");
                return;
            }

            // Lấy trạng thái bàn cờ hiện tại
            int[,] currentBoard = new int[9, 9];
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    currentBoard[r, c] = int.TryParse(_cellLabels[r, c].Text, out int num) ? num : 0;
                }
            }

            // Lấy vị trí ô được chọn
            int row = Grid.GetRow(_selectedCellBorder);
            int col = Grid.GetColumn(_selectedCellBorder);

            // Gọi bộ giải
            var solver = new SudokuSolver();
            int? hint = solver.GetHintForCell(currentBoard, row, col);

            if (hint.HasValue)
            {
                labelInside.Text = hint.Value.ToString();
                labelInside.TextColor = Colors.Green; // Tô màu xanh để phân biệt
            }
            else
            {
                await DisplayAlert("Không thể tìm thấy gợi ý", "Không tìm thấy nước đi hợp lệ từ trạng thái hiện tại của bàn cờ. Có thể bạn đã điền sai ở đâu đó.", "OK");
            }
        }

        #region Other Methods
        private void StartNewGame()
        {
            ClearHighlights();
            _selectedCellBorder = null;
            _secondsElapsed = 0;
            TimerLabel.Text = "00:00";
            _timer?.Start();
            Difficulty selectedDifficulty;
            if (MediumRadioButton.IsChecked) selectedDifficulty = Difficulty.Medium;
            else if (HardRadioButton.IsChecked) selectedDifficulty = Difficulty.Hard;
            else selectedDifficulty = Difficulty.Easy;
            int[,] puzzle = _generator.GenerateRandomPuzzle(selectedDifficulty);
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int number = puzzle[row, col];
                    Label cell = _cellLabels[row, col];
                    if (number != 0)
                    {
                        cell.Text = number.ToString();
                        cell.FontAttributes = FontAttributes.Bold;
                        cell.TextColor = Colors.Black;
                    }
                    else
                    {
                        cell.Text = string.Empty;
                        cell.FontAttributes = FontAttributes.None;
                        cell.TextColor = Colors.DodgerBlue;
                    }
                }
            }
        }
        private void OnCellTapped(object? sender, TappedEventArgs e)
        {
            var tappedBorder = sender as Border;
            if (tappedBorder == null) return;
            ClearHighlights();
            if (_selectedCellBorder == tappedBorder)
            {
                _selectedCellBorder = null;
                return;
            }
            var labelInside = tappedBorder.Content as Label;
            if (labelInside != null && labelInside.FontAttributes == FontAttributes.Bold)
            {
                _selectedCellBorder = null;
                return;
            }
            _selectedCellBorder = tappedBorder;
            int tappedRow = Grid.GetRow(tappedBorder);
            int tappedCol = Grid.GetColumn(tappedBorder);
            HighlightRelatedCells(tappedRow, tappedCol);
            _selectedCellBorder.BackgroundColor = Colors.LightBlue;
        }
        private void HighlightRelatedCells(int row, int col)
        {
            for (int i = 0; i < 9; i++)
            {
                _cellBorders[row, i].BackgroundColor = Colors.Gainsboro;
                _highlightedBorders.Add(_cellBorders[row, i]);
            }
            for (int i = 0; i < 9; i++)
            {
                _cellBorders[i, col].BackgroundColor = Colors.Gainsboro;
                _highlightedBorders.Add(_cellBorders[i, col]);
            }
            int startRow = row - row % 3;
            int startCol = col - col % 3;
            for (int i = startRow; i < startRow + 3; i++)
            {
                for (int j = startCol; j < startCol + 3; j++)
                {
                    _cellBorders[i, j].BackgroundColor = Colors.Gainsboro;
                    _highlightedBorders.Add(_cellBorders[i, j]);
                }
            }
        }
        private void ClearHighlights()
        {
            if (_selectedCellBorder != null)
            {
                _selectedCellBorder.BackgroundColor = Colors.White;
            }
            foreach (var border in _highlightedBorders)
            {
                border.BackgroundColor = Colors.White;
            }
            _highlightedBorders.Clear();
        }
        private void SetupTimer() { _timer = Application.Current.Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(1); _timer.Tick += OnTimerTick; }
        private void OnTimerTick(object? sender, EventArgs e) { _secondsElapsed++; TimerLabel.Text = TimeSpan.FromSeconds(_secondsElapsed).ToString(@"mm\:ss"); }
        private async void OnCheckSolutionClicked(object? sender, EventArgs e)
        {
            int[,] currentBoard = new int[9, 9];
            for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++) currentBoard[r, c] = int.TryParse(_cellLabels[r, c].Text, out int n) ? n : 0;
            var v = new SudokuValidator(); bool isValid = v.IsBoardValid(currentBoard); bool isComplete = v.IsBoardComplete(currentBoard);
            if (isComplete && isValid) { _timer?.Stop(); bool pa = await DisplayAlert("Chúc Mừng!", $"Bạn đã giải thành công trong {TimerLabel.Text}!", "Chơi Ván Mới", "Thoát"); if (pa) StartNewGame(); }
            else if (isValid) await DisplayAlert("Chính Xác!", "Tất cả các số bạn đã điền đều đúng. Hãy tiếp tục!", "OK");
            else await DisplayAlert("Có Lỗi Sai!", "Vẫn còn lỗi sai trên bàn cờ. Hãy kiểm tra lại nhé!", "OK");
        }
        private void OnEraseButtonClicked(object? sender, EventArgs e)
        {
            if (_selectedCellBorder == null) return;
            var l = _selectedCellBorder.Content as Label;
            if (l != null && l.FontAttributes != FontAttributes.Bold) l.Text = string.Empty;
        }
        private void OnNumberButtonClicked(object? sender, EventArgs e)
        {
            if (_selectedCellBorder == null) return;
            var b = sender as Button; if (b == null) return;
            var l = _selectedCellBorder.Content as Label; if (l != null) { l.Text = b.Text; l.TextColor = Colors.DodgerBlue; }
        }
        private void CreateSudokuGrid()
        {
            for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++)
                {
                    var l = new Label { Text = "", FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                    _cellLabels[r, c] = l;
                    var b = new Border { Content = l, Stroke = Colors.DarkGray, StrokeThickness = 1, BackgroundColor = Colors.White, Margin = new Thickness((c > 0 && c % 3 == 0) ? 2 : 0.5, (r > 0 && r % 3 == 0) ? 2 : 0.5, 0.5, 0.5) };
                    _cellBorders[r, c] = b;
                    var t = new TapGestureRecognizer(); t.Tapped += OnCellTapped; b.GestureRecognizers.Add(t);
                    Grid.SetRow(b, r); Grid.SetColumn(b, c); SudokuGrid.Children.Add(b);
                }
        }
        private void CreateNumberPad()
        {
            for (int i = 1; i <= 9; i++)
            {
                var b = new Button { Text = i.ToString(), WidthRequest = 40, HeightRequest = 40 };
                b.Clicked += OnNumberButtonClicked; NumberPad.Children.Add(b);
            }
        }
        #endregion
    }
}