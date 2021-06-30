using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X1nputConfigurator.Misc
{
    public static class Constants
    {
        public static Dictionary<uint, string> ControllerIDs = new Dictionary<uint, string>()
        {
            {0x2FF, "Xbox Controller (Wired)"},
            {0x2EA, "Xbox One S Controller (Wireless)"},
            {0x2E0, "Xbox One S Controller (Bluetooth)"},
            {0xB12, "Xbox Series X/S Controller (Wireless)"},
            {0xB13, "Xbox Series X/S Controller (Bluetooth)"},
        };

        public static IniFile Config = new IniFile(@".\X1nput.ini");
    }
}
