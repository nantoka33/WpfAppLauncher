using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using WpfAppLauncher.Configuration;
using WpfAppLauncher.Extensions;

namespace WpfAppLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Serilog.ILogger? _logger;
        private ExtensionManager? _extensionManager;
        private string? _logDirectory;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                ConfigureLogging();
                RegisterGlobalExceptionHandlers();
                InitializeExtensions();

                _logger?.Information("Application starting. Arguments: {Arguments}", e.Args);

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                _logger?.Fatal(ex, "Fatal error during application startup.");
                ShowCriticalError(ex);
                Shutdown(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.Information("Application exiting with code {ExitCode}.", e.ApplicationExitCode);
            ExtensionHost.Shutdown();
            base.OnExit(e);
            Log.CloseAndFlush();
        }

        private void InitializeExtensions()
        {
            try
            {
                var settings = AppConfiguration.Current;
                var logger = Log.ForContext<ExtensionManager>();
                var manager = new ExtensionManager(settings, AppConfiguration.Configuration, logger);
                manager.Initialize();
                ExtensionHost.Initialize(manager);
                _extensionManager = manager;

                var count = manager.GetExtensionsSnapshot().Count;
                _logger?.Information("Extension manager initialized. {ExtensionCount} extension(s) discovered.", count);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "拡張機能の初期化に失敗しました。拡張機能は無効化されます。");
            }
        }

        private void ConfigureLogging()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var appDataSection = configuration.GetSection("AppData");
            var appDirectoryName = appDataSection.GetValue<string>("ApplicationDirectoryName") ?? "WpfAppLauncher";

            var loggingSection = configuration.GetSection("Logging");
            var logDirectoryName = loggingSection.GetValue<string>("DirectoryName") ?? "Logs";
            var logFileName = loggingSection.GetValue<string>("FileName") ?? "launcher-.log";
            var retainedFileCountLimit = loggingSection.GetValue<int?>("RetainedFileCountLimit") ?? 14;
            var minimumLevelString = loggingSection.GetValue<string>("MinimumLevel") ?? "Information";

            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appDirectoryName,
                logDirectoryName);

            Directory.CreateDirectory(_logDirectory);

            var logFilePath = Path.Combine(_logDirectory, logFileName);

            if (!Enum.TryParse(minimumLevelString, ignoreCase: true, out LogEventLevel minimumLevel))
            {
                minimumLevel = LogEventLevel.Information;
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", environmentName)
                .Enrich.WithProperty("Application", appDirectoryName)
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: retainedFileCountLimit,
                    shared: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _logger = Log.ForContext<App>();
            _logger.Information("Logger initialized. Logs directory: {LogDirectory}", _logDirectory);
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.Error(e.Exception, "Unhandled exception on dispatcher thread.");
            ShowRecoverableError(e.Exception);
            e.Handled = true;
        }

        private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception.");
            _logger?.Fatal(exception, "Unhandled exception on application domain. IsTerminating: {IsTerminating}", e.IsTerminating);
            ShowCriticalError(exception);

            Dispatcher?.BeginInvoke(new Action(() => Shutdown(-1)));
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger?.Error(e.Exception, "Unobserved task exception.");
            ShowRecoverableError(e.Exception);
            e.SetObserved();
        }

        private void ShowRecoverableError(Exception exception)
        {
            var message = "アプリケーションで予期しないエラーが発生しました。必要に応じてサポートチームにログファイルを共有してください。"
                          + Environment.NewLine + Environment.NewLine
                          + exception.Message;

            if (!string.IsNullOrEmpty(_logDirectory))
            {
                message += Environment.NewLine + Environment.NewLine
                    + $"ログファイルの場所: {_logDirectory}";
            }

            ShowMessage(message, "エラー", MessageBoxImage.Error);
        }

        private void ShowCriticalError(Exception? exception)
        {
            var message = "アプリケーションで致命的なエラーが発生しました。終了します。"
                          + (exception is not null
                              ? Environment.NewLine + Environment.NewLine + exception.Message
                              : string.Empty);

            if (!string.IsNullOrEmpty(_logDirectory))
            {
                message += Environment.NewLine + Environment.NewLine
                    + $"ログファイルの場所: {_logDirectory}";
            }

            ShowMessage(message, "致命的なエラー", MessageBoxImage.Stop);
        }

        private void ShowMessage(string message, string caption, MessageBoxImage image)
        {
            if (Dispatcher?.CheckAccess() == true)
            {
                MessageBox.Show(message, caption, MessageBoxButton.OK, image);
            }
            else
            {
                Dispatcher?.Invoke(() => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
            }
        }
    }
}
