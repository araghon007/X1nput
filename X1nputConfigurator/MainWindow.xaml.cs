using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using X1nputConfigurator.Misc;
using X1nputConfigurator.Properties;

namespace X1nputConfigurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<ProcessInfo> foundProcesses = new List<ProcessInfo>();

        private List<ProcessInfo> injectedProcesses = new List<ProcessInfo>();

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpFilename, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

        private DispatcherTimer _timer;
        private TimeSpan _time;
        private const string _startupValue = "Launch X1nput on computer start";
        private const string _startupPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private void InitializeTimer(int duration)
        {
            _time = TimeSpan.FromSeconds(duration);
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                if (_time == TimeSpan.Zero)
                {
                    TimerTick();
                    _time = TimeSpan.FromSeconds(duration);
                }
                _time = _time.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);
            _timer.Start();
        }

        private void TimerTick()
        {
            RefreshProcesses();
            while (Processes.Items.Count > 0)
            {
                var sel = 0;
                var process = foundProcesses[sel];

                CopyConfig(process.Process);

                if (Injector.Inject(process))
                {
                    foundProcesses.RemoveAt(sel);
                    Processes.Items.RemoveAt(sel);

                    var proc = new ListBoxItem
                    {
                        Content = process.Process.ProcessName,
                    };

                    injectedProcesses.Add(process);
                    InjectedProcesses.Items.Add(proc);
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            OverrideConfig.IsChecked = Settings.Default.OverrideConfig;

            InitializeTimer(300);

            RegistryKey key = Registry.LocalMachine.OpenSubKey(_startupPath);
            if (key.GetValue(_startupValue) != null) AutoLaunch.IsChecked = true;
        }

        void RefreshProcesses()
        {
            var processlist = Process.GetProcesses();

            // Don't have access to processes outside of our current session... I think
            var sessionId = Process.GetCurrentProcess().SessionId;

            Processes.Items.Clear();
            InjectedProcesses.Items.Clear();

            foundProcesses.Clear();
            injectedProcesses.Clear();

            foreach (var process in processlist)
            {
                if (process.SessionId == sessionId)
                {
                    // I want YOU for the search of a way to check the elevation status of a process... Or you could just, y'know, run the app as an administrator (This applied back when I just tried to use process.MainModule to see if this process has access)
                    try
                    {
                        if (string.IsNullOrEmpty(process.MainModule.FileName))
                            continue;
                        if (process.Handle == IntPtr.Zero)
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    var foundXinput = false;

                    var foundX1nput = false;


                    // Setting up the variable for the second argument for EnumProcessModules
                    IntPtr[] hMods = new IntPtr[1024];

                    GCHandle gch = GCHandle.Alloc(hMods, GCHandleType.Pinned); // Don't forget to free this later
                    IntPtr pModules = gch.AddrOfPinnedObject();

                    // Setting up the rest of the parameters for EnumProcessModules
                    uint uiSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * hMods.Length);

                    IntPtr kernel32 = IntPtr.Zero;

                    var isWOW64 = false;

                    if (EnumProcessModulesEx(process.Handle, pModules, uiSize, out var cbNeeded, 0x03) == 1)
                    {
                        int uiTotalNumberofModules = (int)(cbNeeded / Marshal.SizeOf(typeof(IntPtr)));

                        for (int i = 0; i < uiTotalNumberofModules; i++)
                        {
                            StringBuilder strbld = new StringBuilder(1024);

                            GetModuleFileNameEx(process.Handle, hMods[i], strbld, (uint)strbld.Capacity);

                            var lower = strbld.ToString().ToLower();
                            if (lower.EndsWith(".dll"))
                            {
                                if (lower.Contains("kernel32"))
                                {
                                    kernel32 = hMods[i];
                                    if (IsWow64Process(process.Handle, out bool isProcessWow64))
                                        isWOW64 = isProcessWow64 && IntPtr.Size == 8;
                                }
                                else if (lower.Contains("x1nput"))
                                {
                                    foundX1nput = true;
                                }
                                else if (lower.Contains("xinput"))
                                {
                                    foundXinput = true;
                                }
                            }
                        }
                    }

                    // Must free the GCHandle object
                    gch.Free();

                    if (foundX1nput)
                    {
                        var proc = new ListBoxItem
                        {
                            Content = process.ProcessName,
                        };

                        var processInfo = new ProcessInfo
                        {
                            Process = process,
                            Kernel32 = kernel32,
                            IsWOW64 = isWOW64
                        };

                        injectedProcesses.Add(processInfo);
                        InjectedProcesses.Items.Add(proc);
                    }
                    else if (foundXinput)
                    {
                        var proc = new ListBoxItem
                        {
                            Content = process.ProcessName,
                        };

                        var processInfo = new ProcessInfo
                        {
                            Process = process,
                            Kernel32 = kernel32,
                            IsWOW64 = isWOW64
                        };

                        foundProcesses.Add(processInfo);
                        Processes.Items.Add(proc);
                    }
                }
            }
        }

        private void CopyConfig(Process process)
        {
            var path = Path.GetDirectoryName(process.MainModule.FileName);
            if (OverrideConfig.IsChecked == true)
                File.Copy("X1nput.ini", $@"{path}\X1nput.ini", true);
        }

        private void InjectClick(object sender, RoutedEventArgs e)
        {
            var sel = Processes.SelectedIndex;
            if (sel != -1)
            {
                var process = foundProcesses[sel];

                CopyConfig(process.Process);

                if (Injector.Inject(process))
                {
                    foundProcesses.RemoveAt(sel);
                    Processes.Items.RemoveAt(sel);


                    var proc = new ListBoxItem
                    {
                        Content = process.Process.ProcessName,
                    };

                    injectedProcesses.Add(process);
                    InjectedProcesses.Items.Add(proc);
                }
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

                if (process.Process.HasExited)
                {
                    injectedProcesses.RemoveAt(sel);
                    InjectedProcesses.Items.RemoveAt(sel);
                }
                else if (Injector.Unload(process))
                {
                    injectedProcesses.RemoveAt(sel);
                    InjectedProcesses.Items.RemoveAt(sel);

                    var proc = new ListBoxItem
                    {
                        Content = process.Process.ProcessName,
                    };

                    foundProcesses.Add(process);
                    Processes.Items.Add(proc);
                }
            }
        }

        private void ReloadClick(object sender, RoutedEventArgs e)
        {
            var sel = InjectedProcesses.SelectedIndex;

            if (sel != -1)
            {
                var process = injectedProcesses[sel];

                if (process.Process.HasExited)
                {
                    injectedProcesses.RemoveAt(sel);
                    InjectedProcesses.Items.RemoveAt(sel);
                }
                else
                {
                    CopyConfig(process.Process);

                    if (Injector.Unload(process))
                        Injector.Inject(process);
                }
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
            Settings.Default.OverrideConfig = OverrideConfig.IsChecked ?? false;
            Settings.Default.Save();
        }

        private void AutoInject_Click(object sender, RoutedEventArgs e)
        {
            if (AutoInject.IsChecked == true) _timer.Start();
            else _timer.Stop();
        }

        private void AutoLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (AutoLaunch.IsChecked == true)
            {
                RegistryKey saveKey = Registry.LocalMachine.CreateSubKey(_startupPath);
                saveKey.SetValue(_startupValue, Assembly.GetExecutingAssembly().Location);
                saveKey.Close();
            }
            else
            {
                RegistryKey deleteKey = Registry.LocalMachine.OpenSubKey(_startupPath, true);
                deleteKey.DeleteValue(_startupValue);
                deleteKey.Close();
            }
        }
    }
}