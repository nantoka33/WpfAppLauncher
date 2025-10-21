using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog;
using WpfAppLauncher.Configuration;

namespace WpfAppLauncher.Extensions
{
    internal sealed class ExtensionStateStore
    {
        private readonly ILogger _logger;
        private readonly string _stateFilePath;
        private readonly object _syncRoot = new();
        private StateModel _state;

        public ExtensionStateStore(AppDataSettings appData, ExtensionSettings extensionSettings, ILogger logger)
        {
            _logger = logger;

            var appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appData.ApplicationDirectoryName);

            Directory.CreateDirectory(appDataDirectory);

            _stateFilePath = Path.Combine(appDataDirectory, extensionSettings.StateFileName);
            _state = LoadState();
        }

        public IReadOnlyCollection<string> GetDisabledExtensionIds()
        {
            lock (_syncRoot)
            {
                return _state.Disabled.Select(id => id).ToArray();
            }
        }

        public void SetExtensionEnabled(string extensionId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(extensionId))
            {
                return;
            }

            lock (_syncRoot)
            {
                var comparer = StringComparer.OrdinalIgnoreCase;

                if (enabled)
                {
                    _state.Disabled.RemoveAll(id => comparer.Equals(id, extensionId));
                }
                else if (_state.Disabled.All(id => !comparer.Equals(id, extensionId)))
                {
                    _state.Disabled.Add(extensionId);
                }

                SaveState();
            }
        }

        private StateModel LoadState()
        {
            try
            {
                if (!File.Exists(_stateFilePath))
                {
                    return new StateModel();
                }

                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<StateModel>(json);
                return state ?? new StateModel();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "拡張機能の状態ファイルを読み込めませんでした。既定値を使用します: {StateFile}", _stateFilePath);
                return new StateModel();
            }
        }

        private void SaveState()
        {
            try
            {
                var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "拡張機能の状態ファイルを保存できませんでした: {StateFile}", _stateFilePath);
            }
        }

        private sealed class StateModel
        {
            public List<string> Disabled { get; set; } = new();
        }
    }
}
