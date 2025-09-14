using System.Linq;

namespace VibeCat.BPMDetection.Audio;

public class CircularBuffer<T>
{
    private readonly T[] buffer;
    private int writePos;
    private int count;

    public int Capacity => buffer.Length;
    public int Count => count;
    public bool IsFull => count == buffer.Length;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));

        buffer = new T[capacity];
        writePos = 0;
        count = 0;
    }

    public void Add(T item)
    {
        buffer[writePos] = item;
        writePos = (writePos + 1) % buffer.Length;
        if (count < buffer.Length)
            count++;
    }

    public void AddRange(T[] items)
    {
        foreach (var item in items) Add(item);
    }

    public T[] ToArray()
    {
        var result = new T[count];
        if (count == 0) return result;

        if (count < buffer.Length)
        {
            Array.Copy(buffer, 0, result, 0, count);
        }
        else
        {
            int firstPartLength = buffer.Length - writePos;
            Array.Copy(buffer, writePos, result, 0, firstPartLength);
            Array.Copy(buffer, 0, result, firstPartLength, writePos);
        }

        return result;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= count)
                throw new IndexOutOfRangeException();

            return count < buffer.Length ? buffer[index] : buffer[(writePos + index) % buffer.Length];
        }
    }

    public void Clear()
    {
        Array.Clear(buffer, 0, buffer.Length);
        writePos = 0;
        count = 0;
    }

    public double Average(Func<T, double> selector) =>
        count == 0 ? 0 : Enumerable.Range(0, count).Sum(i => selector(this[i])) / count;

    public double Variance(Func<T, double> selector)
    {
        if (count <= 1) return 0;

        double mean = Average(selector);
        double sumSquaredDiff = 0;

        for (int i = 0; i < count; i++)
        {
            double value = selector(this[i]);
            double diff = value - mean;
            sumSquaredDiff += diff * diff;
        }

        return sumSquaredDiff / (count - 1);
    }
}