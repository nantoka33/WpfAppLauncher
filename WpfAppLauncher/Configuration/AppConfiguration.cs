using Microsoft.Extensions.Configuration;
using System;

namespace WpfAppLauncher.Configuration
{
    public static class AppConfiguration
    {
        private static readonly Lazy<IConfigurationRoot> ConfigurationRootLazy = new(BuildConfiguration);
        private static readonly Lazy<AppSettings> SettingsLazy = new(() =>
        {
            var settings = new AppSettings();
            ConfigurationRootLazy.Value.Bind(settings);
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
