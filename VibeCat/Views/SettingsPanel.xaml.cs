using System;
using System.Windows;
using System.Windows.Controls;

namespace VibeCat.Views;

public partial class SettingsPanel : UserControl
{
    public event EventHandler<double>? OpacityChanged;

    public SettingsPanel()
    {
        InitializeComponent();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Convert percentage (0-100) to opacity (0.0-1.0)
        var opacity = e.NewValue / 100.0;
        OpacityChanged?.Invoke(this, opacity);
    }
}