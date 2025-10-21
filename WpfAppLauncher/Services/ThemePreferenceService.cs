using System;
using System.IO;

namespace WpfAppLauncher.Services
{
    public static class ThemePreferenceService
    {
        public static string? LoadPreferredTheme(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return null;
                }

                var theme = File.ReadAllText(filePath).Trim();
                return string.IsNullOrWhiteSpace(theme) ? null : theme;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        public static void SavePreferredTheme(string filePath, string theme)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(theme))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, theme);
            }
            catch (IOException)
            {
                // ignore persistence errors
            }
            catch (UnauthorizedAccessException)
            {
                // ignore persistence errors
            }
        }
    }
}
