using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

namespace PSVCalc.App;

public partial class App : Application
{
    private static int StartupErrorDialogShown;

    protected override void OnStartup(StartupEventArgs e)
    {
        StartupLog.Write("App OnStartup begin.");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);
        StartupLog.Write("App OnStartup end.");
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        StartupLog.Write($"DispatcherUnhandledException: {e.Exception}");

        if (Interlocked.Exchange(ref StartupErrorDialogShown, 1) == 0)
        {
            MessageBox.Show(
                e.Exception.Message,
                "PSV Calculator Pro V 1.3.2 Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        e.Handled = true;
        Current?.Shutdown(-1);
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        StartupLog.Write($"UnhandledException: {e.ExceptionObject}");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        StartupLog.Write($"UnobservedTaskException: {e.Exception}");
        e.SetObserved();
    }
}

internal static class StartupLog
{
    private static readonly object Sync = new();

    public static void Write(string message)
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(localAppData, "PSVCalc");
            Directory.CreateDirectory(dir);
            string logPath = Path.Combine(dir, "startup.log");
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}";
            lock (Sync)
            {
                File.AppendAllText(logPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Ignore logging failures.
        }
    }
}
