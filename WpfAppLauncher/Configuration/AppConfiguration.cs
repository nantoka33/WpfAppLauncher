using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfAppLauncher.Configuration
{
    public static class AppConfiguration
    {
        private static readonly IReadOnlyList<ThemeOption> DefaultThemeOptions = new List<ThemeOption>
        {
            new() { Name = "LightTheme", Icon = "☀", Tooltip = "ライトテーマ" },
            new() { Name = "DarkTheme", Icon = "🌙", Tooltip = "ダークテーマ" },
            new() { Name = "BlueTheme", Icon = "🔵", Tooltip = "ブルーテーマ" },
        };

        private static readonly Lazy<IConfigurationRoot> ConfigurationRootLazy = new(BuildConfiguration);
        private static readonly Lazy<AppSettings> SettingsLazy = new(() =>
        {
            var settings = new AppSettings();
            ConfigurationRootLazy.Value.Bind(settings);
            NormalizeThemeSettings(settings.Themes);
            return settings;
        });

        public static IConfigurationRoot Configuration => ConfigurationRootLazy.Value;

        public static AppSettings Current => SettingsLazy.Value;

        private static IConfigurationRoot BuildConfiguration()
        {
            var environment = GetEnvironmentName();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "WPFAPPLAUNCHER_");

            return builder.Build();
        }

        private static void NormalizeThemeSettings(ThemeSettings themeSettings)
        {
            if (themeSettings == null)
            {
                return;
            }

            themeSettings.StateFileName = string.IsNullOrWhiteSpace(themeSettings.StateFileName)
                ? "theme.json"
                : themeSettings.StateFileName;

            var options = themeSettings.Options ?? new List<ThemeOption>();

            var distinctOptions = options
                .Where(option => !string.IsNullOrWhiteSpace(option.Name))
                .GroupBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var option = group.First();
                    return new ThemeOption
                    {
                        Name = option.Name,
                        Icon = option.Icon,
                        Tooltip = option.Tooltip,
                    };
                })
                .ToList();

            if (distinctOptions.Count == 0)
            {
                distinctOptions = DefaultThemeOptions
                    .Select(option => new ThemeOption
                    {
                        Name = option.Name,
                        Icon = option.Icon,
                        Tooltip = option.Tooltip,
                    })
                    .ToList();
            }

            themeSettings.Options = distinctOptions;

            if (string.IsNullOrWhiteSpace(themeSettings.Default) ||
                !themeSettings.Options.Any(option =>
                    string.Equals(option.Name, themeSettings.Default, StringComparison.OrdinalIgnoreCase)))
            {
                themeSettings.Default = themeSettings.Options.First().Name;
            }
        }

        private static string GetEnvironmentName()
        {
#if DEBUG
            const string defaultEnvironment = "Development";
#else
            const string defaultEnvironment = "Production";
#endif
            return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                   ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                   ?? defaultEnvironment;
        }
    }
}
