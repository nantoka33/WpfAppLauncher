using System.Collections.Generic;
using System.Diagnostics;
using DrawingIcon = System.Drawing.Icon;
using DrawingImage = System.Drawing.Image;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace WpfAppLauncher
{
    public partial class MainWindow : Window
    {
        private List<AppEntry> apps = new();
        private readonly string savePath = "apps.json";
        private readonly string groupOrderPath = "group_order.json";
        private List<string> groupOrder = new();
        private readonly string iconCacheDir = "iconcache";
        private UIElement? draggedGroup;
        private StackPanel? draggedIcon;
        private Point groupDragStartPoint;
        private Point iconDragStartPoint;

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(iconCacheDir);
            LoadApps();
            RenderGroups();
            AddThemeSwitcher();
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
                    Margin = new Thickness(0, 0, 0, 10),
                    AllowDrop = true
                };

                groupBox.PreviewMouseLeftButtonDown += (s, e) => {
                    groupDragStartPoint = e.GetPosition(null);
                    draggedGroup = groupBox;
                };
                groupBox.PreviewMouseMove += (s, e) => {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        Point currentPos = e.GetPosition(null);
                        if ((Math.Abs(currentPos.X - groupDragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                            (Math.Abs(currentPos.Y - groupDragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                        {
                            DragDrop.DoDragDrop(groupBox, groupBox, DragDropEffects.Move);
                        }
                    }
                };
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

                var wrap = new WrapPanel { AllowDrop = true };

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
                            var psi = new ProcessStartInfo(path) { UseShellExecute = true };
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
                            var psi = new ProcessStartInfo(path) { UseShellExecute = true, Verb = "runas" };
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

                    stack.PreviewMouseLeftButtonDown += (s, e) => {
                        iconDragStartPoint = e.GetPosition(null);
                        draggedIcon = stack;
                    };
                    stack.PreviewMouseMove += (s, e) => {
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            Point currentPos = e.GetPosition(null);
                            if ((Math.Abs(currentPos.X - iconDragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                                (Math.Abs(currentPos.Y - iconDragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                            {
                                DragDrop.DoDragDrop(stack, stack, DragDropEffects.Move);
                            }
                        }
                    };
                }

                wrap.Drop += (s, e) => {
                    if (draggedIcon == null) return;
                    var target = e.OriginalSource as DependencyObject;
                    while (target != null && !(target is StackPanel panel && wrap.Children.Contains(panel)))
                        target = VisualTreeHelper.GetParent(target);
                    if (target is StackPanel targetPanel)
                    {
                        int oldIndex = wrap.Children.IndexOf(draggedIcon);
                        int newIndex = wrap.Children.IndexOf(targetPanel);
                        if (oldIndex >= 0 && newIndex >= 0)
                        {
                            wrap.Children.Remove(draggedIcon);
                            wrap.Children.Insert(newIndex, draggedIcon);

                            var reordered = wrap.Children.OfType<StackPanel>()
                                .Select(sp => (AppEntry)((Button)sp.Children[1]).Tag).ToList();

                            var groupName = group.Key;
                            apps.RemoveAll(a => a.Group == groupName);
                            apps.AddRange(reordered);
                            SaveApps();
                        }
                    }
                    draggedIcon = null;
                };

                groupBox.Content = wrap;
                GroupPanel.Children.Add(groupBox);
            }
        }

        private object CreateGroupHeader(string groupName)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var label = new Label { Content = groupName, FontWeight = FontWeights.Bold };
            label.SetResourceReference(Label.ForegroundProperty, "ForegroundBrush");

            var contextMenu = new ContextMenu();
            var renameMenuItem = new MenuItem { Header = "グループ名を変更" };
            renameMenuItem.Click += (s, e) =>
            {
                string newName = Microsoft.VisualBasic.Interaction.InputBox(
                    $"「{groupName}」の新しいグループ名を入力してください：",
                    "グループ名の変更",
                    groupName);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    foreach (var app in apps.Where(a => (a.Group ?? "未分類") == groupName))
                    {
                        app.Group = newName.Trim();
                    }

                    for (int i = 0; i < groupOrder.Count; i++)
                    {
                        if (groupOrder[i] == groupName)
                        {
                            groupOrder[i] = newName.Trim();
                            break;
                        }
                    }

                    SaveApps();
                    RenderGroups();
                }
            };
            contextMenu.Items.Add(renameMenuItem);

            label.ContextMenu = contextMenu;
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
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var dropped = e.Data.GetData(DataFormats.FileDrop);
            if (dropped is not string[] files || files.Length == 0) return;

            var allowedExtensions = new[] { ".exe", ".bat", ".lnk" };

            foreach (var file in files)
            {
                if (File.Exists(file) && allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string group = PromptGroupInput(name);
                    var app = new AppEntry { Name = name, Path = file, Group = group };
                    apps.Add(app);
                    LoadIcon(app);
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
        private void AddThemeSwitcher()
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5)
            };

            var lightButton = new Button { Content = "☀", Width = 30, Margin = new Thickness(2) };
            lightButton.Click += (s, e) => SwitchTheme("LightTheme");
            var darkButton = new Button { Content = "🌙", Width = 30, Margin = new Thickness(2) };
            darkButton.Click += (s, e) => SwitchTheme("DarkTheme");
            var blueButton = new Button { Content = "🔵", Width = 30, Margin = new Thickness(2) };
            blueButton.Click += (s, e) => SwitchTheme("BlueTheme");

            stackPanel.Children.Add(lightButton);
            stackPanel.Children.Add(darkButton);
            stackPanel.Children.Add(blueButton);

            GroupPanel.Children.Insert(0, stackPanel);
        }

        private void SwitchTheme(string theme)
        {
            var dict = new ResourceDictionary();
            dict.Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        // ボタンクリックイベント（テーマ切り替え）
        private void LightTheme_Click(object sender, RoutedEventArgs e) => SwitchTheme("LightTheme");
        private void DarkTheme_Click(object sender, RoutedEventArgs e) => SwitchTheme("DarkTheme");
        private void BlueTheme_Click(object sender, RoutedEventArgs e) => SwitchTheme("BlueTheme");

        private void GroupPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            groupDragStartPoint = e.GetPosition(null);
        }

        private void GroupPanel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(null);
                if ((Math.Abs(currentPos.X - groupDragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                    (Math.Abs(currentPos.Y - groupDragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    var draggedItem = e.OriginalSource as DependencyObject;
                    while (draggedItem != null && draggedItem is not GroupBox)
                    {
                        draggedItem = VisualTreeHelper.GetParent(draggedItem);
                    }
                    if (draggedItem is GroupBox groupBox)
                    {
                        draggedGroup = groupBox;
                        DragDrop.DoDragDrop(groupBox, groupBox, DragDropEffects.Move);
                    }
                }
            }
        }

        private void GroupPanel_Drop(object sender, DragEventArgs e)
        {
            if (draggedGroup == null) return;
            var target = e.OriginalSource as DependencyObject;
            while (target != null && target is not GroupBox)
            {
                target = VisualTreeHelper.GetParent(target);
            }
            if (target is GroupBox targetGroup)
            {
                int oldIndex = GroupPanel.Children.IndexOf(draggedGroup);
                int newIndex = GroupPanel.Children.IndexOf(targetGroup);
                if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                {
                    GroupPanel.Children.Remove(draggedGroup);
                    GroupPanel.Children.Insert(newIndex, draggedGroup);
                    groupOrder = GroupPanel.Children.OfType<GroupBox>()
                        .Select(g => ((Label)((StackPanel)g.Header).Children[0]).Content?.ToString() ?? "未分類")
                        .ToList();
                    File.WriteAllText(groupOrderPath, JsonSerializer.Serialize(groupOrder));
                }
            }
            draggedGroup = null;
        }

    }
}