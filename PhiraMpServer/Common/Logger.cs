using System;

namespace PhiraMpServer.Common;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public static class Logger
{
    private static readonly object _lock = new();
    
    public static void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }
    
    public static void Info(string message)
    {
        Log(LogLevel.Info, message);
    }
    
    public static void Warning(string message)
    {
        Log(LogLevel.Warning, message);
    }
    
    public static void Error(string message)
    {
        Log(LogLevel.Error, message);
    }
    
    public static void Error(Exception ex, string message = "")
    {
        Log(LogLevel.Error, $"{message} {ex.Message}\n{ex.StackTrace}");
    }
    
    private static void Log(LogLevel level, string message)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var (color, prefix) = GetLevelDisplay(level);
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[{timestamp}] ");
            
            Console.ForegroundColor = color;
            Console.Write($"{prefix} ");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            
            Console.ResetColor();
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
}
