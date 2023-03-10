using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace X1nputConfigurator.Misc
{
    public static class Constants
    {
        private const string LoadLibraryName = "LoadLibraryA";
        private const string FreeLibraryName = "FreeLibraryAndExitThread";
        public static Dictionary<uint, string> ControllerIDs = new Dictionary<uint, string>
        {
            {0x2FF, "Xbox Controller (Wired)"},
            {0x2EA, "Xbox One S Controller (Wireless)"},
            {0x2E0, "Xbox One S Controller (Bluetooth)"},
            {0xB12, "Xbox Series X/S Controller (Wireless)"},
            {0xB13, "Xbox Series X/S Controller (Bluetooth)"},
        };

        public static IniFile Config = new IniFile(@".\X1nput.ini");

        public static int LoadLibrary;
        public static int LoadLibrary32;

        public static int FreeLibrary;
        public static int FreeLibrary32;
        
        static Constants()
        {
            var kernel = PeThing.Parse($@"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\kernel32.dll");
            LoadLibrary = kernel.GetExportAddress(LoadLibraryName);
            FreeLibrary = kernel.GetExportAddress(FreeLibraryName);

            if (IntPtr.Size == 8) // The best 64-bit detection out there
            {
                var kernel32 = PeThing.Parse($@"{Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)}\kernel32.dll");
                LoadLibrary32 = kernel32.GetExportAddress(LoadLibraryName);
                FreeLibrary32 = kernel32.GetExportAddress(FreeLibraryName);
            }
        }
    }

    public struct ProcessInfo
    {
        public Process Process;
        public IntPtr Kernel32;
        public bool IsWOW64;
    }

}