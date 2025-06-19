using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace WpfAppLauncher.Services
{
    public static class IconLoader
    {
        public static BitmapImage LoadIcon(AppEntry app, string iconCacheDir)
        {
            if (!string.IsNullOrEmpty(app.IconPath) && File.Exists(app.IconPath))
            {
                return new BitmapImage(new Uri(Path.GetFullPath(app.IconPath)));
            }
            else
            {
                try
                {
                    if (string.IsNullOrEmpty(app.Path)) throw new ArgumentNullException(nameof(app.Path));
                    var icon = Icon.ExtractAssociatedIcon(app.Path);
                    if (icon == null) throw new InvalidOperationException($"アイコンを取得できません: {app.Path}");
                    string iconFile = Path.Combine(iconCacheDir, Path.GetFileNameWithoutExtension(app.Path) + ".png");
                    using (var bmp = icon.ToBitmap())
                    {
                        bmp.Save(iconFile);
                    }
                    app.IconPath = iconFile;
                    return new BitmapImage(new Uri(Path.GetFullPath(iconFile)));
                }
                catch
                {
                    return new BitmapImage();
                }
            }
        }
    }
}
