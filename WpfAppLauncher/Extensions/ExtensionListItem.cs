using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfAppLauncher.Extensions
{
    internal sealed class ExtensionListItem : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _statusMessage = string.Empty;
        private string _lastUpdatedDisplay = string.Empty;

        public ExtensionListItem(ExtensionSnapshot snapshot)
        {
            Id = snapshot.Id;
            DisplayName = snapshot.DisplayName;
            Description = snapshot.Description;
            Version = snapshot.Version;
            AssemblyPath = snapshot.AssemblyPath;
            Update(snapshot);
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string? Description { get; }

        public string Version { get; }

        public string AssemblyPath { get; }

        public bool DisabledByConfiguration { get; private set; }

        public bool DisabledByUser { get; private set; }

        public bool InitializationFailed { get; private set; }

        public bool IsLoaded { get; private set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastUpdatedDisplay
        {
            get => _lastUpdatedDisplay;
            private set
            {
                if (_lastUpdatedDisplay != value)
                {
                    _lastUpdatedDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanToggle => !DisabledByConfiguration;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Update(ExtensionSnapshot snapshot)
        {
            DisabledByConfiguration = snapshot.DisabledByConfiguration;
            DisabledByUser = snapshot.DisabledByUser;
            InitializationFailed = snapshot.InitializationFailed;
            IsLoaded = snapshot.IsLoaded;
            IsEnabled = snapshot.IsEnabled;
            StatusMessage = snapshot.StatusMessage;
            LastUpdatedDisplay = snapshot.LastUpdatedUtc.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

            OnPropertyChanged(nameof(DisabledByConfiguration));
            OnPropertyChanged(nameof(DisabledByUser));
            OnPropertyChanged(nameof(InitializationFailed));
            OnPropertyChanged(nameof(IsLoaded));
            OnPropertyChanged(nameof(CanToggle));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
