using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FFMpegCore;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace VibeCat;

public partial class MainWindow : Window
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;
    
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private DispatcherTimer? _frameTimer;
    private int _currentFrame = 0;
    private List<BitmapSource> _frames = new();
    private bool _isClickThrough = true; // Start in click-through mode
    private DateTime _lastClickTime = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        
        // Configure FFmpeg paths on startup
        ConfigureFFmpeg();
        
        // Set up mouse interactions
        this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        
        // Load the actual cat video
        _ = LoadCatVideoAsync();
    }
    
    private void ConfigureFFmpeg()
    {
        // Set FFmpeg location to the included binaries
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var parent = Directory.GetParent(baseDir);
        if (parent?.Parent?.Parent != null)
        {
            var ffmpegPath = Path.Combine(parent.Parent.Parent.FullName, "ffmpeg-bin");
            FFMpegCore.GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
        }
    }
    
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Detect double-click
        var now = DateTime.Now;
        if ((now - _lastClickTime).TotalMilliseconds < 300)
        {
            // Double click detected - toggle UI visibility
            ToggleUIVisibility();
        }
        else if (!_isClickThrough)
        {
            // Single click - allow dragging if not in click-through mode
            this.DragMove();
        }
        _lastClickTime = now;
    }
    
    private void ToggleUIVisibility()
    {
        _isClickThrough = !_isClickThrough;
        
        if (_isClickThrough)
        {
            // Hide UI and enable click-through
            CloseButton.Visibility = Visibility.Collapsed;
            SetClickThrough(true);
        }
        else
        {
            // Show UI and disable click-through
            CloseButton.Visibility = Visibility.Visible;
            SetClickThrough(false);
        }
    }

    private async Task LoadCatVideoAsync()
    {
        string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "cat-vibing.mp4");
        var processor = new Services.VideoProcessor();
        _frames = await processor.ExtractFramesAsync(videoPath, 60);
        StartVideoPlayback();
    }


    private void StartVideoPlayback()
    {
        _frameTimer = new DispatcherTimer();
        _frameTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30 FPS
        _frameTimer.Tick += (s, e) =>
        {
            if (_frames.Count > 0)
            {
                VideoDisplay.Source = _frames[_currentFrame];
                _currentFrame = (_currentFrame + 1) % _frames.Count;
            }
        };
        _frameTimer.Start();
    }


    public void SetClickThrough(bool clickThrough)
    {
        _isClickThrough = clickThrough;
        
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (clickThrough)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                CloseButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                CloseButton.Visibility = Visibility.Visible;
            }
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        // Get window handle
        var hwnd = new WindowInteropHelper(this).Handle;
        
        // Add layered and no-activate extended styles
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_NOACTIVATE);
        
        // Enable click-through after window is initialized
        SetClickThrough(true);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _frameTimer?.Stop();
        Application.Current.Shutdown();
    }
}