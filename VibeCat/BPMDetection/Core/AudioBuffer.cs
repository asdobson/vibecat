namespace VibeCat.BPMDetection.Core;

public class AudioBuffer
{
    public float[] Samples { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int FrameSize { get; set; }
    public int HopSize { get; set; }

    public AudioBuffer(int capacity, int sampleRate, int channels = 1)
    {
        Samples = new float[capacity];
        SampleRate = sampleRate;
        Channels = channels;
        FrameSize = 2048;
        HopSize = 512;
    }

    public float[] GetMono()
    {
        if (Channels == 1) return Samples;

        var mono = new float[Samples.Length / Channels];
        for (int i = 0; i < mono.Length; i++)
        {
            float sum = 0;
            for (int ch = 0; ch < Channels; ch++)
            {
                sum += Samples[i * Channels + ch];
            }
            mono[i] = sum / Channels;
        }
        return mono;
    }

    public double GetEnergy() => Samples.Sum(s => s * s);

    public double GetRMS() => Math.Sqrt(GetEnergy() / Samples.Length);
}