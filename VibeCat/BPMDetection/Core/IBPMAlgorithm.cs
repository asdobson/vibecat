namespace VibeCat.BPMDetection.Core;

public interface IBPMAlgorithm
{
    string Name { get; }
    string Description { get; }

    BPMEstimate EstimateBPM(AudioBuffer buffer);
    void Reset();
    bool RequiresFullAudio { get; }
    int MinimumSamples { get; }
}