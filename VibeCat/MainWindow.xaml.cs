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
    private bool _isDragging = false;

    // Snapping properties
    public bool IsSnappingEnabled { get; set; } = true;
    public double SnapDistance { get; set; } = 20;

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += Window_MouseLeftButtonDown;
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        TitleBar.SettingsClicked += (s, e) => ToggleSettingsPanel();
        TitleBar.HotkeysClicked += (s, e) => ToggleHotkeysPanel();
        TitleBar.MinimizeClicked += (s, e) => WindowState = WindowState.Minimized;
        TitleBar.CloseClicked += (s, e) =>
        {
            CatAnimation.StopAnimation();
            Application.Current.Shutdown();
        };
        SettingsPanel.OpacityChanged += (s, opacity) => CatAnimation.SetOpacity(opacity);
        SettingsPanel.SnappingEnabledChanged += (s, enabled) => IsSnappingEnabled = enabled;
        SettingsPanel.SnapDistanceChanged += (s, distance) => SnapDistance = distance;
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
                _isDragging = true;
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                _isDragging = false;
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

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);

        // Check if Alt key is currently pressed using Keyboard.IsKeyDown
        var isAltPressed = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        // Only snap if dragging, snapping is enabled, and Alt is not pressed
        if (_isDragging && IsSnappingEnabled && !isAltPressed)
        {
            PerformEdgeSnapping();
        }
    }

    private void PerformEdgeSnapping()
    {
        var workArea = SystemParameters.WorkArea;
        var currentLeft = Left;
        var currentTop = Top;
        var windowRight = Left + Width;
        var windowBottom = Top + Height;

        var newLeft = currentLeft;
        var newTop = currentTop;

        // Snap to left edge
        if (Math.Abs(currentLeft - workArea.Left) < SnapDistance)
        {
            newLeft = workArea.Left;
        }
        // Snap to right edge
        else if (Math.Abs(windowRight - workArea.Right) < SnapDistance)
        {
            newLeft = workArea.Right - Width;
        }

        // Snap to top edge
        if (Math.Abs(currentTop - workArea.Top) < SnapDistance)
        {
            newTop = workArea.Top;
        }
        // Snap to bottom edge
        else if (Math.Abs(windowBottom - workArea.Bottom) < SnapDistance)
        {
            newTop = workArea.Bottom - Height;
        }

        // Apply snapped position if changed
        if (newLeft != currentLeft || newTop != currentTop)
        {
            Left = newLeft;
            Top = newTop;
        }
    }

    private void ToggleSettingsPanel()
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Collapsed
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Hide hotkeys panel when showing settings
        if (SettingsPanel.Visibility == Visibility.Visible)
        {
            HotkeysPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void ToggleHotkeysPanel()
    {
        HotkeysPanel.Visibility = HotkeysPanel.Visibility == Visibility.Collapsed
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Hide settings panel when showing hotkeys
        if (HotkeysPanel.Visibility == Visibility.Visible)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
        }
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