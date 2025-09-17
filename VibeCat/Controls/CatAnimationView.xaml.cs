using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace VibeCat.Controls;

public partial class CatAnimationView : UserControl
{
    private const double BaseBPM = 120.0;
    private const double MinBPM = 30.0;
    private const double MaxBPM = 180.0;
    private const int AnimationFrameMs = 16;

    private Storyboard? _animationStoryboard;
    private readonly List<BitmapSource> _frames = new();
    private double _baseOpacity = 1.0;
    private double _opacityMultiplier = 1.0;
    private System.Windows.Threading.DispatcherTimer? _fadeTimer;
    private double _currentSpeedRatio = 1.0;

    public CatAnimationView()
    {
        InitializeComponent();
        LoadCatVideo();
    }

    public void SetOpacity(double opacity)
    {
        _baseOpacity = opacity;
        UpdateDisplayOpacity();
    }

    public void SetFlipped(bool flipped) =>
        VideoDisplay.RenderTransform = flipped ? new ScaleTransform(-1, 1) : new ScaleTransform(1, 1);

    public void StopAnimation()
    {
        _fadeTimer?.Stop();
        _animationStoryboard?.Stop();
    }

    public void SetPlaybackSpeed(double bpm)
    {
        if (_animationStoryboard == null) return;

        var clampedBpm = Math.Max(60, Math.Min(MaxBPM, bpm));
        _currentSpeedRatio = clampedBpm / BaseBPM;
        _animationStoryboard.Pause(this);
        _animationStoryboard.SetSpeedRatio(this, _currentSpeedRatio);
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

    public void AnimateToState(double opacityMultiplier, double targetBpm, TimeSpan duration)
    {
        try
        {
            _fadeTimer?.Stop();

            if (_animationStoryboard == null) return;

            var startValues = (_opacityMultiplier, _currentSpeedRatio);
            var targetValues = (
                Math.Clamp(opacityMultiplier, 0, 1),
                Math.Clamp(targetBpm, MinBPM, MaxBPM) / BaseBPM
            );

            var startTime = DateTime.Now;
            _fadeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AnimationFrameMs)
            };

            _fadeTimer.Tick += (s, e) => UpdateAnimation(startTime, duration, startValues, targetValues);
            _fadeTimer.Start();
        }
        catch
        {
            ApplyTargetState(opacityMultiplier, targetBpm);
        }
    }

    private void UpdateAnimation(DateTime startTime, TimeSpan duration,
        (double opacity, double speed) start, (double opacity, double speed) target)
    {
        var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
        var progress = Math.Min(1.0, elapsed / duration.TotalMilliseconds);
        var easedProgress = EaseInOut(progress);

        _opacityMultiplier = Lerp(start.opacity, target.opacity, easedProgress);
        _currentSpeedRatio = Lerp(start.speed, target.speed, easedProgress);

        UpdateDisplayOpacity();
        UpdateAnimationSpeed(_currentSpeedRatio);

        if (progress >= 1.0)
        {
            _fadeTimer?.Stop();
            _opacityMultiplier = target.opacity;
            _currentSpeedRatio = target.speed;
            UpdateDisplayOpacity();
        }
    }

    private void UpdateAnimationSpeed(double speedRatio)
    {
        if (_animationStoryboard == null) return;
        _animationStoryboard.Pause(this);
        _animationStoryboard.SetSpeedRatio(this, speedRatio);
        _animationStoryboard.Resume(this);
    }

    private void ApplyTargetState(double opacityMultiplier, double targetBpm)
    {
        _opacityMultiplier = opacityMultiplier;
        UpdateDisplayOpacity();
        if (targetBpm > 0)
            SetPlaybackSpeed(targetBpm);
    }

    private static double Lerp(double start, double end, double t) => start + (end - start) * t;
    private static double EaseInOut(double t) => t * t * (3.0 - 2.0 * t);

    public void ResetFade()
    {
        _fadeTimer?.Stop();
        _opacityMultiplier = 1.0;
        UpdateDisplayOpacity();
        SetPlaybackSpeed(BaseBPM);
    }

    private void UpdateDisplayOpacity() =>
        VideoDisplay.Opacity = _baseOpacity * _opacityMultiplier;
}