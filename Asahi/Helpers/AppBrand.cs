using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Helpers
{
    public static class AppBrand
    {
        public static string AppName => "Asahi";
        public static string AppFolderName => "AsahiApp";
        public static string Publisher => "AMV";
        public const string Version = "0.0";

        public static string InstallPath => $@"C:\{Publisher}\{AppFolderName}\{Version}";
        public static string ProgramDataPath => $@"C:\ProgramData\{Publisher}\{AppFolderName}\{Version}";
    }
}
