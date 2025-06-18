using System.Collections.Generic;
using System.Diagnostics;
using DrawingIcon = System.Drawing.Icon;
using DrawingImage = System.Drawing.Image;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfAppLauncher
{
    public partial class MainWindow : Window
    {
        private List<AppEntry> apps = new();
        private readonly string savePath = "apps.json";
        private readonly string groupOrderPath = "group_order.json";
        private List<string> groupOrder = new();
        private readonly string iconCacheDir = "iconcache";

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(iconCacheDir);
            LoadApps();
            RenderGroups();
        }

        private void LoadApps()
        {
            if (File.Exists(savePath))
            {
                var json = File.ReadAllText(savePath);
                apps = JsonSerializer.Deserialize<List<AppEntry>>(json) ?? new List<AppEntry>();
            }
            if (File.Exists(groupOrderPath))
            {
                var json = File.ReadAllText(groupOrderPath);
                groupOrder = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
        }

        private void SaveApps()
        {
            File.WriteAllText(savePath, JsonSerializer.Serialize(apps));
            File.WriteAllText(groupOrderPath, JsonSerializer.Serialize(groupOrder));
        }

        private UIElement? draggedGroup = null!;

        private void RenderGroups()
        {
            GroupPanel.Children.Clear();
            if (groupOrder.Count == 0)
                groupOrder = apps.Select(a => a.Group ?? "未分類").Distinct().ToList();
            var groups = apps.GroupBy(a => a.Group ?? "未分類")
                .OrderBy(g => groupOrder.IndexOf(g.Key) >= 0 ? groupOrder.IndexOf(g.Key) : int.MaxValue)
                .ThenBy(g => g.Key);
            foreach (var group in groups)
            {
                var groupBox = new GroupBox
                {
                    Header = CreateGroupHeader(group.Key),
                    Margin = new Thickness(0, 0, 0, 10)
                };
                var wrap = new WrapPanel();

                foreach (var app in group)
                {
                    var stack = new StackPanel { Width = 100, Margin = new Thickness(5) };
                    var img = new Image
                    {
                        Width = 32,
                        Height = 32,
                        Margin = new Thickness(0, 0, 0, 5),
                        Source = LoadIcon(app)
                    };
                    var btn = new Button
                    {
                        Content = app.Name,
                        Width = 100,
                        Height = 40,
                        Tag = app
                    };
                    btn.Click += (s, e) => {
                        var path = ((AppEntry)((Button)s).Tag).Path;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            var psi = new ProcessStartInfo(path)
                            {
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                        }
                        else
                        {
                            MessageBox.Show($"パスが無効か存在しません：{path}", "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    var contextMenu = new ContextMenu();
                    var deleteItem = new MenuItem { Header = "削除" };
                    deleteItem.Click += (s, e) => {
                        apps.Remove((AppEntry)btn.Tag);
                        SaveApps();
                        RenderGroups();
                    };

                    var renameItem = new MenuItem { Header = "名前変更" };
                    renameItem.Click += (s, e) => {
                        var appEntry = (AppEntry)btn.Tag;
                        string newName = Microsoft.VisualBasic.Interaction.InputBox(
                        $"「{appEntry.Name ?? "アプリ"}」の新しい名前を入力してください：",
                        "名前の変更",
                        appEntry.Name ?? "アプリ");
                        if (!string.IsNullOrWhiteSpace(newName))
                        {
                            appEntry.Name = newName.Trim();
                            SaveApps();
                            RenderGroups();
                        }
                    };

                    var editGroupItem = new MenuItem { Header = "グループ変更" };
                    editGroupItem.Click += (s, e) => {
                        var appEntry = (AppEntry)btn.Tag;
                        string newGroup = PromptGroupInput(appEntry.Name ?? "アプリ", appEntry.Group ?? "未分類");
                        appEntry.Group = newGroup;
                        SaveApps();
                        RenderGroups();
                    };

                    var runAsAdminItem = new MenuItem { Header = "管理者として実行" };
                    runAsAdminItem.Click += (s, e) => {
                        var appEntry = (AppEntry)btn.Tag;
                        var path = appEntry.Path;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            var psi = new ProcessStartInfo(path)
                            {
                                UseShellExecute = true,
                                Verb = "runas"
                            };
                            try { Process.Start(psi); }
                            catch { MessageBox.Show("管理者としての実行が拒否されました。", "実行キャンセル", MessageBoxButton.OK, MessageBoxImage.Warning); }
                        }
                        else
                        {
                            MessageBox.Show($"パスが無効か存在しません：{path}", "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    contextMenu.Items.Add(runAsAdminItem);
                    contextMenu.Items.Add(renameItem);

                    contextMenu.Items.Add(editGroupItem);
                    contextMenu.Items.Add(deleteItem);
                    btn.ContextMenu = contextMenu;

                    stack.Children.Add(img);
                    stack.Children.Add(btn);
                    wrap.Children.Add(stack);
                }

                groupBox.Content = wrap;
                groupBox.AllowDrop = true;
                groupBox.PreviewMouseDown += (s, e) => draggedGroup = groupBox;
                groupBox.Drop += (s, e) => {
                    if (draggedGroup == null || draggedGroup == groupBox) return;
                    int oldIndex = GroupPanel.Children.IndexOf(draggedGroup);
                    int newIndex = GroupPanel.Children.IndexOf(groupBox);
                    if (oldIndex >= 0 && newIndex >= 0)
                    {
                        GroupPanel.Children.Remove(draggedGroup);
                        GroupPanel.Children.Insert(newIndex, draggedGroup);
                        groupOrder = GroupPanel.Children.OfType<GroupBox>()
                    .Select(g => ((Label)((StackPanel)g.Header).Children[0]).Content?.ToString() ?? "未分類")
                    .ToList();
                        SaveApps();
                    }
                    draggedGroup = null;
                };
                GroupPanel.Children.Add(groupBox);
            }
        }

        private object CreateGroupHeader(string groupName)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var label = new Label { Content = groupName, FontWeight = FontWeights.Bold };
            panel.Children.Add(label);
            return panel;
        }

        private BitmapImage LoadIcon(AppEntry app)
        {
            if (!string.IsNullOrEmpty(app.IconPath) && File.Exists(app.IconPath))
            {
                return new BitmapImage(new System.Uri(Path.GetFullPath(app.IconPath)));
            }
            else
            {
                try
                {
                    if (string.IsNullOrEmpty(app.Path)) throw new ArgumentNullException(nameof(app.Path));
                    if (string.IsNullOrEmpty(app.Path)) throw new ArgumentNullException(nameof(app.Path));
                    DrawingIcon? icon = DrawingIcon.ExtractAssociatedIcon(app.Path);
                    if (icon == null) throw new InvalidOperationException($"アイコンを取得できません: {app.Path}");
                    string iconFile = Path.Combine(iconCacheDir, Path.GetFileNameWithoutExtension(app.Path) + ".png");
                    using (var bmp = icon.ToBitmap())
                    {
                        bmp.Save(iconFile);
                    }
                    app.IconPath = iconFile;
                    return new BitmapImage(new System.Uri(Path.GetFullPath(iconFile)));
                }
                catch
                {
                    return new BitmapImage();
                }
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var allowedExtensions = new[] { ".exe", ".bat", ".lnk" };
                if (File.Exists(file) && allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string group = PromptGroupInput(name);
                    var app = new AppEntry { Name = name, Path = file, Group = group };
                    apps.Add(app);
                    LoadIcon(app); // アイコンキャッシュ生成
                }
            }
            SaveApps();
            RenderGroups();
        }

        private string PromptGroupInput(string appName = "アプリ", string currentGroup = "未分類")
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                $"「{appName}」のグループ名を入力してください：",
                "グループ名の入力",
                currentGroup);
            return string.IsNullOrWhiteSpace(input) ? "未分類" : input.Trim();
        }
    }
}
