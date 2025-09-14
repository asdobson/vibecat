namespace VibeCat.BPMDetection.Core;

public class BPMEstimate
{
    public float BPM { get; set; }
    public float Confidence { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public float Phase { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<float> AlternativeBPMs { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public bool IsValid => BPM > 0 && BPM < 300 && Confidence > 0;

    public bool IsOctaveRelated(BPMEstimate other, float tolerance = 0.03f)
    {
        if (other == null) return false;

        float ratio = BPM / other.BPM;
        float[] octaveRatios = { 0.5f, 1.0f, 2.0f, 0.33f, 3.0f, 0.66f, 1.5f };

        return octaveRatios.Any(r => Math.Abs(ratio - r) < tolerance);
    }
}