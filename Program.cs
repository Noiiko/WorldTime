﻿namespace WorldTime;

class Program {
    private static WorldTime _bot = null!;
    private static readonly DateTimeOffset _botStartTime = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns the amount of time the program has been running in a human-readable format.
    /// </summary>
    public static string BotUptime => (DateTimeOffset.UtcNow - _botStartTime).ToString("d' days, 'hh':'mm':'ss");

    static async Task Main() {
        Configuration? cfg = null;
        try {
            cfg = new Configuration();
        } catch (Exception ex) {
            Console.WriteLine(ex);
            Environment.Exit(2);
        }

        _bot = new WorldTime(cfg);
        AppDomain.CurrentDomain.ProcessExit += OnCancelEvent;
        Console.CancelKeyPress += OnCancelEvent;

        await _bot.StartAsync();
        await Task.Delay(-1);
    }

    /// <summary>
    /// Sends a formatted message to console.
    /// </summary>
    public static void Log(string source, string message) {
        var ts = DateTime.Now;
        var ls = new string[] { "\r\n", "\n" };
        foreach (var item in message.Split(ls, StringSplitOptions.None))
            Console.WriteLine($"{ts:s} [{source}] {item}");
    }

    private static bool _shutdownRequested = false;
    private static void OnCancelEvent(object? sender, EventArgs e) {
        if (e is ConsoleCancelEventArgs ce) ce.Cancel = true;
        
        if (_shutdownRequested) return;
        _shutdownRequested = true;
        Log("Shutdown", "Shutting down...");

        var dispose = Task.Run(_bot.Dispose);
        if (!dispose.Wait(15000)) {
            Log("Shutdown", "Normal shutdown is taking too long. Will force quit.");
            Environment.ExitCode = 1;
        }
        Environment.Exit(Environment.ExitCode);
    }
}
