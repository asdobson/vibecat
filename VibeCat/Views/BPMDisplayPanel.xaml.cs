using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VibeCat.BPMDetection;
using VibeCat.BPMDetection.Core;

namespace VibeCat.Views;

public partial class BPMDisplayPanel : UserControl
{
    private BPMDetectionService? bpmService;
    private DispatcherTimer? beatTimer;
    private Storyboard? beatPulseAnimation;
    private ObservableCollection<AlgorithmResult> algorithmResults;

    public BPMDisplayPanel()
    {
        InitializeComponent();
        algorithmResults = new ObservableCollection<AlgorithmResult>();
        AlgorithmsList.ItemsSource = algorithmResults;
        beatPulseAnimation = Resources["BeatPulse"] as Storyboard;
    }

    public void Initialize(BPMDetectionService service)
    {
        bpmService = service;
        bpmService.BPMUpdated += OnBPMUpdated;
        bpmService.ErrorOccurred += OnErrorOccurred;
    }

    private void OnBPMUpdated(object? sender, BPMUpdateEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateConsensusDisplay(e.ConsensusBPM, e.Estimates);
            UpdateAlgorithmsList(e.Estimates);
            UpdateBeatIndicator(e.ConsensusBPM);
        });
    }

    private void UpdateConsensusDisplay(float consensusBPM, Dictionary<string, BPMEstimate> estimates)
    {
        ConsensusBPM.Text = consensusBPM > 0 ? consensusBPM.ToString("F1") : "---.--";
        ConsensusConfidence.Value = estimates.TryGetValue("Consensus", out var consensus) ? consensus.Confidence : 0;
    }

    private void UpdateAlgorithmsList(Dictionary<string, BPMEstimate> estimates)
    {
        var nonConsensusEstimates = estimates
            .Where(kvp => kvp.Key != "Consensus")
            .OrderBy(kvp => kvp.Key);

        algorithmResults.Clear();

        foreach (var kvp in nonConsensusEstimates)
        {
            var result = new AlgorithmResult
            {
                AlgorithmName = kvp.Key,
                BPM = kvp.Value.BPM,
                Confidence = kvp.Value.Confidence
            };

            algorithmResults.Add(result);
        }

        if (algorithmResults.Count == 0)
        {
            algorithmResults.Add(new AlgorithmResult
            {
                AlgorithmName = "Waiting for audio...",
                BPM = 0,
                Confidence = 0
            });
        }
    }

    private void UpdateBeatIndicator(float bpm)
    {
        beatTimer?.Stop();

        if (bpm > 0 && bpm < 300)
        {
            beatTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60000.0 / bpm) };
            beatTimer.Tick += (s, e) => beatPulseAnimation?.Begin(BeatIndicator);
            beatTimer.Start();
        }
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        Dispatcher.Invoke(() =>
        {
            algorithmResults.Clear();
            algorithmResults.Add(new AlgorithmResult
            {
                AlgorithmName = "Error",
                BPM = 0,
                Confidence = 0
            });
        });
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        algorithmResults.Clear();
        ConsensusBPM.Text = "---.--";
        ConsensusConfidence.Value = 0;
        beatTimer?.Stop();
    }

    public void Cleanup()
    {
        beatTimer?.Stop();
        if (bpmService != null)
        {
            bpmService.BPMUpdated -= OnBPMUpdated;
            bpmService.ErrorOccurred -= OnErrorOccurred;
        }
    }
}

public class AlgorithmResult : INotifyPropertyChanged
{
    private string algorithmName = string.Empty;
    private float bpm;
    private float confidence;

    public string AlgorithmName
    {
        get => algorithmName;
        set
        {
            algorithmName = value;
            OnPropertyChanged();
        }
    }

    public float BPM
    {
        get => bpm;
        set
        {
            bpm = value;
            OnPropertyChanged();
        }
    }

    public float Confidence
    {
        get => confidence;
        set
        {
            confidence = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsHighConfidence));
            OnPropertyChanged(nameof(IsMediumConfidence));
            OnPropertyChanged(nameof(IsLowConfidence));
        }
    }

    public bool IsHighConfidence => Confidence >= 0.7f;
    public bool IsMediumConfidence => Confidence >= 0.4f && Confidence < 0.7f;
    public bool IsLowConfidence => Confidence < 0.4f;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}