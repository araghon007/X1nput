using System;
using System.Collections.Generic;
using System.Diagnostics;
using PeNet;

namespace X1nputConfigurator.Misc
{
    public static class Constants
    {
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
            var kernel = new PeFile($@"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\kernel32.dll");
            var kernelExports = kernel.ExportedFunctions;
            foreach (var export in kernelExports)
            {
                if (export.Name == "LoadLibraryA")
                    LoadLibrary = (int) export.Address;
                else if(export.Name == "FreeLibraryAndExitThread")
                    FreeLibrary = (int)export.Address;
            }

            if (IntPtr.Size == 8) // The best 64-bit detection out there
            {
                var kernel32  = new PeFile($@"{Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)}\kernel32.dll");
                var kernelExports32 = kernel32.ExportedFunctions;
                foreach (var export in kernelExports32)
                {
                    if (export.Name == "LoadLibraryA")
                        LoadLibrary32 = (int)export.Address;
                    else if (export.Name == "FreeLibraryAndExitThread")
                        FreeLibrary32 = (int)export.Address;
                }
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