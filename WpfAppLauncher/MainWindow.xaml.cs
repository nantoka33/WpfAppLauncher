using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfAppLauncher.Configuration;
using WpfAppLauncher.Extensions;
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
        private readonly string themeStatePath;
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
            themeStatePath = Path.Combine(
                appDataDir,
                string.IsNullOrWhiteSpace(settings.Themes.StateFileName)
                    ? "theme.json"
                    : settings.Themes.StateFileName);
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

            ThemeSwitcher.AddThemeSwitcher(ThemePanel, settings.Themes.Options, ApplyTheme);

            var initialTheme = ThemePreferenceService.LoadPreferredTheme(themeStatePath);
            if (!TryApplyTheme(initialTheme, persist: false))
            {
                TryApplyTheme(settings.Themes.Default, persist: true);
            }

            ExtensionManagerButton.IsEnabled = ExtensionHost.IsInitialized;
            if (!ExtensionHost.IsInitialized)
            {
                ExtensionManagerButton.ToolTip = "拡張機能の初期化に失敗しました。ログを確認してください。";
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

        private void ExtensionManagerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ExtensionHost.IsInitialized)
            {
                MessageBox.Show(this, "拡張機能システムが初期化されていないため、管理画面を開けません。", "拡張機能", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new ExtensionManagerWindow
            {
                Owner = this,
            };

            window.ShowDialog();
        }

        private void ApplyTheme(string theme)
        {
            TryApplyTheme(theme, persist: true);
        }

        private bool TryApplyTheme(string? theme, bool persist)
        {
            if (string.IsNullOrWhiteSpace(theme))
            {
                return false;
            }

            if (!settings.Themes.Options.Any(option =>
                    string.Equals(option.Name, theme, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            ThemeSwitcher.SwitchTheme(theme);

            if (persist)
            {
                ThemePreferenceService.SavePreferredTheme(themeStatePath, theme);
            }

            return true;
        }
    }
}
