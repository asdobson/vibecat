using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace VibeCat;

public partial class MainWindow : Window
{
    // Animation constants
    private const double AspectRatio = 4.0 / 3.0;
    private const int FadeAnimationDuration = 200;
    private const int DoubleClickThreshold = 300;
    private const double MinimumWindowWidth = 266;

    // P/Invoke constants
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private DateTime _lastClickTime = DateTime.MinValue;
    private IntPtr _windowHandle;
    private bool _isUIMode = false;

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += Window_MouseLeftButtonDown;
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        TitleBar.SettingsClicked += (s, e) => ToggleSettingsPanel();
        TitleBar.MinimizeClicked += (s, e) => WindowState = WindowState.Minimized;
        TitleBar.CloseClicked += (s, e) =>
        {
            CatAnimation.StopAnimation();
            Application.Current.Shutdown();
        };
        SettingsPanel.OpacityChanged += (s, opacity) => CatAnimation.SetOpacity(opacity);
        ResizeGrip.DragDelta += (s, e) => HandleResize(e);
    }
    
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.Now;
        if ((now - _lastClickTime).TotalMilliseconds < DoubleClickThreshold)
        {
            ToggleUIMode();
        }
        else
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }
        _lastClickTime = now;
    }
    
    private void ToggleUIMode()
    {
        _isUIMode = !_isUIMode;

        if (_isUIMode)
        {
            // Show UI overlay
            UIPanel.Visibility = Visibility.Visible;
            BackgroundOverlay.Visibility = Visibility.Visible;
            ShowInTaskbar = true;

            // Fade in animation
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(FadeAnimationDuration));
            UIPanel.BeginAnimation(OpacityProperty, fadeIn);
            BackgroundOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }
        else
        {
            // Hide UI overlay
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(FadeAnimationDuration));
            fadeOut.Completed += (s, e) =>
            {
                UIPanel.Visibility = Visibility.Collapsed;
                BackgroundOverlay.Visibility = Visibility.Collapsed;
            };
            UIPanel.BeginAnimation(OpacityProperty, fadeOut);
            BackgroundOverlay.BeginAnimation(OpacityProperty, fadeOut);

            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
        }
    }



    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(_windowHandle, GWL_EXSTYLE);
        SetWindowLong(_windowHandle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_NOACTIVATE);
    }

    private void ToggleSettingsPanel()
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Collapsed
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void HandleResize(DragDeltaEventArgs e)
    {
        // Use horizontal drag only (common pattern for aspect-locked resize)
        // This is how video players, image viewers, etc. handle aspect ratio resize
        var newWidth = Math.Max(MinimumWindowWidth, Width + e.HorizontalChange);

        // Set width and auto-calculate height to maintain aspect ratio
        Width = newWidth;
        Height = newWidth / AspectRatio;
    }
}