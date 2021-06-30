using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using X1nputConfigurator.Misc;

namespace X1nputConfigurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<Process> foundProcesses = new List<Process>();

        private List<Process> injectedProcesses = new List<Process>();



        /* Maybe later

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpFilename, uint nSize);

        */

        public MainWindow()
        {
            InitializeComponent();

            OverrideConfig.IsChecked = Settings.Data.OverrideConfig;
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

        private void CopyConfig(Process process)
        {
            var path = Path.GetDirectoryName(process.MainModule.FileName);
            if(OverrideConfig.IsChecked == true)
                File.Copy("X1nput.ini", $@"{path}\X1nput.ini", true);
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
            new ConfigurationWindow().Show();
        }

        private void OpenSetup(object sender, RoutedEventArgs e)
        {
            new ControllerSetupWindow().Show();
        }

        private void OverrideConfig_OnClick(object sender, RoutedEventArgs e)
        {
            Settings.Data.OverrideConfig = OverrideConfig.IsChecked ?? false;
            Settings.Save();
        }
    }
}