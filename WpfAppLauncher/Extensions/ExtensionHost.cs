using System;

namespace WpfAppLauncher.Extensions
{
    public static class ExtensionHost
    {
        private static readonly object SyncRoot = new();
        private static ExtensionManager? _manager;

        public static bool IsInitialized => _manager is not null;

        public static ExtensionManager Manager => _manager ?? throw new InvalidOperationException("拡張機能マネージャーが初期化されていません。");

        internal static void Initialize(ExtensionManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            lock (SyncRoot)
            {
                _manager?.Dispose();
                _manager = manager;
            }
        }

        internal static void Shutdown()
        {
            lock (SyncRoot)
            {
                _manager?.Dispose();
                _manager = null;
            }
        }
    }
}
