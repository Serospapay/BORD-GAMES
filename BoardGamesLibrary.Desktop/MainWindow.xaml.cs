/**
 * @file: MainWindow.xaml.cs
 * @description: Головне вікно з меню вибору ігор
 * @dependencies: GameWindows
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using BoardGamesLibrary.Desktop.Services;
using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly StatisticsService _statisticsService;
    private readonly SoundService _soundService;

    public MainWindow()
    {
        InitializeComponent();
        _statisticsService = new StatisticsService();
        _soundService = SoundService.Instance;
        Loaded += MainWindow_Loaded;
        UpdateSoundButton();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Невелика затримка, щоб переконатися, що всі елементи ініціалізовані
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateStatistics();
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateStatistics()
    {
        try
        {
            var panel = StatsPanel ?? FindName("StatsPanel") as System.Windows.Controls.StackPanel;
            if (panel == null)
            {
                System.Diagnostics.Debug.WriteLine("StatsPanel не знайдено");
                return;
            }

            var stats = _statisticsService.GetAllStatistics();
            panel.Children.Clear();
            
            System.Diagnostics.Debug.WriteLine($"Завантажено статистику: {stats.TotalGamesPlayed} ігор");
            System.Diagnostics.Debug.WriteLine($"StatsPanel знайдено: {StatsPanel != null}, Children count: {StatsPanel?.Children.Count ?? 0}");

            // Завжди показуємо панель, навіть якщо немає статистики
            if (stats.TotalGamesPlayed > 0)
            {
                // Загальна статистика
                var totalStatsText = new System.Windows.Controls.TextBlock
                {
                    Text = $"Всього ігор: {stats.TotalGamesPlayed}",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(241, 245, 249)),
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                panel.Children.Add(totalStatsText);

                // Статистика для кожної гри
                var gameNames = new[] { "Шахи", "Шашки", "Reversi", "Connect Four" };
                foreach (var gameName in gameNames)
                {
                    var gameStats = stats.GetOrCreateGameStats(gameName);
                    if (gameStats.TotalGames > 0)
                    {
                        var gameStatsText = new System.Windows.Controls.TextBlock
                        {
                            Text = $"{gameName}: {gameStats.TotalGames} ігор | " +
                                   $"Player1: {gameStats.Player1Wins} ({gameStats.Player1WinRate:F1}%) | " +
                                   $"Player2: {gameStats.Player2Wins} ({gameStats.Player2WinRate:F1}%) | " +
                                   $"Нічиї: {gameStats.Draws}",
                            FontSize = 14,
                            Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(148, 163, 184)),
                            Margin = new Thickness(0, 0, 0, 10),
                            TextWrapping = TextWrapping.Wrap
                        };
                        panel.Children.Add(gameStatsText);
                    }
                }
            }
            else
            {
                var noStatsText = new System.Windows.Controls.TextBlock
                {
                    Text = "Поки що немає статистики.\nЗіграйте кілька ігор!",
                    FontSize = 16,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(148, 163, 184)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(10)
                };
                panel.Children.Add(noStatsText);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка оновлення статистики: {ex.Message}\n{ex.StackTrace}");
            
            var errorPanel = StatsPanel ?? FindName("StatsPanel") as System.Windows.Controls.StackPanel;
            if (errorPanel != null)
            {
                errorPanel.Children.Clear();
                var errorText = new System.Windows.Controls.TextBlock
                {
                    Text = $"Помилка завантаження статистики:\n{ex.Message}",
                    FontSize = 14,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(239, 68, 68)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10)
                };
                errorPanel.Children.Add(errorText);
            }
        }
    }

    private void GameModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var modeTag = button.Tag as string;
        var cardButton = FindTemplatedParent<Button>(button);
        var gameTag = cardButton?.Tag as string;

        if (string.IsNullOrEmpty(gameTag) || string.IsNullOrEmpty(modeTag))
            return;

        var (playVsAI, difficulty) = ParseMode(modeTag);

        try
        {
            switch (gameTag)
            {
                case "Chess":
                    new ChessWindow(playVsAI, difficulty).ShowDialog();
                    break;
                case "Checkers":
                    new CheckersWindow(playVsAI, difficulty).ShowDialog();
                    break;
                case "Reversi":
                    new ReversiWindow(playVsAI, difficulty).ShowDialog();
                    break;
                case "ConnectFour":
                    new ConnectFourWindow(playVsAI, difficulty).ShowDialog();
                    break;
                default:
                    return;
            }
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка відкриття гри: {ex.Message}",
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static T? FindTemplatedParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.TemplatedParent is T parent)
                return parent;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static (bool playVsAI, AIDifficulty difficulty) ParseMode(string modeTag)
    {
        return modeTag switch
        {
            "2Players" => (false, AIDifficulty.Medium),
            "AIEasy" => (true, AIDifficulty.Easy),
            "AIMedium" => (true, AIDifficulty.Medium),
            "AIHard" => (true, AIDifficulty.Hard),
            _ => (false, AIDifficulty.Medium)
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private void SoundToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _soundService.IsEnabled = !_soundService.IsEnabled;
        UpdateSoundButton();
    }

    private void UpdateSoundButton()
    {
        if (SoundToggleButton != null)
        {
            SoundToggleButton.Content = _soundService.IsEnabled ? "Звук" : "Вимк";
            SoundToggleButton.ToolTip = _soundService.IsEnabled ? "Вимкнути звук" : "Увімкнути звук";
        }
    }
}
