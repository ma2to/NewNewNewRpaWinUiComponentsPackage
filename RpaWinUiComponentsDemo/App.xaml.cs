using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace RpaWinUiComponents.Demo;

/// <summary>
/// Demo aplikácia pre testovanie RpaWinUiComponentsPackage s package reference
/// Implementuje UI Update Strategy z newProject.md
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Logger factory pre demo aplikáciu
    /// </summary>
    public static ILoggerFactory? LoggerFactory { get; private set; }

    /// <summary>
    /// Main window
    /// </summary>
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();

        // Setup logging pre demo aplikáciu
        SetupLogging();

        // SENIOR DEV: Global exception handler for comprehensive logging
        this.UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log unhandled exceptions
        var logger = LoggerFactory?.CreateLogger<App>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<App>.Instance;
        logger.LogCritical(e.Exception, "UNHANDLED EXCEPTION in application - Message: {ErrorMessage}", e.Exception.Message);

        // Mark as handled to prevent crash
        e.Handled = true;
    }

    /// <summary>
    /// Invoked when the application is launched
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    /// <summary>
    /// Setup Microsoft.Extensions.Logging pre testing balíka - SENIOR DEVELOPER COMPREHENSIVE LOGGING
    /// </summary>
    private void SetupLogging()
    {
        // Create logs directory if it doesn't exist
        var logDirectory = Path.Combine(Path.GetTempPath(), "RpaWinUiDemo");
        Directory.CreateDirectory(logDirectory);
        
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                })
                .AddDebug()             // Debug output
                .SetMinimumLevel(LogLevel.Debug);  // CRITICAL: Ensure Debug level logs are processed
        });
        
        // Test the logger immediately
        var testLogger = LoggerFactory.CreateLogger<App>();
        testLogger.LogInformation("[APP-SETUP] SENIOR DEVELOPER LOGGING - Comprehensive logging initialized");
        testLogger.LogDebug("[APP-SETUP] Log directory: {LogDirectory}", logDirectory);
        testLogger.LogDebug("[APP-SETUP] Logger factory type: {LoggerFactoryType}", LoggerFactory.GetType().Name);
    }
}