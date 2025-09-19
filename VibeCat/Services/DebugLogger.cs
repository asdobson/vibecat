using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VibeCat.Services;

public static class DebugLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VibeCat"
    );

    private static readonly string LogFilePath = Path.Combine(LogDirectory, "debug.log");
    private static readonly object LogLock = new();
    private static bool _isEnabled = false;
    private const long MaxLogSize = 10 * 1024 * 1024; // 10MB
    private static readonly BlockingCollection<string> _logQueue = new();
    private static Task? _writerTask;
    private static CancellationTokenSource? _cancellationTokenSource;

    public static bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (value)
            {
                // Ensure directory exists before first write
                try
                {
                    if (!Directory.Exists(LogDirectory))
                        Directory.CreateDirectory(LogDirectory);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to create log directory: {ex.Message}\nPath: {LogDirectory}",
                                                   "Debug Logger Error",
                                                   System.Windows.MessageBoxButton.OK,
                                                   System.Windows.MessageBoxImage.Error);
                    _isEnabled = false;
                    return;
                }

                StartWriterTask();

                Info("DebugLogger", () => $"Debug logging enabled - VibeCat v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                Info("DebugLogger", () => $"OS: {Environment.OSVersion} | .NET: {Environment.Version} | 64-bit: {Environment.Is64BitProcess}");
                Info("DebugLogger", () => $"Machine: {Environment.MachineName} | Processors: {Environment.ProcessorCount}");
                Info("DebugLogger", () => $"Log file: {LogFilePath}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string source, string message, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        Write("INFO", source, message, caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string source, Func<string> messageFactory, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        Write("INFO", source, messageFactory(), caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string source, string message, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        Write("DEBUG", source, message, caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string source, Func<string> messageFactory, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        Write("DEBUG", source, messageFactory(), caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string source, string message, Exception? ex = null, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        var fullMessage = ex != null
            ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message} | StackTrace: {ex.StackTrace}"
            : message;
        Write("ERROR", source, fullMessage, caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string source, Func<string> messageFactory, Exception? ex = null, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        var message = messageFactory();
        var fullMessage = ex != null
            ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message} | StackTrace: {ex.StackTrace}"
            : message;
        Write("ERROR", source, fullMessage, caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(string source, string message, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        Write("WARN", source, message, caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(string source, Func<string> messageFactory, [CallerMemberName] string? caller = null)
    {
        if (!_isEnabled) return;
        Write("WARN", source, messageFactory(), caller);
    }

    private static void Write(string level, string source, string message, string? caller)
    {
        if (!_isEnabled) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var threadId = Thread.CurrentThread.ManagedThreadId;
        var callerInfo = caller != null ? $".{caller}" : "";
        var logLine = $"[{timestamp}] [{level,-5}] [T{threadId:D3}] [{source}{callerInfo}] {message}";

        _logQueue.TryAdd(logLine);
    }

    private static void StartWriterTask()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _writerTask = Task.Run(() => WriteLoop(_cancellationTokenSource.Token));
    }

    private static async Task WriteLoop(CancellationToken cancellationToken)
    {
        var buffer = new StringBuilder();
        var lastFlush = DateTime.Now;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_logQueue.TryTake(out var logLine, 100))
                {
                    buffer.AppendLine(logLine);
                }

                // Flush buffer if it's large enough or enough time has passed
                if (buffer.Length > 4096 || (DateTime.Now - lastFlush).TotalMilliseconds > 500)
                {
                    if (buffer.Length > 0)
                    {
                        await FlushBufferAsync(buffer.ToString());
                        buffer.Clear();
                        lastFlush = DateTime.Now;
                    }
                }
            }
            catch
            {
                // Continue on error
            }
        }

        // Final flush
        if (buffer.Length > 0)
        {
            await FlushBufferAsync(buffer.ToString());
        }
    }

    private static async Task FlushBufferAsync(string content)
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            RotateLogIfNeeded();

            await File.AppendAllTextAsync(LogFilePath, content);
        }
        catch
        {
            // Silently fail
        }
    }

    private static void RotateLogIfNeeded()
    {
        try
        {
            if (File.Exists(LogFilePath))
            {
                var fileInfo = new FileInfo(LogFilePath);
                if (fileInfo.Length > MaxLogSize)
                {
                    var archivePath = Path.Combine(LogDirectory, $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    File.Move(LogFilePath, archivePath);

                    // Keep only last 3 archived logs
                    var archives = Directory.GetFiles(LogDirectory, "debug_*.log")
                        .OrderByDescending(f => new FileInfo(f).CreationTime)
                        .Skip(3);

                    foreach (var oldArchive in archives)
                        File.Delete(oldArchive);
                }
            }
        }
        catch
        {
            // Ignore rotation errors
        }
    }

    public static void LogMemoryUsage()
    {
        if (!_isEnabled) return;

        Info("System", () =>
        {
            var workingSet = Environment.WorkingSet / (1024 * 1024);
            var gcMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            return $"Memory - Working Set: {workingSet}MB | GC Heap: {gcMemory}MB | Gen0: {GC.CollectionCount(0)} | Gen1: {GC.CollectionCount(1)} | Gen2: {GC.CollectionCount(2)}";
        });
    }

    public static void Shutdown()
    {
        _cancellationTokenSource?.Cancel();
        _writerTask?.Wait(1000);
        _logQueue?.Dispose();
    }
}