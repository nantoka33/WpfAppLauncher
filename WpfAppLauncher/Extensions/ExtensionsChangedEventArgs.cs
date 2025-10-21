using System;
using System.Collections.Generic;

namespace WpfAppLauncher.Extensions
{
    public sealed class ExtensionsChangedEventArgs : EventArgs
    {
        public ExtensionsChangedEventArgs(IReadOnlyList<ExtensionSnapshot> extensions)
        {
            Extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
        }

        public IReadOnlyList<ExtensionSnapshot> Extensions { get; }
    }
}
