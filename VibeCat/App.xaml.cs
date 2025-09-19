using System;
using System.Linq;
using System.Windows;
using VibeCat.Services;

namespace VibeCat;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Check command line arguments for debug mode
        if (e.Args.Contains("--debug") || e.Args.Contains("-d"))
        {
            DebugLogger.IsEnabled = true;
            DebugLogger.Info("App", () => $"VibeCat started with args: {string.Join(" ", e.Args)}");
        }

        // Create and show the main window
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DebugLogger.Info("App", "VibeCat shutting down");
        DebugLogger.Shutdown();
        base.OnExit(e);
    }
}