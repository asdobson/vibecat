using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;
using VibeCat.Services;
using SpotifyAPI.Web;

namespace VibeCat;

public partial class MainWindow : Window
{
    private const double AspectRatio = 4.0 / 3.0;
    private const int FadeAnimationDuration = 200;
    private const int DoubleClickThreshold = 300;
    private const double MinimumWindowWidth = 266;

    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int GWL_EXSTYLE = -20;

    private const int HOTKEY_ID = 9000;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const uint VK_T = 0x54;
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private DateTime _lastClickTime = DateTime.MinValue;
    private IntPtr _windowHandle;
    private bool _isUIMode = false;
    private bool _isDragging = false;
    private bool _isClickThrough = false;
    private TaskbarIcon? _trayIcon;
    private MenuItem? _clickThroughMenuItem;
    private SpotifyService _spotifyService;
    private bool _spotifySyncEnabled = false;

    public bool IsSnappingEnabled { get; set; } = true;
    public double SnapDistance { get; set; } = 20;
    public bool IsAutoFlipEnabled { get; set; } = true;
    public bool IsFlipped { get; set; } = false;

    public bool IsClickThrough
    {
        get => _isClickThrough;
        set
        {
            _isClickThrough = value;
            UpdateClickThroughState();
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += Window_MouseLeftButtonDown;
        _spotifyService = new SpotifyService();
        SetupEventHandlers();
        SetupSystemTray();
        SetupSpotifyService();
    }

    private void SetupEventHandlers()
    {
        TitleBar.SettingsClicked += (s, e) => TogglePanel(SettingsPanel, HotkeysPanel);
        TitleBar.HotkeysClicked += (s, e) => TogglePanel(HotkeysPanel, SettingsPanel);
        TitleBar.MinimizeClicked += (s, e) => WindowState = WindowState.Minimized;
        TitleBar.CloseClicked += (s, e) =>
        {
            CatAnimation.StopAnimation();
            Application.Current.Shutdown();
        };
        SettingsPanel.OpacityChanged += (s, opacity) => CatAnimation.SetOpacity(opacity);
        SettingsPanel.SnappingEnabledChanged += (s, enabled) => IsSnappingEnabled = enabled;
        SettingsPanel.SnapDistanceChanged += (s, distance) => SnapDistance = distance;
        SettingsPanel.AutoFlipEnabledChanged += (s, enabled) => IsAutoFlipEnabled = enabled;
        SettingsPanel.ManualFlipRequested += (s, e) => ToggleFlip();
        SettingsPanel.BPMChanged += (s, bpm) => { if (!_spotifySyncEnabled) CatAnimation.SetPlaybackSpeed(bpm); };
        SettingsPanel.ClickThroughChanged += (s, enabled) => IsClickThrough = enabled;
        SettingsPanel.SpotifyConnectRequested += async (s, e) => await ConnectSpotifyAsync();
        SettingsPanel.SpotifyDisconnectRequested += (s, e) => DisconnectSpotify();
        SettingsPanel.SpotifySyncEnabledChanged += (s, enabled) => SetSpotifySyncEnabled(enabled);
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
            UIPanel.Visibility = Visibility.Visible;
            BackgroundOverlay.Visibility = Visibility.Visible;
            ShowInTaskbar = true;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(FadeAnimationDuration));
            UIPanel.BeginAnimation(OpacityProperty, fadeIn);
            BackgroundOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }
        else
        {
            ShowInTaskbar = false;

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(FadeAnimationDuration));
            fadeOut.Completed += (s, e) =>
            {
                UIPanel.Visibility = Visibility.Collapsed;
                BackgroundOverlay.Visibility = Visibility.Collapsed;
            };
            UIPanel.BeginAnimation(OpacityProperty, fadeOut);
            BackgroundOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }
    }



    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(_windowHandle, GWL_EXSTYLE);
        SetWindowLong(_windowHandle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_NOACTIVATE);

        RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_T);
        HwndSource source = HwndSource.FromHwnd(_windowHandle);
        source.AddHook(WndProc);
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);

        var isAltPressed = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

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

        var newLeft = currentLeft;
        var newTop = currentTop;
        var snappedToLeftEdge = false;
        var snappedToRightEdge = false;

        if (Math.Abs(currentLeft - workArea.Left) < SnapDistance)
        {
            newLeft = workArea.Left;
            snappedToLeftEdge = true;
        }
        else if (Math.Abs(currentLeft + Width - workArea.Right) < SnapDistance)
        {
            newLeft = workArea.Right - Width;
            snappedToRightEdge = true;
        }

        if (Math.Abs(currentTop - workArea.Top) < SnapDistance)
        {
            newTop = workArea.Top;
        }
        else if (Math.Abs(currentTop + Height - workArea.Bottom) < SnapDistance)
        {
            newTop = workArea.Bottom - Height;
        }

        if (newLeft != currentLeft || newTop != currentTop)
        {
            Left = newLeft;
            Top = newTop;
        }

        if (IsAutoFlipEnabled && (snappedToLeftEdge || snappedToRightEdge))
        {
            SetFlipped(snappedToRightEdge);
        }
    }

    private void SetFlipped(bool flipped)
    {
        IsFlipped = flipped;
        CatAnimation.SetFlipped(flipped);
    }

    public void ToggleFlip()
    {
        SetFlipped(!IsFlipped);
    }

    private void TogglePanel(FrameworkElement panelToToggle, FrameworkElement panelToHide)
    {
        panelToToggle.Visibility = panelToToggle.Visibility == Visibility.Collapsed
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (panelToToggle.Visibility == Visibility.Visible)
        {
            panelToHide.Visibility = Visibility.Collapsed;
        }
    }

    private void HandleResize(DragDeltaEventArgs e)
    {
        var newWidth = Math.Max(MinimumWindowWidth, Width + e.HorizontalChange);
        Width = newWidth;
        Height = newWidth / AspectRatio;
    }

    private void SetupSystemTray()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "VibeCat",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(_isClickThrough
                ? "pack://application:,,,/Resources/tray-icon-outline.ico"
                : "pack://application:,,,/Resources/tray-icon.ico"))
        };

        var contextMenu = new ContextMenu();

        _clickThroughMenuItem = new MenuItem
        {
            Header = "Click-Through Mode",
            IsCheckable = true,
            IsChecked = _isClickThrough
        };
        _clickThroughMenuItem.Click += (s, e) => IsClickThrough = _clickThroughMenuItem.IsChecked;

        contextMenu.Items.Add(_clickThroughMenuItem);
        contextMenu.Items.Add(new Separator());

        var exitMenuItem = new MenuItem { Header = "Exit" };
        exitMenuItem.Click += (s, e) =>
        {
            CatAnimation.StopAnimation();
            Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitMenuItem);

        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayLeftMouseDown += (s, e) => IsClickThrough = !IsClickThrough;
    }

    private void UpdateClickThroughState()
    {
        if (_windowHandle == IntPtr.Zero) return;

        var style = GetWindowLong(_windowHandle, GWL_EXSTYLE);

        if (_isClickThrough)
        {
            SetWindowLong(_windowHandle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);

            if (_isUIMode)
            {
                _isUIMode = false;
                UIPanel.Visibility = Visibility.Collapsed;
                BackgroundOverlay.Visibility = Visibility.Collapsed;
                ShowInTaskbar = false;
            }
        }
        else
        {
            SetWindowLong(_windowHandle, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
        }

        if (_clickThroughMenuItem != null)
        {
            _clickThroughMenuItem.IsChecked = _isClickThrough;
        }

        if (_trayIcon != null)
        {
            _trayIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(_isClickThrough
                ? "pack://application:,,,/Resources/tray-icon-outline.ico"
                : "pack://application:,,,/Resources/tray-icon.ico"));
            _trayIcon.ToolTipText = $"VibeCat - Click-Through: {(_isClickThrough ? "ON" : "OFF")}";
        }

        SettingsPanel?.UpdateClickThroughState(_isClickThrough);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            IsClickThrough = !IsClickThrough;
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }

        _trayIcon?.Dispose();
        _spotifyService?.Disconnect();

        base.OnClosed(e);
    }

    private void SetupSpotifyService()
    {
        SettingsPanel.SetSpotifyService(_spotifyService);
        _spotifyService.CurrentTrackChanged += async (_, context) =>
        {
            try
            {
                if (!_spotifySyncEnabled || context?.Item is not FullTrack track) return;
                var features = await _spotifyService.GetAudioFeaturesAsync(track.Id);
                if (features?.Tempo != null && features.Tempo > 0)
                    await Dispatcher.InvokeAsync(() => CatAnimation.SetPlaybackSpeed(features.Tempo));
            }
            catch { }
        };
    }

    private async Task ConnectSpotifyAsync()
    {
        if (!await _spotifyService.AuthenticateAsync())
            MessageBox.Show("Failed to connect to Spotify. Please try again.",
                          "Spotify Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void DisconnectSpotify()
    {
        _spotifyService.Disconnect();
        _spotifySyncEnabled = false;
    }

    private void SetSpotifySyncEnabled(bool enabled)
    {
        _spotifySyncEnabled = enabled;
        if (!enabled) CatAnimation.SetPlaybackSpeed(SettingsPanel.BPM);
    }
}