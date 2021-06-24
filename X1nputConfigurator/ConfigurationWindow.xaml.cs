using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Ini;

namespace X1nputConfigurator
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        private IniFile config = new IniFile(@".\X1nput.ini");

        private int vendorID = 1118;
        private int productID = 746;

        private float leftStrength = 1;
        private float rightStrength = 1;

        private float leftInputModifierBase = 100;
        private float rightInputModifierBase = 100;

        private int leftTriggerLink = 0;
        private int rightTriggerLink = 0;

        private float leftMotorStrength = 1;
        private float rightMotorStrength = 1;

        private bool swapSides = false;


        public ConfigurationWindow()
        {
            InitializeComponent();
            Load();
        }

        void Load()
        {
            vendorID = int.Parse(config.IniReadValue("Controller", "VendorID", "1118"));
            VendorID.Text = vendorID.ToString();

            productID = int.Parse(config.IniReadValue("Controller", "ProductID", "746"));
            ProductID.Text = productID.ToString();

            leftStrength = float.Parse(config.IniReadValue("Triggers", "LeftStrength", "1.0"), CultureInfo.InvariantCulture);
            LeftStrength.Text = leftStrength.ToString();

            rightStrength = float.Parse(config.IniReadValue("Triggers", "RightStrength", "1.0"), CultureInfo.InvariantCulture);
            RightStrength.Text = rightStrength.ToString();

            leftInputModifierBase = float.Parse(config.IniReadValue("Triggers", "LeftInputModifierBase", "100.0"), CultureInfo.InvariantCulture);
            LeftInputModifierBase.Text = leftInputModifierBase.ToString();

            rightInputModifierBase = float.Parse(config.IniReadValue("Triggers", "RightInputModifierBase", "100.0"), CultureInfo.InvariantCulture);
            RightInputModifierBase.Text = rightInputModifierBase.ToString();

            leftTriggerLink = int.Parse(config.IniReadValue("Triggers", "LeftTriggerLink", "0"));
            LeftTriggerLink.SelectedIndex = leftTriggerLink;

            rightTriggerLink = int.Parse(config.IniReadValue("Triggers", "RightTriggerLink", "0"));
            RightTriggerLink.SelectedIndex = rightTriggerLink;

            leftMotorStrength = float.Parse(config.IniReadValue("Motors", "LeftStrength", "1.0"), CultureInfo.InvariantCulture);
            LeftMotorStrength.Text = leftMotorStrength.ToString();

            rightMotorStrength = float.Parse(config.IniReadValue("Motors", "RightStrength", "1.0"), CultureInfo.InvariantCulture);
            RightMotorStrength.Text = rightMotorStrength.ToString();

            swapSides = bool.Parse(config.IniReadValue("Motors", "SwapSides", "False"));
            SwapSides.IsChecked = swapSides;
        }

        private void IntOnly_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }

        private void IntOnly_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void FloatOnly_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _) && e.Text != "." && e.Text != ",")
            {
                e.Handled = true;
            }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            vendorID = int.Parse(VendorID.Text);
            config.IniWriteValue("Controller", "VendorID", VendorID.Text);

            productID = int.Parse(ProductID.Text);
            config.IniWriteValue("Controller", "ProductID", ProductID.Text);

            leftStrength = float.Parse(LeftStrength.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            config.IniWriteValue("Triggers", "LeftStrength", leftStrength.ToString(CultureInfo.InvariantCulture));

            rightStrength = float.Parse(RightStrength.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            config.IniWriteValue("Triggers", "RightStrength", rightStrength.ToString(CultureInfo.InvariantCulture));

            leftInputModifierBase = float.Parse(LeftInputModifierBase.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            config.IniWriteValue("Triggers", "LeftInputModifierBase", leftInputModifierBase.ToString(CultureInfo.InvariantCulture));

            rightInputModifierBase = float.Parse(RightInputModifierBase.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            config.IniWriteValue("Triggers", "RightInputModifierBase", rightInputModifierBase.ToString(CultureInfo.InvariantCulture));

            leftTriggerLink = LeftTriggerLink.SelectedIndex;
            config.IniWriteValue("Triggers", "LeftTriggerLink", LeftTriggerLink.SelectedIndex.ToString());

            rightTriggerLink = RightTriggerLink.SelectedIndex;
            config.IniWriteValue("Triggers", "RightTriggerLink", RightTriggerLink.SelectedIndex.ToString());

            leftMotorStrength = float.Parse(LeftMotorStrength.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            config.IniWriteValue("Motors", "LeftStrength", leftMotorStrength.ToString(CultureInfo.InvariantCulture));

            rightMotorStrength = float.Parse(RightMotorStrength.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            config.IniWriteValue("Motors", "RightStrength", rightMotorStrength.ToString(CultureInfo.InvariantCulture));

            swapSides = SwapSides.IsChecked ?? false;
            config.IniWriteValue("Motors", "SwapSides", swapSides.ToString());
        }
    }
}
