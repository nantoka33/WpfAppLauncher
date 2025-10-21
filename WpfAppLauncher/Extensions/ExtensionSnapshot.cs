using System;

namespace WpfAppLauncher.Extensions
{
    /// <summary>
    /// 拡張機能の状態スナップショットを表します。
    /// </summary>
    public sealed class ExtensionSnapshot
    {
        public string Id { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string Version { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string AssemblyPath { get; init; } = string.Empty;

        public DateTime LastUpdatedUtc { get; init; }

        public bool IsEnabled { get; init; }

        public bool IsLoaded { get; init; }

        public bool DisabledByConfiguration { get; init; }

        public bool DisabledByUser { get; init; }

        public bool InitializationFailed { get; init; }

        public string StatusMessage { get; init; } = string.Empty;
    }
}
