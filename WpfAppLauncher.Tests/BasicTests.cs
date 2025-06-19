using System;
using System.Collections.Generic;
using System.IO;
using WpfAppLauncher;
using WpfAppLauncher.Services;
using Xunit;

namespace WpfAppLauncher.Tests
{
    public class BasicTests
    {
        [Fact]
        public void SaveAndLoadApps_WorksCorrectly()
        {
            // 一時ファイルパスを生成
            string tempAppPath = Path.GetTempFileName();
            string tempGroupPath = Path.GetTempFileName();

            try
            {
                // テストデータを作成
                var originalApps = new List<AppEntry>
                {
                    new AppEntry { Name = "TestApp", Path = @"C:\Test\test.exe", Group = "ユニットテスト" }
                };
                var originalGroupOrder = new List<string> { "ユニットテスト" };

                // 保存
                AppDataService.SaveApps(originalApps, originalGroupOrder, tempAppPath, tempGroupPath);

                // 読み込み
                var loadedApps = AppDataService.LoadApps(tempAppPath, tempGroupPath, out var loadedGroupOrder);

                // 検証
                Assert.Single(loadedApps);
                Assert.Equal("TestApp", loadedApps[0].Name);
                Assert.Equal(@"C:\Test\test.exe", loadedApps[0].Path);
                Assert.Equal("ユニットテスト", loadedApps[0].Group);

                Assert.Single(loadedGroupOrder);
                Assert.Equal("ユニットテスト", loadedGroupOrder[0]);
            }
            finally
            {
                // クリーンアップ
                File.Delete(tempAppPath);
                File.Delete(tempGroupPath);
            }
        }
    }
}
