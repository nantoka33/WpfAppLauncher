using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WpfAppLauncher.Services
{
    public static class AppDataService
    {
        public static List<AppEntry> LoadApps(string savePath, string groupOrderPath, out List<string> groupOrder)
        {
            List<AppEntry> apps = new();
            groupOrder = new();

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

            return apps;
        }

        public static void SaveApps(List<AppEntry> apps, List<string> groupOrder, string savePath, string groupOrderPath)
        {
            File.WriteAllText(savePath, JsonSerializer.Serialize(apps));
            File.WriteAllText(groupOrderPath, JsonSerializer.Serialize(groupOrder));
        }
    }
}
