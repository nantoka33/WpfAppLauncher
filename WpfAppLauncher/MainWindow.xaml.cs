using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfAppLauncher.Services;

namespace WpfAppLauncher
{
    public partial class MainWindow : Window
    {
        private List<AppEntry> apps = new();
        private List<string> groupOrder = new();
        private readonly string appDataDir = Path.Combine(
                                             Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WpfAppLauncher");
        private readonly string savePath;
        private readonly string groupOrderPath;
        private readonly string iconCacheDir;


        private GroupRenderer? groupRenderer;

        public MainWindow()
        {
            InitializeComponent();

            Directory.CreateDirectory(appDataDir); // AppData/WpfAppLauncher を作成
            savePath = Path.Combine(appDataDir, "apps.json");
            groupOrderPath = Path.Combine(appDataDir, "group_order.json");
            iconCacheDir = Path.Combine(appDataDir, "iconcache");

            Directory.CreateDirectory(iconCacheDir);

            apps = AppDataService.LoadApps(savePath, groupOrderPath, out groupOrder);

            groupRenderer = new GroupRenderer(apps, groupOrder, savePath, groupOrderPath, iconCacheDir, GroupPanel);
            groupRenderer.RenderGroups();

            ThemeSwitcher.AddThemeSwitcher(ThemePanel, ThemeSwitcher.SwitchTheme);
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
                new[] { ".exe", ".bat", ".lnk" },
                iconCacheDir,
                savePath,
                groupOrderPath,
                () => groupRenderer?.RenderGroups()
            );
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e) => ThemeSwitcher.SwitchTheme("LightTheme");
        private void DarkTheme_Click(object sender, RoutedEventArgs e) => ThemeSwitcher.SwitchTheme("DarkTheme");
        private void BlueTheme_Click(object sender, RoutedEventArgs e) => ThemeSwitcher.SwitchTheme("BlueTheme");

        public List<string> GetGroupList()
        {
            return apps.Select(a => a.Group ?? "未分類").Distinct().ToList();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (groupRenderer != null)
            {
                // グループの順番だけ保存（apps の保存はここでは不要）
                File.WriteAllText(groupOrderPath, JsonSerializer.Serialize(groupOrder));
            }
        }
    }
}
