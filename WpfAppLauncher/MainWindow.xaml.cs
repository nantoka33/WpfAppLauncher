using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfAppLauncher.Configuration;
using WpfAppLauncher.Services;

namespace WpfAppLauncher
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings settings;
        private readonly string appDataDir;
        private readonly string savePath;
        private readonly string groupOrderPath;
        private readonly string iconCacheDir;
        private readonly string[] allowedExtensions;

        private List<AppEntry> apps = new();
        private List<string> groupOrder = new();
        private GroupRenderer? groupRenderer;

        public MainWindow()
        {
            InitializeComponent();

            settings = AppConfiguration.Current;

            appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                settings.AppData.ApplicationDirectoryName);
            savePath = Path.Combine(appDataDir, settings.AppData.AppsFileName);
            groupOrderPath = Path.Combine(appDataDir, settings.AppData.GroupOrderFileName);
            iconCacheDir = Path.Combine(appDataDir, settings.AppData.IconCacheDirectoryName);
            allowedExtensions = settings.DragDrop.AllowedExtensions
                .Select(extension =>
                    string.IsNullOrWhiteSpace(extension)
                        ? string.Empty
                        : (extension.StartsWith(".") ? extension : $".{extension}").ToLowerInvariant())
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Distinct()
                .ToArray();

            Directory.CreateDirectory(appDataDir);
            Directory.CreateDirectory(iconCacheDir);

            apps = AppDataService.LoadApps(savePath, groupOrderPath, out groupOrder);

            groupRenderer = new GroupRenderer(apps, groupOrder, savePath, groupOrderPath, iconCacheDir, GroupPanel);
            groupRenderer.RenderGroups();

            ThemeSwitcher.AddThemeSwitcher(ThemePanel, settings.Themes.Options, ThemeSwitcher.SwitchTheme);

            if (!string.IsNullOrWhiteSpace(settings.Themes.Default))
            {
                ThemeSwitcher.SwitchTheme(settings.Themes.Default);
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            DropHandler.HandleDragEnter(e);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            DropHandler.HandleDrop(
                e,
                ref apps,
                allowedExtensions,
                iconCacheDir,
                savePath,
                groupOrderPath,
                () => groupRenderer?.RenderGroups()
            );
        }

        public List<string> GetGroupList()
        {
            return apps.Select(a => a.Group ?? "未分類").Distinct().ToList();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (groupRenderer != null)
            {
                File.WriteAllText(groupOrderPath, JsonSerializer.Serialize(groupOrder));
            }
        }
    }
}
