using System.Collections.Generic;

namespace WpfAppLauncher.Configuration
{
    public class AppSettings
    {
        public EnvironmentSettings Environment { get; set; } = new();
        public AppDataSettings AppData { get; set; } = new();
        public DragDropSettings DragDrop { get; set; } = new();
        public ThemeSettings Themes { get; set; } = new();
        public ExtensionSettings Extensions { get; set; } = new();
    }

    public class EnvironmentSettings
    {
        public string? Name { get; set; }
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
        public string StateFileName { get; set; } = "theme.json";
        public List<ThemeOption> Options { get; set; } = new();
    }

    public class ThemeOption
    {
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Tooltip { get; set; }
    }

    public class ExtensionSettings
    {
        public string DirectoryName { get; set; } = "Extensions";
        public string DataDirectoryName { get; set; } = "Extensions";
        public string SearchPattern { get; set; } = "*.Extension.dll";
        public string StateFileName { get; set; } = "extensions.json";
        public bool WatchForChanges { get; set; } = true;
        public List<string> Disabled { get; set; } = new();
    }
}
