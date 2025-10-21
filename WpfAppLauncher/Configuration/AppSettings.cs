using System.Collections.Generic;

namespace WpfAppLauncher.Configuration
{
    public class AppSettings
    {
        public AppDataSettings AppData { get; set; } = new();
        public DragDropSettings DragDrop { get; set; } = new();
        public ThemeSettings Themes { get; set; } = new();
    }

    public class AppDataSettings
    {
        public string ApplicationDirectoryName { get; set; } = "WpfAppLauncher";
        public string AppsFileName { get; set; } = "apps.json";
        public string GroupOrderFileName { get; set; } = "group_order.json";
        public string IconCacheDirectoryName { get; set; } = "iconcache";
    }

    public class DragDropSettings
    {
        public string[] AllowedExtensions { get; set; } = new[] { ".exe", ".bat", ".lnk" };
    }

    public class ThemeSettings
    {
        public string Default { get; set; } = "LightTheme";
        public List<ThemeOption> Options { get; set; } = new()
        {
            new ThemeOption { Name = "LightTheme", Icon = "☀", Tooltip = "ライトテーマ" },
            new ThemeOption { Name = "DarkTheme", Icon = "🌙", Tooltip = "ダークテーマ" },
            new ThemeOption { Name = "BlueTheme", Icon = "🔵", Tooltip = "ブルーテーマ" },
        };
    }

    public class ThemeOption
    {
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Tooltip { get; set; }
    }
}
