using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppLauncher
{
    public class AppEntry
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Group { get; set; }
        public string? IconPath { get; set; }  // アイコンキャッシュ
    }

}
