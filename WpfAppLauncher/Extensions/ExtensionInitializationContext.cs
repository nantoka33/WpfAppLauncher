using System;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace WpfAppLauncher.Extensions
{
    /// <summary>
    /// 拡張機能の初期化時にホストから渡される情報を表します。
    /// </summary>
    public sealed class ExtensionInitializationContext
    {
        public ExtensionInitializationContext(
            IServiceProvider? services,
            IConfiguration configuration,
            ILogger logger,
            string extensionsDirectory,
            string extensionDataDirectory)
        {
            Services = services;
            Configuration = configuration;
            Logger = logger;
            ExtensionsDirectory = extensionsDirectory;
            ExtensionDataDirectory = extensionDataDirectory;
        }

        /// <summary>
        /// ホストが公開しているサービスプロバイダー。利用できない場合は <c>null</c> です。
        /// </summary>
        public IServiceProvider? Services { get; }

        /// <summary>
        /// アプリケーションの構成情報。
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 拡張機能専用のロガー。
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// 拡張機能 DLL が配置されているディレクトリ。
        /// </summary>
        public string ExtensionsDirectory { get; }

        /// <summary>
        /// 拡張機能がデータファイルを保存するためのディレクトリ。
        /// </summary>
        public string ExtensionDataDirectory { get; }
    }
}
