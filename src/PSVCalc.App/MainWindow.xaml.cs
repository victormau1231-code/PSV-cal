using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PSVCalc.App.ViewModels;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Services;

namespace PSVCalc.App;

public partial class MainWindow : Window
{
    private static readonly Dictionary<FrameworkElement, ToolTip> FocusHintToolTips = new();

    public MainWindow()
    {
        StartupLog.Write("MainWindow ctor begin.");
        InitializeComponent();
        StartupLog.Write("MainWindow InitializeComponent done.");
        Loaded += OnMainWindowLoaded;
    }

    private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnMainWindowLoaded;
        StartupLog.Write("MainWindow Loaded event.");
        Cursor = Cursors.Wait;
        try
        {
            MainViewModel vm = await Task.Run(BuildViewModel);
            DataContext = vm;
            StartupLog.Write("MainWindow DataContext assigned.");
        }
        catch (Exception ex)
        {
            StartupLog.Write($"MainWindow startup failed: {ex}");
            MessageBox.Show(
                $"Startup failed: {ex.Message}",
                "PSV Calculator Pro V 1.3.1 Startup",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
        finally
        {
            Cursor = null;
        }
    }

    private static MainViewModel BuildViewModel()
    {
        StartupLog.Write("BuildViewModel begin.");
        StoragePaths paths = ResolveStoragePathsWithFallback();
        StartupLog.Write($"BuildViewModel storage path: {paths.RootDirectory}");
        IOrificeSelector orificeSelector = new OrificeSelector();
        IStandardProfileProvider standardProfileProvider = new JsonStandardProfileProvider();
        ISafetyValveCalculator calculator = new SafetyValveCalculator(orificeSelector, standardProfileProvider);
        IProjectRepository repository = new JsonProjectRepository(paths);
        IExcelReportExporter exporter = new ExcelHtmlReportExporter(paths);
        IValidationCaseStore validationCaseStore = new JsonValidationCaseStore();
        IValidationCaseRunner validationCaseRunner = new ValidationCaseRunner(calculator);
        validationCaseStore.EnsureTemplate(paths.ValidationDirectory);

        MainViewModel vm = new MainViewModel(
            calculator,
            repository,
            exporter,
            validationCaseStore,
            validationCaseRunner,
            paths);
        StartupLog.Write("BuildViewModel end.");
        return vm;
    }

    private static StoragePaths ResolveStoragePathsWithFallback()
    {
        string? documentsPath = TryGetDocumentsPath(timeoutMs: 2000);
        if (!string.IsNullOrWhiteSpace(documentsPath))
        {
            StartupLog.Write($"ResolveStoragePaths use Documents: {documentsPath}");
            return new StoragePaths(Path.Combine(documentsPath, "PSVCalc"));
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        StartupLog.Write($"ResolveStoragePaths fallback LocalAppData: {localAppData}");
        return new StoragePaths(Path.Combine(localAppData, "PSVCalc"));
    }

    private static string? TryGetDocumentsPath(int timeoutMs)
    {
        try
        {
            Task<string> task = Task.Run(() => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            bool completed = task.Wait(timeoutMs);
            if (!completed)
            {
                StartupLog.Write("TryGetDocumentsPath timeout.");
                return null;
            }

            return string.IsNullOrWhiteSpace(task.Result) ? null : task.Result;
        }
        catch
        {
            StartupLog.Write("TryGetDocumentsPath failed.");
            return null;
        }
    }

    private void InputHint_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        object? content = ResolveToolTipContent(element.ToolTip);
        if (content is null)
        {
            return;
        }

        CloseFocusToolTip(element);

        var toolTip = new ToolTip
        {
            Content = content,
            PlacementTarget = element,
            Placement = PlacementMode.Right,
            StaysOpen = false,
            MaxWidth = 320,
            HasDropShadow = true
        };

        FocusHintToolTips[element] = toolTip;
        toolTip.IsOpen = true;
    }

    private void InputHint_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            CloseFocusToolTip(element);
        }
    }

    private static object? ResolveToolTipContent(object? source)
    {
        return source switch
        {
            null => null,
            ToolTip existingToolTip => existingToolTip.Content,
            _ => source
        };
    }

    private static void CloseFocusToolTip(FrameworkElement element)
    {
        if (!FocusHintToolTips.TryGetValue(element, out ToolTip? toolTip))
        {
            return;
        }

        toolTip.IsOpen = false;
        FocusHintToolTips.Remove(element);
    }
}
