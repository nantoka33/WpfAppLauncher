using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic;

namespace WpfAppLauncher.Services
{
    public static class DropHandler
    {
        public static void HandleDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        public static void HandleDrop(DragEventArgs e, ref List<AppEntry> apps, string[] allowedExtensions, string iconCacheDir, string savePath, string groupOrderPath, Action renderCallback)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var dropped = e.Data.GetData(DataFormats.FileDrop);
            if (dropped is not string[] files || files.Length == 0) return;

            foreach (var file in files)
            {
                if (File.Exists(file) && allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string group = PromptGroupInput(name);
                    var app = new AppEntry { Name = name, Path = file, Group = group };
                    apps.Add(app);
                    IconLoader.LoadIcon(app, iconCacheDir);
                }
            }

            AppDataService.SaveApps(apps, apps.Select(a => a.Group ?? "未分類").Distinct().ToList(), savePath, groupOrderPath);
            renderCallback();
        }

        public static string PromptGroupInput(string appName = "アプリ", string currentGroup = "未分類")
        {
            var input = Interaction.InputBox(
                $"「{appName}」のグループ名を入力してください：",
                "グループ名の入力",
                currentGroup);
            return string.IsNullOrWhiteSpace(input) ? "未分類" : input.Trim();
        }
    }
}
