using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VibeCat.BPMDetection.Algorithms;
using VibeCat.BPMDetection.Audio;
using VibeCat.BPMDetection.Core;

namespace VibeCat.BPMDetection;

public class BPMDetectionService : IDisposable
{
    private readonly AudioCaptureManager audioCaptureManager;
    private readonly List<IBPMAlgorithm> algorithms;
    private readonly Dictionary<string, BPMEstimate> currentEstimates;
    private bool isRunning;

    public event EventHandler<BPMUpdateEventArgs>? BPMUpdated;
    public event EventHandler<string>? ErrorOccurred;

    public IReadOnlyDictionary<string, BPMEstimate> CurrentEstimates => currentEstimates;
    public bool IsRunning => isRunning;

    public BPMDetectionService()
    {
        audioCaptureManager = new AudioCaptureManager();
        algorithms = new List<IBPMAlgorithm>();
        currentEstimates = new Dictionary<string, BPMEstimate>();

        InitializeAlgorithms();
        audioCaptureManager.DataAvailable += OnAudioDataAvailable;
    }

    private void InitializeAlgorithms()
    {
        algorithms.Add(new PatinEnergyVariance());
    }

    public void Start()
    {
        if (isRunning) return;

        try
        {
            algorithms.ForEach(a => a.Reset());
            currentEstimates.Clear();
            audioCaptureManager.StartCapture();
            isRunning = true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to start BPM detection: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (!isRunning) return;
        audioCaptureManager.StopCapture();
        isRunning = false;
    }

    private async void OnAudioDataAvailable(object? sender, AudioDataEventArgs e)
    {
        var tasks = algorithms.Select(algo => Task.Run(() =>
        {
            try
            {
                return algo.EstimateBPM(e.Buffer);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Error in {algo.Name}: {ex.Message}");
                return new BPMEstimate
                {
                    Algorithm = algo.Name,
                    BPM = 0,
                    Confidence = 0
                };
            }
        }));

        var estimates = await Task.WhenAll(tasks);

        foreach (var estimate in estimates)
        {
            if (estimate.IsValid)
            {
                currentEstimates[estimate.Algorithm] = estimate;
            }
        }

        var consensus = CalculateConsensus();
        if (consensus != null)
        {
            currentEstimates["Consensus"] = consensus;
        }

        BPMUpdated?.Invoke(this, new BPMUpdateEventArgs
        {
            Estimates = new Dictionary<string, BPMEstimate>(currentEstimates),
            ConsensusBPM = consensus?.BPM ?? 0
        });
    }

    private BPMEstimate? CalculateConsensus()
    {
        var validEstimates = currentEstimates.Values
            .Where(e => e.IsValid && e.Algorithm != "Consensus")
            .ToList();

        if (validEstimates.Count == 0) return null;

        if (validEstimates.Count == 1)
        {
            return new BPMEstimate
            {
                Algorithm = "Consensus",
                BPM = validEstimates[0].BPM,
                Confidence = validEstimates[0].Confidence * 0.5f,
                Timestamp = DateTime.Now
            };
        }

        var clusters = ClusterBPMs(validEstimates);
        var bestCluster = clusters.OrderByDescending(c => c.TotalConfidence).FirstOrDefault();

        if (bestCluster == null) return null;

        float weightedSum = 0;
        float totalWeight = 0;

        foreach (var estimate in bestCluster.Estimates)
        {
            float weight = estimate.Confidence;
            weightedSum += estimate.BPM * weight;
            totalWeight += weight;
        }

        return new BPMEstimate
        {
            Algorithm = "Consensus",
            BPM = weightedSum / totalWeight,
            Confidence = bestCluster.TotalConfidence / bestCluster.Estimates.Count,
            Timestamp = DateTime.Now,
            Metadata = new Dictionary<string, object>
            {
                ["ClusterSize"] = bestCluster.Estimates.Count,
                ["Algorithms"] = string.Join(", ", bestCluster.Estimates.Select(e => e.Algorithm))
            }
        };
    }

    private List<BPMCluster> ClusterBPMs(List<BPMEstimate> estimates, float tolerance = 0.05f)
    {
        var clusters = new List<BPMCluster>();

        foreach (var estimate in estimates)
        {
            bool added = false;

            foreach (var cluster in clusters)
            {
                float ratio = estimate.BPM / cluster.CenterBPM;
                if (Math.Abs(ratio - 1) < tolerance ||
                    Math.Abs(ratio - 2) < tolerance ||
                    Math.Abs(ratio - 0.5) < tolerance)
                {
                    cluster.AddEstimate(estimate);
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                clusters.Add(new BPMCluster(estimate));
            }
        }

        return clusters;
    }

    public void Dispose()
    {
        Stop();
        audioCaptureManager?.Dispose();
    }

    private class BPMCluster
    {
        public List<BPMEstimate> Estimates { get; }
        public float CenterBPM { get; private set; }
        public float TotalConfidence { get; private set; }

        public BPMCluster(BPMEstimate initial)
        {
            Estimates = new List<BPMEstimate> { initial };
            CenterBPM = initial.BPM;
            TotalConfidence = initial.Confidence;
        }

        public void AddEstimate(BPMEstimate estimate)
        {
            Estimates.Add(estimate);

            float adjustedBPM = estimate.BPM;
            if (Math.Abs(estimate.BPM / CenterBPM - 2) < 0.1f)
                adjustedBPM = estimate.BPM / 2;
            else if (Math.Abs(estimate.BPM / CenterBPM - 0.5f) < 0.1f)
                adjustedBPM = estimate.BPM * 2;

            CenterBPM = (CenterBPM * (Estimates.Count - 1) + adjustedBPM) / Estimates.Count;
            TotalConfidence += estimate.Confidence;
        }
    }
}

public class BPMUpdateEventArgs : EventArgs
{
    public Dictionary<string, BPMEstimate> Estimates { get; set; } = new();
    public float ConsensusBPM { get; set; }
}