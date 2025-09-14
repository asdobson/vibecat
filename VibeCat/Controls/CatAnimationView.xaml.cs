using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace VibeCat.Controls;

public partial class CatAnimationView : UserControl
{
    private const int FrameRate = 30;
    private Storyboard? _animationStoryboard;
    private readonly List<BitmapSource> _frames = new();

    public CatAnimationView()
    {
        InitializeComponent();
        _ = LoadCatVideoAsync();
    }

    public void SetOpacity(double opacity)
    {
        VideoDisplay.Opacity = opacity;
    }

    public void SetFlipped(bool flipped)
    {
        if (VideoDisplay != null)
        {
            VideoDisplay.RenderTransform = flipped
                ? new ScaleTransform(-1, 1)
                : new ScaleTransform(1, 1);
        }
    }

    public void StopAnimation()
    {
        _animationStoryboard?.Stop();
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
            frameTime += 1000.0 / FrameRate;
        }

        Storyboard.SetTarget(animation, VideoDisplay);
        Storyboard.SetTargetProperty(animation, new PropertyPath(Image.SourceProperty));

        _animationStoryboard = new Storyboard();
        _animationStoryboard.Children.Add(animation);
        _animationStoryboard.Begin(this);
    }
}