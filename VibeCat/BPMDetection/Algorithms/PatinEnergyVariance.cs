using System;
using System.Collections.Generic;
using System.Linq;
using VibeCat.BPMDetection.Audio;
using VibeCat.BPMDetection.Core;

namespace VibeCat.BPMDetection.Algorithms;

public class PatinEnergyVariance : IBPMAlgorithm
{
    public string Name => "Patin Energy Variance";
    public string Description => "Energy variance beat detection";

    private const int BufferSize = 1024;
    private const int HistorySize = 43;
    private const float MinVarianceThreshold = 0.0025f;
    private const float MaxVarianceThreshold = 0.02f;

    private readonly CircularBuffer<double> energyHistory;
    private readonly List<double> beatTimes;
    private readonly List<double> beatIntervals;
    private double lastBeatTime;
    private double totalTime;
    private int sampleCount;

    public bool RequiresFullAudio => false;
    public int MinimumSamples => BufferSize * HistorySize;

    public PatinEnergyVariance()
    {
        energyHistory = new CircularBuffer<double>(HistorySize);
        beatTimes = new List<double>();
        beatIntervals = new List<double>();
        lastBeatTime = 0;
        totalTime = 0;
        sampleCount = 0;
    }

    public BPMEstimate EstimateBPM(AudioBuffer buffer)
    {
        var mono = buffer.GetMono();
        var estimate = new BPMEstimate
        {
            Algorithm = Name,
            Timestamp = DateTime.Now
        };

        ProcessAudioFrames(mono, buffer.SampleRate, estimate);

        return estimate;
    }

    private void ProcessAudioFrames(float[] samples, int sampleRate, BPMEstimate estimate)
    {
        int frameCount = samples.Length / BufferSize;

        for (int frame = 0; frame < frameCount; frame++)
        {
            int startIdx = frame * BufferSize;
            int endIdx = Math.Min(startIdx + BufferSize, samples.Length);
            int frameSize = endIdx - startIdx;

            double instantEnergy = CalculateEnergy(samples, startIdx, frameSize);

            energyHistory.Add(instantEnergy);

            if (energyHistory.Count >= HistorySize)
            {
                double averageEnergy = energyHistory.Average(e => e);
                double variance = energyHistory.Variance(e => e);

                double C = CalculateAdaptiveThreshold(variance);

                if (instantEnergy > C * averageEnergy)
                {
                    double currentTime = totalTime + (frame * BufferSize / (double)sampleRate);

                    if (currentTime - lastBeatTime > 0.25)
                    {
                        beatTimes.Add(currentTime);

                        if (lastBeatTime > 0)
                        {
                            double interval = currentTime - lastBeatTime;
                            beatIntervals.Add(interval);
                        }

                        lastBeatTime = currentTime;
                    }
                }
            }
        }

        totalTime += samples.Length / (double)sampleRate;
        sampleCount += samples.Length;

        CalculateBPM(estimate);
    }

    private double CalculateEnergy(float[] samples, int start, int length) =>
        Enumerable.Range(0, length).Sum(i => (double)(samples[start + i] * samples[start + i]));

    private double CalculateAdaptiveThreshold(double variance) =>
        variance < MinVarianceThreshold ? 1.55 :
        variance < 0.01 ? 1.4 :
        variance < MaxVarianceThreshold ? 1.3 - 10 * variance :
        1.25;

    private void CalculateBPM(BPMEstimate estimate)
    {
        if (beatIntervals.Count < 5)
        {
            estimate.BPM = 0;
            estimate.Confidence = 0;
            return;
        }

        var recentIntervals = beatIntervals.TakeLast(20).ToList();

        recentIntervals.Sort();
        int removeCount = (int)(recentIntervals.Count * 0.1);
        if (removeCount > 0)
        {
            recentIntervals.RemoveRange(0, removeCount);
            recentIntervals.RemoveRange(recentIntervals.Count - removeCount, removeCount);
        }

        if (recentIntervals.Count == 0)
        {
            estimate.BPM = 0;
            estimate.Confidence = 0;
            return;
        }

        double medianInterval = recentIntervals[recentIntervals.Count / 2];
        double bpm = 60.0 / medianInterval;

        double stdDev = CalculateStandardDeviation(recentIntervals);
        double coefficientOfVariation = stdDev / medianInterval;

        float confidence = Math.Max(0, Math.Min(1, 1.0f - (float)coefficientOfVariation));

        while (bpm < 60 && confidence > 0.5)
        {
            bpm *= 2;
        }
        while (bpm > 180 && confidence > 0.5)
        {
            bpm /= 2;
        }

        estimate.BPM = (float)bpm;
        estimate.Confidence = confidence;

        estimate.AlternativeBPMs.Clear();
        estimate.AlternativeBPMs.Add((float)(bpm * 2));
        estimate.AlternativeBPMs.Add((float)(bpm / 2));

        estimate.Metadata["BeatCount"] = beatTimes.Count;
        estimate.Metadata["Variance"] = energyHistory.Count > 0 ? energyHistory.Variance(e => e) : 0;
    }

    private double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count <= 1) return 0;

        double mean = values.Average();
        double sumSquaredDiff = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquaredDiff / (values.Count - 1));
    }

    public void Reset()
    {
        energyHistory.Clear();
        beatTimes.Clear();
        beatIntervals.Clear();
        lastBeatTime = 0;
        totalTime = 0;
        sampleCount = 0;
    }
}