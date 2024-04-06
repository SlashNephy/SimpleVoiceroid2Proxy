using System;

namespace SimpleVoiceroid2Proxy;

public sealed class ConsoleLogger : ILogger
{
    public static readonly ILogger Instance = new ConsoleLogger();

    public void Debug(string message)
    {
#if DEBUG
        Log(LogLevel.DEBUG, message);
#endif
    }

    public void Info(string message)
    {
        Log(LogLevel.INFO, message);
    }

    public void Warn(string message)
    {
        Log(LogLevel.WARN, message);
    }

    public void Error(Exception exception, string message)
    {
        Log(LogLevel.ERROR, $"{message}\n{exception}");
    }

    private void Log(LogLevel level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");
    }
}

public interface ILogger
{
    public void Debug(string message);
    public void Info(string message);
    public void Warn(string message);
    public void Error(Exception exception, string message);
}

public enum LogLevel
{
    DEBUG,
    INFO,
    WARN,
    ERROR,
}
