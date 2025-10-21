using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppLauncher.Extensions
{
    public partial class ExtensionManagerWindow : Window
    {
        private readonly ObservableCollection<ExtensionListItem> _extensions = new();
        private bool _suppressToggleEvents;

        public ExtensionManagerWindow()
        {
            InitializeComponent();
            ExtensionListView.ItemsSource = _extensions;

            if (ExtensionHost.IsInitialized)
            {
                LoadSnapshot(ExtensionHost.Manager.GetExtensionsSnapshot());
                ExtensionHost.Manager.ExtensionsReloaded += OnExtensionsReloaded;
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExtensionHost.IsInitialized)
            {
                ExtensionHost.Manager.ReloadExtensions();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ExtensionToggle_Checked(object sender, RoutedEventArgs e)
        {
            HandleToggle(sender as CheckBox, enabled: true);
        }

        private void ExtensionToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            HandleToggle(sender as CheckBox, enabled: false);
        }

        private void HandleToggle(CheckBox? checkBox, bool enabled)
        {
            if (_suppressToggleEvents || checkBox?.Tag is not ExtensionListItem item)
            {
                return;
            }

            if (!ExtensionHost.IsInitialized)
            {
                return;
            }

            bool result;
            string? message;

            if (enabled)
            {
                result = ExtensionHost.Manager.TryEnableExtension(item.Id, out message);
            }
            else
            {
                result = ExtensionHost.Manager.TryDisableExtension(item.Id, out message);
            }

            if (!result)
            {
                _suppressToggleEvents = true;
                checkBox.IsChecked = !enabled;
                _suppressToggleEvents = false;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    MessageBox.Show(this, message, "拡張機能", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void LoadSnapshot(IReadOnlyList<ExtensionSnapshot> snapshots)
        {
            _suppressToggleEvents = true;

            foreach (var snapshot in snapshots)
            {
                var item = _extensions.FirstOrDefault(x => string.Equals(x.Id, snapshot.Id, StringComparison.OrdinalIgnoreCase));
                if (item is null)
                {
                    _extensions.Add(new ExtensionListItem(snapshot));
                }
                else
                {
                    item.Update(snapshot);
                }
            }

            for (var index = _extensions.Count - 1; index >= 0; index--)
            {
                if (!snapshots.Any(snapshot => string.Equals(snapshot.Id, _extensions[index].Id, StringComparison.OrdinalIgnoreCase)))
                {
                    _extensions.RemoveAt(index);
                }
            }

            _suppressToggleEvents = false;
        }

        private void OnExtensionsReloaded(object? sender, ExtensionsChangedEventArgs e)
        {
            Dispatcher.Invoke(() => LoadSnapshot(e.Extensions));
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ExtensionHost.IsInitialized)
            {
                ExtensionHost.Manager.ExtensionsReloaded -= OnExtensionsReloaded;
            }

            base.OnClosed(e);
        }
    }
}
