using System;

namespace WpfAppLauncher.Extensions
{
    /// <summary>
    /// 基本的な拡張機能のインターフェースです。各プラグインはこのインターフェースを実装し、
    /// <see cref="ExtensionMetadataAttribute"/> を併せて指定する必要があります。
    /// </summary>
    public interface IAppExtension
    {
        /// <summary>
        /// 拡張機能の初期化を行います。
        /// </summary>
        /// <param name="context">ホストから提供される初期化コンテキスト。</param>
        void Initialize(ExtensionInitializationContext context);
    }
}
