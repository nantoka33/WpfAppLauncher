using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Serilog;
using WpfAppLauncher.Configuration;

namespace WpfAppLauncher.Extensions
{
    public sealed class ExtensionManager : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly ExtensionSettings _extensionSettings;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IServiceProvider? _serviceProvider;
        private readonly ExtensionStateStore _stateStore;
        private readonly List<ManagedExtension> _extensions = new();
        private readonly object _syncRoot = new();
        private FileSystemWatcher? _watcher;
        private Timer? _reloadTimer;
        private bool _disposed;

        private readonly string _extensionsDirectory;
        private readonly string _extensionDataRootDirectory;

        public ExtensionManager(AppSettings settings, IConfiguration configuration, ILogger logger, IServiceProvider? serviceProvider = null)
        {
            _settings = settings;
            _extensionSettings = settings.Extensions;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;

            _extensionsDirectory = Path.Combine(AppContext.BaseDirectory, _extensionSettings.DirectoryName);

            var appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                settings.AppData.ApplicationDirectoryName);

            _extensionDataRootDirectory = Path.Combine(appDataDirectory, _extensionSettings.DataDirectoryName);

            _stateStore = new ExtensionStateStore(settings.AppData, _extensionSettings, logger);
        }

        public event EventHandler<ExtensionsChangedEventArgs>? ExtensionsReloaded;

        public void Initialize()
        {
            EnsureDirectories();
            ReloadExtensions();
            InitializeWatcher();
        }

        public IReadOnlyList<ExtensionSnapshot> GetExtensionsSnapshot()
        {
            lock (_syncRoot)
            {
                return CreateSnapshots();
            }
        }

        public bool TryEnableExtension(string extensionId, out string? message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);

            lock (_syncRoot)
            {
                var extension = FindExtension(extensionId);
                if (extension is null)
                {
                    message = "指定された拡張機能が見つかりません。";
                    return false;
                }

                if (extension.DisabledByConfiguration)
                {
                    message = "この拡張機能は構成ファイルによって無効化されています。";
                    return false;
                }

                if (!extension.DisabledByUser)
                {
                    message = null;
                    return true;
                }
            }

            _stateStore.SetExtensionEnabled(extensionId, enabled: true);
            ReloadExtensions();
            message = null;
            return true;
        }

        public bool TryDisableExtension(string extensionId, out string? message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);

            lock (_syncRoot)
            {
                var extension = FindExtension(extensionId);
                if (extension is null)
                {
                    message = "指定された拡張機能が見つかりません。";
                    return false;
                }

                if (extension.DisabledByConfiguration)
                {
                    message = "構成で無効化されているため変更できません。";
                    return false;
                }
            }

            _stateStore.SetExtensionEnabled(extensionId, enabled: false);
            ReloadExtensions();
            message = null;
            return true;
        }

        public void ReloadExtensions()
        {
            if (_disposed)
            {
                return;
            }

            IReadOnlyList<ExtensionSnapshot> snapshots;

            lock (_syncRoot)
            {
                EnsureDirectories();
                DisposeExtensions();

                var disabledByConfiguration = new HashSet<string>(_extensionSettings.Disabled ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                var disabledByUser = new HashSet<string>(_stateStore.GetDisabledExtensionIds(), StringComparer.OrdinalIgnoreCase);

                foreach (var assemblyPath in DiscoverAssemblies())
                {
                    LoadExtensionFromAssembly(assemblyPath, disabledByConfiguration, disabledByUser);
                }

                snapshots = CreateSnapshots();
            }

            ExtensionsReloaded?.Invoke(this, new ExtensionsChangedEventArgs(snapshots));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            lock (_syncRoot)
            {
                DisposeWatcher();
                DisposeTimer();
                DisposeExtensions();
            }
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(_extensionsDirectory);
            Directory.CreateDirectory(_extensionDataRootDirectory);
        }

        private IEnumerable<string> DiscoverAssemblies()
        {
            try
            {
                return Directory.EnumerateFiles(_extensionsDirectory, _extensionSettings.SearchPattern, SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "拡張機能フォルダーの列挙に失敗しました: {Directory}", _extensionsDirectory);
                return Array.Empty<string>();
            }
        }

        private void LoadExtensionFromAssembly(string assemblyPath, HashSet<string> disabledByConfiguration, HashSet<string> disabledByUser)
        {
            ExtensionLoadContext? loadContext = null;

            try
            {
                loadContext = new ExtensionLoadContext(assemblyPath);

                using var assemblyStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                Stream? symbolStream = null;

                var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                if (!string.IsNullOrEmpty(pdbPath) && File.Exists(pdbPath))
                {
                    symbolStream = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }

                using (symbolStream)
                {
                    var assembly = symbolStream is null
                        ? loadContext.LoadFromStream(assemblyStream)
                        : loadContext.LoadFromStream(assemblyStream, symbolStream);

                    var extensionType = ResolveExtensionType(assembly);

                    if (extensionType is null)
                    {
                        _logger.Warning("拡張機能のエントリーポイントが見つかりませんでした: {Assembly}", assemblyPath);
                        loadContext.Unload();
                        return;
                    }

                    var metadata = extensionType.GetCustomAttribute<ExtensionMetadataAttribute>();
                    if (metadata is null)
                    {
                        _logger.Warning("ExtensionMetadataAttribute が見つかりません: {Type}", extensionType.FullName);
                        loadContext.Unload();
                        return;
                    }

                    var manifest = new ExtensionManifest(
                        metadata.Id,
                        string.IsNullOrWhiteSpace(metadata.DisplayName) ? extensionType.Name : metadata.DisplayName,
                        metadata.Version,
                        metadata.Description,
                        assemblyPath,
                        File.GetLastWriteTimeUtc(assemblyPath));

                    var disabledByConfig = disabledByConfiguration.Contains(manifest.Id);
                    var disabledByState = disabledByUser.Contains(manifest.Id);

                    var managed = new ManagedExtension(manifest, disabledByConfig, disabledByState);

                    if (_extensions.Any(existing => string.Equals(existing.Manifest.Id, manifest.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.Warning("拡張機能 ID {ExtensionId} は既に読み込まれているためスキップします。", manifest.Id);
                        loadContext.Unload();
                        return;
                    }

                    if (managed.CanActivate)
                    {
                        try
                        {
                            var logger = _logger
                                .ForContext("ExtensionId", manifest.Id)
                                .ForContext("ExtensionAssembly", Path.GetFileName(manifest.AssemblyPath));

                            var extensionDataDirectory = Path.Combine(_extensionDataRootDirectory, manifest.Id);
                            Directory.CreateDirectory(extensionDataDirectory);

                            var context = new ExtensionInitializationContext(
                                _serviceProvider,
                                _configuration,
                                logger,
                                _extensionsDirectory,
                                extensionDataDirectory);

                            managed.Activate(extensionType, loadContext, context, logger);
                            loadContext = null;
                            _logger.Information("拡張機能 {ExtensionId} ({DisplayName}) を読み込みました。", manifest.Id, manifest.DisplayName);
                        }
                        catch (Exception ex)
                        {
                            managed.MarkFailed(ex);
                            _logger.Error(ex, "拡張機能 {ExtensionId} の初期化に失敗しました。", manifest.Id);
                            loadContext.Unload();
                            loadContext = null;
                        }
                    }
                    else
                    {
                        loadContext.Unload();
                        loadContext = null;
                        if (disabledByConfig)
                        {
                            _logger.Information("拡張機能 {ExtensionId} は構成により無効化されています。", manifest.Id);
                        }
                        else if (disabledByState)
                        {
                            _logger.Information("拡張機能 {ExtensionId} はユーザー設定により無効化されています。", manifest.Id);
                        }
                    }

                    _extensions.Add(managed);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    _logger.Error(loaderException, "拡張機能の読み込み中に依存関係エラーが発生しました: {Assembly}", assemblyPath);
                }

                loadContext?.Unload();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "拡張機能アセンブリの読み込みに失敗しました: {Assembly}", assemblyPath);
                loadContext?.Unload();
            }
        }

        private static Type? ResolveExtensionType(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .FirstOrDefault(type =>
                        typeof(IAppExtension).IsAssignableFrom(type) &&
                        !type.IsAbstract &&
                        type.GetConstructor(Type.EmptyTypes) is not null);
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.FirstOrDefault(type =>
                    type is not null &&
                    typeof(IAppExtension).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) is not null);
            }
        }

        private ManagedExtension? FindExtension(string extensionId)
        {
            return _extensions.FirstOrDefault(extension =>
                string.Equals(extension.Manifest.Id, extensionId, StringComparison.OrdinalIgnoreCase));
        }

        private IReadOnlyList<ExtensionSnapshot> CreateSnapshots()
        {
            return _extensions
                .Select(extension => extension.ToSnapshot())
                .ToArray();
        }

        private void InitializeWatcher()
        {
            if (!_extensionSettings.WatchForChanges)
            {
                return;
            }

            try
            {
                _watcher = new FileSystemWatcher(_extensionsDirectory)
                {
                    Filter = _extensionSettings.SearchPattern,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                };

                _watcher.Created += OnExtensionsDirectoryChanged;
                _watcher.Changed += OnExtensionsDirectoryChanged;
                _watcher.Deleted += OnExtensionsDirectoryChanged;
                _watcher.Renamed += OnExtensionsDirectoryChanged;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "拡張機能フォルダーの監視を開始できませんでした: {Directory}", _extensionsDirectory);
                DisposeWatcher();
            }
        }

        private void OnExtensionsDirectoryChanged(object sender, FileSystemEventArgs e)
        {
            _logger.Information("拡張機能フォルダーで変更を検出しました: {ChangeType} {Path}", e.ChangeType, e.FullPath);
            ScheduleReload();
        }

        private void ScheduleReload()
        {
            lock (_syncRoot)
            {
                _reloadTimer ??= new Timer(_ => SafeReload(), null, Timeout.Infinite, Timeout.Infinite);
                _reloadTimer.Change(TimeSpan.FromMilliseconds(750), Timeout.InfiniteTimeSpan);
            }
        }

        private void SafeReload()
        {
            try
            {
                ReloadExtensions();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "拡張機能の自動再読み込みに失敗しました。");
            }
        }

        private void DisposeExtensions()
        {
            foreach (var extension in _extensions)
            {
                extension.Dispose(_logger);
            }

            _extensions.Clear();
        }

        private void DisposeWatcher()
        {
            if (_watcher is null)
            {
                return;
            }

            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnExtensionsDirectoryChanged;
            _watcher.Changed -= OnExtensionsDirectoryChanged;
            _watcher.Deleted -= OnExtensionsDirectoryChanged;
            _watcher.Renamed -= OnExtensionsDirectoryChanged;
            _watcher.Dispose();
            _watcher = null;
        }

        private void DisposeTimer()
        {
            _reloadTimer?.Dispose();
            _reloadTimer = null;
        }

        private sealed class ManagedExtension
        {
            public ManagedExtension(ExtensionManifest manifest, bool disabledByConfiguration, bool disabledByUser)
            {
                Manifest = manifest;
                DisabledByConfiguration = disabledByConfiguration;
                DisabledByUser = disabledByUser;
            }

            public ExtensionManifest Manifest { get; }

            public bool DisabledByConfiguration { get; }

            public bool DisabledByUser { get; }

            public bool CanActivate => !DisabledByConfiguration && !DisabledByUser;

            public IAppExtension? Instance { get; private set; }

            public ExtensionLoadContext? LoadContext { get; private set; }

            public Exception? LastError { get; private set; }

            public bool IsRuntimeLoaded => Instance is not null;

            public void Activate(Type implementationType, ExtensionLoadContext loadContext, ExtensionInitializationContext context, ILogger logger)
            {
                var instance = (IAppExtension?)Activator.CreateInstance(implementationType)
                    ?? throw new InvalidOperationException($"{implementationType.FullName} のインスタンスを作成できません。");

                Instance = instance;
                LoadContext = loadContext;

                try
                {
                    instance.Initialize(context);
                }
                catch
                {
                    Dispose(logger);
                    throw;
                }
            }

            public void MarkFailed(Exception exception)
            {
                LastError = exception;
            }

            public ExtensionSnapshot ToSnapshot()
            {
                var status = BuildStatusMessage();

                return new ExtensionSnapshot
                {
                    Id = Manifest.Id,
                    DisplayName = Manifest.DisplayName,
                    Version = Manifest.Version,
                    Description = Manifest.Description,
                    AssemblyPath = Manifest.AssemblyPath,
                    LastUpdatedUtc = Manifest.LastUpdatedUtc,
                    IsEnabled = !DisabledByUser && !DisabledByConfiguration,
                    IsLoaded = IsRuntimeLoaded,
                    DisabledByConfiguration = DisabledByConfiguration,
                    DisabledByUser = DisabledByUser,
                    InitializationFailed = LastError is not null,
                    StatusMessage = status,
                };
            }

            public void Dispose(ILogger logger)
            {
                if (Instance is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, "拡張機能 {ExtensionId} の破棄処理でエラーが発生しました。", Manifest.Id);
                    }
                }

                Instance = null;

                if (LoadContext is not null)
                {
                    LoadContext.Unload();
                    LoadContext = null;
                }
            }

            private string BuildStatusMessage()
            {
                if (DisabledByConfiguration)
                {
                    return "構成で無効化";
                }

                if (DisabledByUser)
                {
                    return "ユーザーにより無効化";
                }

                if (LastError is not null)
                {
                    return $"初期化失敗: {LastError.Message}";
                }

                return IsRuntimeLoaded ? "有効" : "未読み込み";
            }
        }

        private sealed record ExtensionManifest(
            string Id,
            string DisplayName,
            string Version,
            string? Description,
            string AssemblyPath,
            DateTime LastUpdatedUtc);

        private sealed class ExtensionLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;

            public ExtensionLoadContext(string assemblyPath)
                : base(isCollectible: true)
            {
                _resolver = new AssemblyDependencyResolver(assemblyPath);
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                return libraryPath is null
                    ? base.LoadUnmanagedDll(unmanagedDllName)
                    : LoadUnmanagedDllFromPath(libraryPath);
            }
        }
    }
}
