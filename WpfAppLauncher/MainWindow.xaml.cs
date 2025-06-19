using System.Collections.Generic;
using System.IO;
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
        private readonly string savePath = "apps.json";
        private readonly string groupOrderPath = "group_order.json";
        private readonly string iconCacheDir = "iconcache";

        private GroupRenderer? groupRenderer;

        public MainWindow()
        {
            InitializeComponent();

            Directory.CreateDirectory(iconCacheDir);

            apps = AppDataService.LoadApps(savePath, groupOrderPath, out groupOrder);

            groupRenderer = new GroupRenderer(apps, groupOrder, savePath, groupOrderPath, iconCacheDir, GroupPanel);
            groupRenderer.RenderGroups();

            ThemeSwitcher.AddThemeSwitcher(GroupPanel, ThemeSwitcher.SwitchTheme);
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
    }
}
