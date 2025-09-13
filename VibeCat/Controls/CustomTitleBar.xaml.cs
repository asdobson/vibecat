using System;
using System.Windows;
using System.Windows.Controls;

namespace VibeCat.Controls;

public partial class CustomTitleBar : UserControl
{
    public event EventHandler? SettingsClicked;
    public event EventHandler? MinimizeClicked;
    public event EventHandler? CloseClicked;

    public CustomTitleBar()
    {
        InitializeComponent();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsClicked?.Invoke(this, EventArgs.Empty);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        MinimizeClicked?.Invoke(this, EventArgs.Empty);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseClicked?.Invoke(this, EventArgs.Empty);
    }
}