using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfAppLauncher.Configuration;

namespace WpfAppLauncher.Services
{
    public static class ThemeSwitcher
    {
        public static void AddThemeSwitcher(Panel targetPanel, IEnumerable<ThemeOption> themeOptions, Action<string> onThemeChange)
        {
            targetPanel.Children.Clear();

            var addedThemeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var option in themeOptions ?? Enumerable.Empty<ThemeOption>())
            {
                if (string.IsNullOrWhiteSpace(option.Name))
                {
                    continue;
                }

                if (!addedThemeNames.Add(option.Name))
                {
                    continue;
                }

                var button = new Button
                {
                    Content = string.IsNullOrWhiteSpace(option.Icon) ? option.Name : option.Icon,
                    Width = 30,
                    Margin = new Thickness(2),
                    ToolTip = option.Tooltip ?? option.Name
                };

                button.Click += (s, e) => onThemeChange(option.Name);

                targetPanel.Children.Add(button);
            }
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
