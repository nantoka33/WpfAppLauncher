using System;

namespace WpfAppLauncher.Extensions
{
    /// <summary>
    /// 拡張機能クラスに付与するメタデータ属性です。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ExtensionMetadataAttribute : Attribute
    {
        public ExtensionMetadataAttribute(string id, string displayName, string version)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("拡張機能 ID を指定してください。", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("拡張機能名を指定してください。", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("バージョンを指定してください。", nameof(version));
            }

            Id = id;
            DisplayName = displayName;
            Version = version;
        }

        /// <summary>
        /// 拡張機能を一意に識別する ID。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// UI に表示される拡張機能名。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 拡張機能のバージョン文字列。
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// 拡張機能の簡単な説明。
        /// </summary>
        public string? Description { get; set; }
    }
}
