using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ini;
using Path = System.IO.Path;

namespace X1nputConfigurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<HID.HID_DEVICE> foundDevices = new List<HID.HID_DEVICE>();

        private List<Process> foundProcesses = new List<Process>();

        private List<Process> injectedProcesses = new List<Process>();

        private static IniFile config = new IniFile(@".\X1nput.ini");

        private static Dictionary<uint, string> controllerIDs = new Dictionary<uint, string>()
        {
            {0x2FF, "Xbox Controller (Wired)"},
            {0x2EA, "Xbox One S Controller (Wireless)"},
            {0x2E0, "Xbox One S Controller (Bluetooth)"},
            {0xB12, "Xbox Series X/S Controller (Wireless)"},
            {0xB13, "Xbox Series X/S Controller (Bluetooth)"},
        };

        /* Maybe later

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpFilename, uint nSize);

        */

        public MainWindow()
        {
            InitializeComponent();



            RefreshDevices();
        }

        void RefreshProcesses()
        {
            var processlist = Process.GetProcesses();

            // Don't have access to processes outside of our current session... I think
            var sessionId = Process.GetCurrentProcess().SessionId;

            var winRoot = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System));

            Processes.Items.Clear();
            InjectedProcesses.Items.Clear();

            foundProcesses.Clear();
            injectedProcesses.Clear();

            foreach (var process in processlist)
            {
                if (process.SessionId == sessionId)
                {
                    // I want YOU for the search of a way to check the elevation status of a process... Or you could just, y'know, run the app as an administrator (This applied back when I just tried to use process.MainModule to see if this process has access)
                    var test = false;
                    try
                    {
                        test = process.MainModule.FileName.StartsWith(winRoot.FullName);
                    }
                    catch
                    {
                        continue;
                    }

                    if (test)
                    {
                        continue;
                    }

                    var foundXinput = false;

                    var foundX1nput = false;


                    // I won't give up on injecting to 32 bit processes from 64 bit app just yet
                    /*
                    // Setting up the variable for the second argument for EnumProcessModules
                    IntPtr[] hMods = new IntPtr[1024];

                    GCHandle gch = GCHandle.Alloc(hMods, GCHandleType.Pinned); // Don't forget to free this later
                    IntPtr pModules = gch.AddrOfPinnedObject();

                    // Setting up the rest of the parameters for EnumProcessModules
                    uint uiSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * (hMods.Length));
                    uint cbNeeded = 0;

                    if (EnumProcessModulesEx(process.Handle, pModules, uiSize, out cbNeeded, 0x03) == 1)
                    {
                        Int32 uiTotalNumberofModules = (Int32)(cbNeeded / (Marshal.SizeOf(typeof(IntPtr))));

                        for (int i = 0; i < (int)uiTotalNumberofModules; i++)
                        {
                            StringBuilder strbld = new StringBuilder(1024);

                            GetModuleFileNameEx(process.Handle, hMods[i], strbld, (uint)(strbld.Capacity));

                            var lower = strbld.ToString().ToLower();
                            if (lower.Contains("xinput"))
                            {
                                foundXinput = true;
                            }

                            if (lower.Contains("x1nput") && !lower.Contains("x1nputconfigurator"))
                            {
                                foundX1nput = true;
                            }
                        }
                        Debug.WriteLine("Number of Modules: " + uiTotalNumberofModules);
                        Debug.WriteLine("");
                    }

                    // Must free the GCHandle object
                    gch.Free();
                    */
                    // Wouldn't list 32-bit modules in 32-bit processes so I had to replace it with PInvoke

                    
                    foreach (ProcessModule module in process.Modules)
                    {
                        var name = module.ModuleName.ToLower();
                        if (!foundXinput && name.StartsWith("xinput"))
                        {
                            foundXinput = true;
                        }
                        if (name.StartsWith("x1nput") && !name.StartsWith("x1nputconfigurator"))
                        {
                            foundX1nput = true;
                            break;
                        }
                    }
                    
                    if (foundX1nput)
                    {
                        var proc = new ListBoxItem()
                        {
                            Content = process.ProcessName,
                        };

                        injectedProcesses.Add(process);
                        InjectedProcesses.Items.Add(proc);
                    }
                    else if (foundXinput)
                    {
                        var proc = new ListBoxItem()
                        {
                            Content = process.ProcessName,
                        };

                        foundProcesses.Add(process);
                        Processes.Items.Add(proc);
                    }
                }
            }
        }

        void RefreshDevices()
        {
            Devices.Items.Clear();
            foundDevices.Clear();
            var numDevices = HID.FindNumberDevices();
            var devices = new HID.HID_DEVICE[numDevices];
            HID.FindKnownHidDevices(ref devices);

            var vendor = int.Parse(config.IniReadValue("Controller", "VendorID", "1118"));

            foreach (var device in devices)
            {
                if (device.Attributes.VendorID == vendor)
                {
                    var split = device.DevicePath.Split('\\');
                    var split2 = split.Last();
                    var split3 = split2.Split('#');

                    var split4 = split3[1].Split('&');
                    var split5 = split4[2].Split('_');

                    var devicePath = string.Join(@"\", split3, 0, 3).ToUpper();

                    var name = HID.GetProductString(device);

                    if (controllerIDs.ContainsKey(device.Attributes.ProductID))
                        name = controllerIDs[device.Attributes.ProductID];

                    uint id;
                    if(uint.TryParse(split5[1], NumberStyles.HexNumber, null, out id) && id > 0)
                        name += $" ({id/2})";

                    var dev = new ListBoxItem()
                    {
                        ToolTip = new ToolTip{Content = devicePath},
                        Content = name,
                    };

                    foundDevices.Add(device);

                    Devices.Items.Add(dev);
                }
            }
        }

        private void CopyConfig(Process process)
        {
            var path = Path.GetDirectoryName(process.MainModule.FileName);
            if(OverrideConfig.IsChecked == true)
                File.Copy("X1nput.ini", $@"{path}\X1nput.ini", true);
        }

        private void TestDevClick(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
                HID.Write(foundDevices[Devices.SelectedIndex]);
        }

        private void UseDevClick(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
                config.IniWriteValue("Controller", "ProductID", foundDevices[Devices.SelectedIndex].Attributes.ProductID.ToString());
        }

        private void RefreshDevClick(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private void InjectClick(object sender, RoutedEventArgs e)
        {
            var sel = Processes.SelectedIndex;
            if (sel != -1)
            {
                var process = foundProcesses[sel];

                Injector.Inject(process);
                foundProcesses.RemoveAt(sel);
                Processes.Items.RemoveAt(sel);

                CopyConfig(process);

                var proc = new ListBoxItem()
                {
                    Content = process.ProcessName,
                };

                injectedProcesses.Add(process);
                InjectedProcesses.Items.Add(proc);
            }
        }

        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshProcesses();
        }

        private void UnloadClick(object sender, RoutedEventArgs e)
        {
            var sel = InjectedProcesses.SelectedIndex;

            if (sel != -1)
            {
                var process = injectedProcesses[sel];

                Injector.UnDeInject(process);
                injectedProcesses.RemoveAt(sel);
                InjectedProcesses.Items.RemoveAt(sel);

                var proc = new ListBoxItem()
                {
                    Content = process.ProcessName,
                };

                foundProcesses.Add(process);
                Processes.Items.Add(proc);
            }
        }

        private void ReloadClick(object sender, RoutedEventArgs e)
        {
            if (InjectedProcesses.SelectedIndex != -1)
            {
                var process = injectedProcesses[InjectedProcesses.SelectedIndex];

                CopyConfig(process);

                Injector.UnDeInject(process);
                Injector.Inject(process);
            }
        }

        private void OpenConfig(object sender, RoutedEventArgs e)
        {
            var configWin = new ConfigurationWindow();
            configWin.Show();
        }
    }
}