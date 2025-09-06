namespace SudokuGame;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đăng ký các route để có thể điều hướng bằng code
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(DailyChallengePage), typeof(DailyChallengePage));
        Routing.RegisterRoute(nameof(StatsPage), typeof(StatsPage));
    }
}