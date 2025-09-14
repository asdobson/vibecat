using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Threading;
using VibeCat.BPMDetection.Core;

namespace VibeCat.BPMDetection.Audio;

public class AudioCaptureManager : IDisposable
{
    private WasapiLoopbackCapture? loopbackCapture;
    private readonly CircularBuffer<float> audioBuffer;
    private readonly object bufferLock = new();
    private bool isCapturing;

    public const int BufferSize = 8192;
    public const int SampleRate = 44100;

    public event EventHandler<AudioDataEventArgs>? DataAvailable;
    public bool IsCapturing => isCapturing;

    public AudioCaptureManager()
    {
        audioBuffer = new CircularBuffer<float>(BufferSize * 2);
    }

    public void StartCapture()
    {
        if (isCapturing) return;

        try
        {
            loopbackCapture = new WasapiLoopbackCapture();

            loopbackCapture.DataAvailable += OnDataAvailable;
            loopbackCapture.RecordingStopped += OnRecordingStopped;

            loopbackCapture.StartRecording();
            isCapturing = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start audio capture: {ex.Message}", ex);
        }
    }

    public void StopCapture()
    {
        if (!isCapturing) return;

        loopbackCapture?.StopRecording();
        isCapturing = false;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;

        float[] samples = ConvertBytesToFloat(e.Buffer, e.BytesRecorded, loopbackCapture!.WaveFormat);

        lock (bufferLock)
        {
            audioBuffer.AddRange(samples);
        }

        if (audioBuffer.Count >= BufferSize)
        {
            AudioBuffer buffer;
            lock (bufferLock)
            {
                var bufferData = new float[BufferSize];
                for (int i = 0; i < BufferSize; i++)
                {
                    bufferData[i] = audioBuffer[audioBuffer.Count - BufferSize + i];
                }

                buffer = new AudioBuffer(BufferSize, loopbackCapture.WaveFormat.SampleRate, loopbackCapture.WaveFormat.Channels)
                {
                    Samples = bufferData
                };
            }

            DataAvailable?.Invoke(this, new AudioDataEventArgs { Buffer = buffer });
        }
    }

    private float[] ConvertBytesToFloat(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        int bytesPerSample = format.BitsPerSample / 8;
        int sampleCount = bytesRecorded / bytesPerSample;
        float[] samples = new float[sampleCount];

        if (format.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            Buffer.BlockCopy(buffer, 0, samples, 0, bytesRecorded);
        }
        else if (format.Encoding == WaveFormatEncoding.Pcm)
        {
            if (format.BitsPerSample == 16)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = BitConverter.ToInt16(buffer, i * 2);
                    samples[i] = sample / 32768f;
                }
            }
            else if (format.BitsPerSample == 24)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    int sample = (buffer[i * 3] | (buffer[i * 3 + 1] << 8) | (buffer[i * 3 + 2] << 16));
                    if ((sample & 0x800000) != 0)
                        sample |= unchecked((int)0xFF000000);
                    samples[i] = sample / 8388608f;
                }
            }
            else if (format.BitsPerSample == 32)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    int sample = BitConverter.ToInt32(buffer, i * 4);
                    samples[i] = sample / 2147483648f;
                }
            }
        }

        return samples;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
            DataAvailable?.Invoke(this, new AudioDataEventArgs { Buffer = null });
    }

    public void Dispose()
    {
        StopCapture();
        loopbackCapture?.Dispose();
    }
}

public class AudioDataEventArgs : EventArgs
{
    public AudioBuffer Buffer { get; set; } = null!;
}