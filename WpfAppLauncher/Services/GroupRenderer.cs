using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfAppLauncher.Services
{
    public class GroupRenderer
    {
        private readonly List<AppEntry> apps;
        private readonly List<string> groupOrder;
        private readonly string savePath;
        private readonly string groupOrderPath;
        private readonly string iconCacheDir;
        private readonly Panel groupPanel;

        private UIElement? draggedGroup;
        private StackPanel? draggedIcon;
        private Point groupDragStartPoint;
        private Point iconDragStartPoint;

        public GroupRenderer(List<AppEntry> apps, List<string> groupOrder, string savePath, string groupOrderPath, string iconCacheDir, Panel groupPanel)
        {
            this.apps = apps;
            this.groupOrder = groupOrder;
            this.savePath = savePath;
            this.groupOrderPath = groupOrderPath;
            this.iconCacheDir = iconCacheDir;
            this.groupPanel = groupPanel;
        }

        public void RenderGroups()
        {
            groupPanel.Children.Clear();

            if (groupOrder.Count == 0)
                groupOrder.AddRange(apps.Select(a => a.Group ?? "未分類").Distinct());

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

                groupBox.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    groupDragStartPoint = e.GetPosition(null);
                    draggedGroup = groupBox;
                };
                groupBox.PreviewMouseMove += (s, e) =>
                {
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
                groupBox.Drop += (s, e) =>
                {
                    if (draggedGroup == null || draggedGroup == groupBox) return;
                    int oldIndex = groupPanel.Children.IndexOf(draggedGroup);
                    int newIndex = groupPanel.Children.IndexOf(groupBox);
                    if (oldIndex >= 0 && newIndex >= 0)
                    {
                        groupPanel.Children.Remove(draggedGroup);
                        groupPanel.Children.Insert(newIndex, draggedGroup);
                        UpdateGroupOrder();
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
                        Source = IconLoader.LoadIcon(app, iconCacheDir)
                    };
                    var btn = new Button
                    {
                        Content = app.Name,
                        Width = 100,
                        Height = 40,
                        Tag = app
                    };
                    btn.Click += (s, e) =>
                    {
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

                    // コンテキストメニュー
                    var contextMenu = new ContextMenu();
                    var deleteItem = new MenuItem { Header = "削除" };
                    deleteItem.Click += (s, e) =>
                    {
                        apps.Remove((AppEntry)btn.Tag);
                        SaveAndRender();
                    };
                    var renameItem = new MenuItem { Header = "名前変更" };
                    renameItem.Click += (s, e) =>
                    {
                        var appEntry = (AppEntry)btn.Tag;
                        string newName = Interaction.InputBox($"「{appEntry.Name ?? "アプリ"}」の新しい名前を入力してください：", "名前の変更", appEntry.Name ?? "アプリ");
                        if (!string.IsNullOrWhiteSpace(newName))
                        {
                            appEntry.Name = newName.Trim();
                            SaveAndRender();
                        }
                    };
                    var groupEditItem = new MenuItem { Header = "グループ変更" };
                    groupEditItem.Click += (s, e) =>
                    {
                        var appEntry = (AppEntry)btn.Tag;
                        string newGroup = DropHandler.PromptGroupInput(appEntry.Name ?? "アプリ", appEntry.Group ?? "未分類");
                        appEntry.Group = newGroup;
                        SaveAndRender();
                    };
                    var adminRunItem = new MenuItem { Header = "管理者として実行" };
                    adminRunItem.Click += (s, e) =>
                    {
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
                    var changePathItem = new MenuItem { Header = "パス変更" };
                    changePathItem.Click += (s, e) =>
                    {
                        var appEntry = (AppEntry)btn.Tag;
                        string newPath = Interaction.InputBox($"「{appEntry.Name ?? "アプリ"}」の新しいパスを入力してください：", "パスの変更", appEntry.Path ?? "アプリ");
                        if (!string.IsNullOrWhiteSpace(newPath) && File.Exists(newPath))
                        {
                            appEntry.Path = newPath.Trim();
                            SaveAndRender();
                        }
                        else 
                        {
                            MessageBox.Show($"指定されたパスは無効です：{newPath}", "パスエラー", MessageBoxButton.OK, MessageBoxImage.Error); 
                        }
                    };
                    contextMenu.Items.Add(adminRunItem);
                    contextMenu.Items.Add(renameItem);
                    contextMenu.Items.Add(groupEditItem);
                    contextMenu.Items.Add(deleteItem);
                    contextMenu.Items.Add(changePathItem);
                    btn.ContextMenu = contextMenu;

                    stack.Children.Add(img);
                    stack.Children.Add(btn);
                    wrap.Children.Add(stack);

                    stack.PreviewMouseLeftButtonDown += (s, e) =>
                    {
                        iconDragStartPoint = e.GetPosition(null);
                        draggedIcon = stack;
                    };
                    stack.PreviewMouseMove += (s, e) =>
                    {
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

                wrap.Drop += (s, e) =>
                {
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
                            SaveAndRender();
                        }
                    }
                    draggedIcon = null;
                };

                groupBox.Content = wrap;
                groupPanel.Children.Add(groupBox);
            }
        }

        private void SaveAndRender()
        {
            AppDataService.SaveApps(apps, groupOrder, savePath, groupOrderPath);
            RenderGroups();
        }

        private void UpdateGroupOrder()
        {
            groupOrder.Clear();
            groupOrder.AddRange(groupPanel.Children.OfType<GroupBox>()
                .Select(g => ((Label)((StackPanel)g.Header).Children[0]).Content?.ToString() ?? "未分類"));
            File.WriteAllText(groupOrderPath, JsonSerializer.Serialize(groupOrder));
        }

        private object CreateGroupHeader(string groupName)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var label = new Label { Content = groupName, FontWeight = FontWeights.Bold };
            label.SetResourceReference(Label.ForegroundProperty, "ForegroundBrush");

            var contextMenu = new ContextMenu();
            var renameItem = new MenuItem { Header = "グループ名を変更" };
            renameItem.Click += (s, e) =>
            {
                string newName = Interaction.InputBox($"「{groupName}」の新しいグループ名を入力してください：", "グループ名の変更", groupName);
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
                    SaveAndRender();
                }
            };
            contextMenu.Items.Add(renameItem);
            label.ContextMenu = contextMenu;

            panel.Children.Add(label);
            return panel;
        }
    }
}
