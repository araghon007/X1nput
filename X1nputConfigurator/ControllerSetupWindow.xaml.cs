using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using X1nputConfigurator.Misc;

namespace X1nputConfigurator
{
    /// <summary>
    /// Interaction logic for ControllerSetupWindow.xaml
    /// </summary>
    public partial class ControllerSetupWindow : Window
    {
        private List<HID.HID_DEVICE> foundDevices = new List<HID.HID_DEVICE>();

        public ControllerSetupWindow()
        {
            InitializeComponent();

            RefreshDevices();

            Automatic.IsChecked = bool.Parse(Constants.Config.IniReadValue("Controllers", "Auto", "True"));
            MultiController.IsChecked = bool.Parse(Constants.Config.IniReadValue("Controllers", "Enabled", "False"));

            RefreshTooltips();
        }

        void RefreshDevices()
        {
            Devices.Items.Clear();
            foreach (var device in foundDevices)
            {
                HID.CloseHidDevice(device);
            }
            foundDevices.Clear();
            var numDevices = HID.FindNumberDevices();
            var devices = new HID.HID_DEVICE[numDevices];
            HID.FindKnownHidDevices(ref devices);

            var vendor = int.Parse(Constants.Config.IniReadValue("Controller", "VendorID", "1118"));

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

                    if (Constants.ControllerIDs.ContainsKey(device.Attributes.ProductID))
                        name = Constants.ControllerIDs[device.Attributes.ProductID];

                    uint id;
                    if (uint.TryParse(split5[1], NumberStyles.HexNumber, null, out id) && id > 0)
                        name += $" ({id / 2})";

                    var dev = new ListBoxItem()
                    {
                        ToolTip = new ToolTip { Content = devicePath },
                        Content = name,
                    };

                    foundDevices.Add(device);

                    Devices.Items.Add(dev);
                }
            }
        }

        private void RefreshTooltips()
        {
            var useVID = Constants.Config.IniReadValue("Controller", "VendorID");
            var usePID = Constants.Config.IniReadValue("Controller", "ProductID");

            if (!string.IsNullOrEmpty(useVID) && !string.IsNullOrEmpty(usePID))
                UseTip.Content = $"Use this device as default controller\nCurrently used (VendorID:{useVID}, ProductID:{usePID})";

            var useOne = Constants.Config.IniReadValue("Controllers", "One");
            if (!string.IsNullOrEmpty(useOne))
                Use1Tip.Content = $"Use this device as controller #1\nCurrently used ({useOne})";

            var useTwo = Constants.Config.IniReadValue("Controllers", "Two");
            if (!string.IsNullOrEmpty(useTwo))
                Use2Tip.Content = $"Use this device as controller #2\nCurrently used ({useTwo})";

            var useThree = Constants.Config.IniReadValue("Controllers", "Three");
            if (!string.IsNullOrEmpty(useThree))
                Use3Tip.Content = $"Use this device as controller #3\nCurrently used ({useThree})";

            var useFour = Constants.Config.IniReadValue("Controllers", "Four");
            if (!string.IsNullOrEmpty(useFour))
                Use4Tip.Content = $"Use this device as controller #4\nCurrently used ({useFour})";
        }

        private void TestDevClick(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
                HID.Write(foundDevices[Devices.SelectedIndex]);
        }

        private void UseDevClick(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
            {
                Constants.Config.IniWriteValue("Controller", "ProductID", foundDevices[Devices.SelectedIndex].Attributes.ProductID.ToString());
                RefreshTooltips();
            }
        }

        private void RefreshDevClick(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var device in foundDevices)
            {
                HID.CloseHidDevice(device);
            }
        }

        private void Automatic_OnClick(object sender, RoutedEventArgs e)
        {
            Constants.Config.IniWriteValue("Controllers", "Auto", (Automatic.IsChecked ?? false).ToString());
        }

        private void MultiController_OnClick(object sender, RoutedEventArgs e)
        {
            Constants.Config.IniWriteValue("Controllers", "Enabled", (MultiController.IsChecked ?? false).ToString());
        }

        private void UseOne(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
            {
                Constants.Config.IniWriteValue("Controllers", "One", foundDevices[Devices.SelectedIndex].DevicePath);
                RefreshTooltips();
            }
        }

        private void UseTwo(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
            {
                Constants.Config.IniWriteValue("Controllers", "Two", foundDevices[Devices.SelectedIndex].DevicePath);
                RefreshTooltips();
            }
        }

        private void UseThree(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
            {
                Constants.Config.IniWriteValue("Controllers", "Three", foundDevices[Devices.SelectedIndex].DevicePath);
                RefreshTooltips();
            }
        }

        private void UseFour(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedIndex != -1)
            {
                Constants.Config.IniWriteValue("Controllers", "Four", foundDevices[Devices.SelectedIndex].DevicePath);
                RefreshTooltips();
            }
        }
    }
}
