using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace VibeCat.Controls;

public partial class CatAnimationView : UserControl
{
    private const double BaseBPM = 115.0;  // ~21 bops in 11 seconds
    private Storyboard? _animationStoryboard;
    private readonly List<BitmapSource> _frames = new();

    public CatAnimationView()
    {
        InitializeComponent();
        LoadCatVideo();
    }

    public void SetOpacity(double opacity) => VideoDisplay.Opacity = opacity;

    public void SetFlipped(bool flipped) =>
        VideoDisplay.RenderTransform = flipped ? new ScaleTransform(-1, 1) : new ScaleTransform(1, 1);

    public void StopAnimation() => _animationStoryboard?.Stop();

    public void SetPlaybackSpeed(double bpm)
    {
        if (_animationStoryboard == null) return;

        _animationStoryboard.Pause(this);
        _animationStoryboard.SetSpeedRatio(this, bpm / BaseBPM);
        _animationStoryboard.Resume(this);
    }

    private void LoadCatVideo()
    {
        var framesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "frames");
        foreach (var file in Directory.GetFiles(framesPath, "*.png").OrderBy(f => f))
        {
            var bitmap = new BitmapImage(new Uri(file));
            bitmap.Freeze();
            _frames.Add(bitmap);
        }

        if (_frames.Count == 0) return;

        var animation = new ObjectAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
        for (int i = 0; i < _frames.Count; i++)
            animation.KeyFrames.Add(new DiscreteObjectKeyFrame(_frames[i], TimeSpan.FromMilliseconds(i * 33.33)));

        Storyboard.SetTarget(animation, VideoDisplay);
        Storyboard.SetTargetProperty(animation, new PropertyPath(Image.SourceProperty));

        (_animationStoryboard = new Storyboard()).Children.Add(animation);
        _animationStoryboard.Begin(this, true);
    }
}