using System.Buffers;
using System.Text;
using System.Threading.Channels;

namespace PhiraMp.Server;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// High-performance logging system with batching and minimal allocations
/// </summary>
public static class Logger
{
    // Use Channel instead of BlockingCollection for better async performance
    private static readonly Channel<LogEntry> _logChannel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(5000)
    {
        FullMode = BoundedChannelFullMode.DropOldest // Drop oldest messages if buffer full
    });
    
    private static readonly CancellationTokenSource _cts = new();
    private static readonly Task _logTask;
    
    // Object pool for log entries to reduce allocations
    private static readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    private static readonly StringBuilder _stringBuilder = new(512);
    
    private struct LogEntry
    {
        public LogLevel Level;
        public string Message;
        public long Timestamp; // Ticks for faster formatting
    }

    static Logger()
    {
        // Start background logging thread with higher priority to avoid log delays
        _logTask = Task.Run(ProcessLogQueue);
        
        // Register application exit event
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
    }
    
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Debug(string message)
    {
        EnqueueLog(LogLevel.Debug, message);
    }
    
    public static void Info(string message)
    {
        EnqueueLog(LogLevel.Info, message);
    }
    
    public static void Warning(string message)
    {
        EnqueueLog(LogLevel.Warning, message);
    }
    
    public static void Error(string message)
    {
        EnqueueLog(LogLevel.Error, message);
    }
    
    public static void Error(Exception ex, string message = "")
    {
        var fullMessage = string.IsNullOrEmpty(message) 
            ? $"{ex.Message}\n{ex.StackTrace}" 
            : $"{message} {ex.Message}\n{ex.StackTrace}";
        EnqueueLog(LogLevel.Error, fullMessage);
    }
    
    private static void EnqueueLog(LogLevel level, string message)
    {
        if (_cts.Token.IsCancellationRequested)
            return;

        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            Timestamp = DateTime.Now.Ticks
        };

        // Use TryWrite to avoid blocking
        _logChannel.Writer.TryWrite(entry);
    }
    
    private static void ProcessLogQueue()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_logChannel.Reader.TryRead(out var entry))
                {
                    WriteLogDirect(entry);
                }
                else if (_logChannel.Reader.TryPeek(out _))
                {
                    // More messages available
                    continue;
                }
                else
                {
                    // No messages, wait a bit before checking again
                    Thread.Sleep(1);
                }
            }
            
            // Flush remaining messages on shutdown
            while (_logChannel.Reader.TryRead(out var entry))
            {
                WriteLogDirect(entry);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Logger thread error: {ex.Message}");
        }
    }
    
    private static void WriteLogDirect(LogEntry entry)
    {
        try
        {
            // Use cached StringBuilder to reduce allocations
            lock (_stringBuilder)
            {
                _stringBuilder.Clear();
                
                // Format timestamp more efficiently
                var dt = new DateTime(entry.Timestamp);
                _stringBuilder.Append('[');
                _stringBuilder.Append(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                _stringBuilder.Append("] ");
                
                var (color, prefix) = GetLevelDisplay(entry.Level);
                
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(_stringBuilder.ToString());
                
                Console.ForegroundColor = color;
                Console.Write(prefix);
                Console.Write(" ");
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(entry.Message);
                
                Console.ResetColor();
            }
        }
        catch
        {
            // Ignore output errors to prevent logger crashes
        }
    }
    
    private static (ConsoleColor color, string prefix) GetLevelDisplay(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => (ConsoleColor.Cyan, "DEBUG"),
            LogLevel.Info => (ConsoleColor.Green, "INFO "),
            LogLevel.Warning => (ConsoleColor.Yellow, "WARN "),
            LogLevel.Error => (ConsoleColor.Red, "ERROR"),
            _ => (ConsoleColor.White, "     ")
        };
    }
    
    public static void Shutdown()
    {
        if (_cts.IsCancellationRequested)
            return;

        _cts.Cancel();
        _logChannel.Writer.Complete();
        
        // Wait for all logs to be flushed, with 5 second timeout
        try
        {
            _logTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore timeouts
        }
        
        _cts.Dispose();
    }
}
