/**
 * @file: RulesDialog.xaml.cs
 * @description: Модальне вікно з правилами гри
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Input;

namespace BoardGamesLibrary.Desktop;

public partial class RulesDialog : Window
{
    public RulesDialog(string gameName, string rulesText)
    {
        InitializeComponent();
        TitleText.Text = $"ПРАВИЛА: {gameName.ToUpper()}";
        RulesText.Text = rulesText;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}

