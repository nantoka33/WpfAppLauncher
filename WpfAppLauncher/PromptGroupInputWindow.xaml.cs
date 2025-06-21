using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppLauncher.Views
{
    public partial class PromptGroupInputWindow : Window
    {
        public string GroupName { get; private set; } = "未分類";

        public PromptGroupInputWindow(IEnumerable<string> existingGroups, string currentGroup = "未分類")
        {
            InitializeComponent();

            GroupInputBox.Text = currentGroup;

            foreach (var group in existingGroups.Distinct().Where(g => !string.IsNullOrWhiteSpace(g)))
            {
                var btn = new Button
                {
                    Content = group,
                    Margin = new Thickness(5),
                    Padding = new Thickness(10)
                };
                btn.Click += (_, _) => GroupInputBox.Text = group;
                GroupButtonsPanel.Children.Add(btn);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            GroupName = string.IsNullOrWhiteSpace(GroupInputBox.Text) ? "未分類" : GroupInputBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
