using System.Windows;
using System.Windows.Controls;

namespace VibeCat.Views;

public partial class SettingsPanel : UserControl
{
    public event EventHandler<double>? OpacityChanged;
    public event EventHandler<bool>? SnappingEnabledChanged;
    public event EventHandler<double>? SnapDistanceChanged;
    public event EventHandler<bool>? AutoFlipEnabledChanged;
    public event EventHandler? ManualFlipRequested;
    public event EventHandler<double>? BPMChanged;
    public event EventHandler<bool>? ClickThroughChanged;

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

    private void SnappingEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            SnappingEnabledChanged?.Invoke(this, checkBox.IsChecked ?? true);
        }
    }

    private void SnapDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SnapDistanceChanged?.Invoke(this, e.NewValue);
    }

    private void AutoFlipEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            var isEnabled = checkBox.IsChecked ?? true;
            AutoFlipEnabledChanged?.Invoke(this, isEnabled);

            // Enable manual flip button only when auto-flip is disabled
            if (ManualFlipButton != null)
            {
                ManualFlipButton.IsEnabled = !isEnabled;
            }
        }
    }

    private void ManualFlipButton_Click(object sender, RoutedEventArgs e)
    {
        ManualFlipRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BPMSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        BPMChanged?.Invoke(this, e.NewValue);
    }

    public double BPM
    {
        get => BPMSlider?.Value ?? 115;
        set
        {
            if (BPMSlider != null)
            {
                BPMSlider.Value = Math.Max(60, Math.Min(180, value));
            }
        }
    }

    private void ClickThroughCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            ClickThroughChanged?.Invoke(this, checkBox.IsChecked ?? false);
        }
    }

    public void UpdateClickThroughState(bool isClickThrough)
    {
        if (ClickThroughCheckBox != null)
        {
            ClickThroughCheckBox.IsChecked = isClickThrough;
        }
    }
}