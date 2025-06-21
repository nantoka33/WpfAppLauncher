using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppLauncher.Services
{
    public static class ThemeSwitcher
    {
        public static void AddThemeSwitcher(Panel targetPanel, Action<string> onThemeChange)
        {
            var lightButton = new Button { Content = "☀", Width = 30, Margin = new Thickness(2) };
            var darkButton = new Button { Content = "🌙", Width = 30, Margin = new Thickness(2) };
            var blueButton = new Button { Content = "🔵", Width = 30, Margin = new Thickness(2) };

            lightButton.Click += (s, e) => onThemeChange("LightTheme");
            darkButton.Click += (s, e) => onThemeChange("DarkTheme");
            blueButton.Click += (s, e) => onThemeChange("BlueTheme");

            targetPanel.Children.Add(lightButton);
            targetPanel.Children.Add(darkButton);
            targetPanel.Children.Add(blueButton);
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
