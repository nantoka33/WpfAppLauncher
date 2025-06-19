using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppLauncher.Services
{
    public static class ThemeSwitcher
    {
        public static void AddThemeSwitcher(Panel targetPanel, Action<string> onThemeChange)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5)
            };

            var lightButton = new Button { Content = "☀", Width = 30, Margin = new Thickness(2) };
            var darkButton = new Button { Content = "🌙", Width = 30, Margin = new Thickness(2) };
            var blueButton = new Button { Content = "🔵", Width = 30, Margin = new Thickness(2) };

            lightButton.Click += (s, e) => onThemeChange("LightTheme");
            darkButton.Click += (s, e) => onThemeChange("DarkTheme");
            blueButton.Click += (s, e) => onThemeChange("BlueTheme");

            stackPanel.Children.Add(lightButton);
            stackPanel.Children.Add(darkButton);
            stackPanel.Children.Add(blueButton);

            targetPanel.Children.Insert(0, stackPanel);
        }

        public static void SwitchTheme(string theme)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
