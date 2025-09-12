using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
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

    private Storyboard? _animationStoryboard;
    private readonly List<BitmapSource> _frames = new();
    private bool _isClickThrough = true;
    private DateTime _lastClickTime = DateTime.MinValue;
    private IntPtr _windowHandle;

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += Window_MouseLeftButtonDown;
        _ = LoadCatVideoAsync();
    }
    
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.Now;
        if ((now - _lastClickTime).TotalMilliseconds < 300)
            ToggleClickThrough();
        else if (!_isClickThrough)
            DragMove();
        _lastClickTime = now;
    }
    
    private void ToggleClickThrough()
    {
        SetClickThrough(!_isClickThrough);
    }

    private async Task LoadCatVideoAsync()
    {
        var framesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "frames");
        var frameFiles = await Task.Run(() => 
            Directory.GetFiles(framesPath, "*.png").OrderBy(f => f).ToList());
        
        foreach (var frameFile in frameFiles)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(frameFile, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            _frames.Add(bitmap);
        }
        
        if (_frames.Count > 0)
            StartVideoPlayback();
    }


    private void StartVideoPlayback()
    {
        var animation = new ObjectAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
        
        double frameTime = 0;
        foreach (var frame in _frames)
        {
            animation.KeyFrames.Add(new DiscreteObjectKeyFrame(frame, TimeSpan.FromMilliseconds(frameTime)));
            frameTime += 1000.0 / 30.0;
        }
        
        Storyboard.SetTarget(animation, VideoDisplay);
        Storyboard.SetTargetProperty(animation, new PropertyPath(System.Windows.Controls.Image.SourceProperty));
        
        _animationStoryboard = new Storyboard();
        _animationStoryboard.Children.Add(animation);
        _animationStoryboard.Begin(this);
    }


    private void SetClickThrough(bool clickThrough)
    {
        _isClickThrough = clickThrough;
        CloseButton.Visibility = clickThrough ? Visibility.Collapsed : Visibility.Visible;
        
        if (_windowHandle != IntPtr.Zero)
        {
            var style = GetWindowLong(_windowHandle, GWL_EXSTYLE);
            SetWindowLong(_windowHandle, GWL_EXSTYLE, 
                clickThrough ? style | WS_EX_TRANSPARENT : style & ~WS_EX_TRANSPARENT);
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(_windowHandle, GWL_EXSTYLE);
        SetWindowLong(_windowHandle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_NOACTIVATE);
        SetClickThrough(true);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _animationStoryboard?.Stop();
        Application.Current.Shutdown();
    }
}