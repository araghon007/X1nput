// Thanks to wqaxs36 https://www.codeproject.com/Articles/1244702/How-to-Communicate-with-its-USB-Devices-using-HID

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace X1nputConfigurator.Misc
{
    public static class HID
    {

        #region WinAPI

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport(@"setupapi.dll", SetLastError = true)]
        static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

        [DllImport(@"setupapi.dll", SetLastError = true)]
        static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

        [DllImport(@"kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFile(string fileName, uint fileAccess, uint fileShare, FileMapProtection securityAttributes, uint creationDisposition, uint flags, IntPtr overlapped);

        [DllImport("kernel32.dll")]
        static extern bool WriteFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToWrite, ref uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
        
        [DllImport("hid.dll")]
        static extern void HidD_GetHidGuid(ref Guid Guid);

        [DllImport("hid.dll", SetLastError = true)]
        static extern bool HidD_GetPreparsedData(IntPtr HidDeviceObject, ref IntPtr PreparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        static extern bool HidD_GetAttributes(IntPtr DeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll", SetLastError = true)]
        static extern uint HidP_GetCaps(IntPtr PreparsedData, ref HIDP_CAPS Capabilities);

        [DllImport("hid.dll", SetLastError = true)]
        static extern bool HidD_GetProductString(IntPtr HidDeviceObject, byte[] Buffer, int BufferLength);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("hid.dll", SetLastError = true)]
        static extern bool HidD_FreePreparsedData(ref IntPtr PreparsedData);
        
        #endregion

        #region DLL Var

        static IntPtr hardwareDeviceInfo;

        const int DIGCF_PRESENT = 0x00000002;
        const int DIGCF_DEVICEINTERFACE = 0x00000010;

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;

        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;

        const uint OPEN_EXISTING = 3;

        const int DEVICE_PATH = 260;

        enum FileMapProtection : uint
        {
            PageReadonly = 0x02,
            PageReadWrite = 0x04,
            PageWriteCopy = 0x08,
            PageExecuteRead = 0x20,
            PageExecuteReadWrite = 0x40,
            SectionCommit = 0x8000000,
            SectionImage = 0x1000000,
            SectionNoCache = 0x10000000,
            SectionReserve = 0x4000000,
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;
            public int flags;
            private UIntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DEVICE_PATH)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort Usage;
            [MarshalAs(UnmanagedType.U2)]
            public ushort UsagePage;
            [MarshalAs(UnmanagedType.U2)]
            public ushort InputReportByteLength;
            [MarshalAs(UnmanagedType.U2)]
            public ushort OutputReportByteLength;
            [MarshalAs(UnmanagedType.U2)]
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberLinkCollectionNodes;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberInputButtonCaps;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberInputValueCaps;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberInputDataIndices;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberOutputButtonCaps;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberOutputValueCaps;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberOutputDataIndices;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberFeatureButtonCaps;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberFeatureValueCaps;
            [MarshalAs(UnmanagedType.U2)]
            public ushort NumberFeatureDataIndices;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public short VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ButtonData
        {
            public int UsageMin;
            public int UsageMax;
            public int MaxUsageLength;
            public short Usages;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ValueData
        {
            public ushort Usage;
            public ushort Reserved;

            public ulong Value;
            public long ScaledValue;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct HID_DATA
        {
            [FieldOffset(0)]
            public bool IsButtonData;
            [FieldOffset(1)]
            public byte Reserved;
            [FieldOffset(2)]
            public ushort UsagePage;
            [FieldOffset(4)]
            public int Status;
            [FieldOffset(8)]
            public int ReportID;
            [FieldOffset(16)]
            public bool IsDataSet;

            [FieldOffset(17)]
            public ButtonData ButtonData;
            [FieldOffset(17)]
            public ValueData ValueData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_Range
        {
            public ushort UsageMin, UsageMax;
            public ushort StringMin, StringMax;
            public ushort DesignatorMin, DesignatorMax;
            public ushort DataIndexMin, DataIndexMax;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_NotRange
        {
            public ushort Usage, Reserved1;
            public ushort StringIndex, Reserved2;
            public ushort DesignatorIndex, Reserved3;
            public ushort DataIndex, Reserved4;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct HIDP_BUTTON_CAPS
        {
            [FieldOffset(0)]
            public ushort UsagePage;
            [FieldOffset(2)]
            public byte ReportID;
            [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
            public bool IsAlias;
            [FieldOffset(4)]
            public short BitField;
            [FieldOffset(6)]
            public short LinkCollection;
            [FieldOffset(8)]
            public short LinkUsage;
            [FieldOffset(10)]
            public short LinkUsagePage;
            [FieldOffset(12), MarshalAs(UnmanagedType.U1)]
            public bool IsRange;
            [FieldOffset(13), MarshalAs(UnmanagedType.U1)]
            public bool IsStringRange;
            [FieldOffset(14), MarshalAs(UnmanagedType.U1)]
            public bool IsDesignatorRange;
            [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
            public bool IsAbsolute;
            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public int[] Reserved;
            [FieldOffset(56)]
            public HIDP_Range Range;
            [FieldOffset(56)]
            public HIDP_NotRange NotRange;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct HIDP_VALUE_CAPS
        {
            [FieldOffset(0)]
            public ushort UsagePage;
            [FieldOffset(2)]
            public byte ReportID;
            [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
            public bool IsAlias;
            [FieldOffset(4)]
            public ushort BitField;
            [FieldOffset(6)]
            public ushort LinkCollection;
            [FieldOffset(8)]
            public ushort LinkUsage;
            [FieldOffset(10)]
            public ushort LinkUsagePage;
            [FieldOffset(12), MarshalAs(UnmanagedType.U1)]
            public bool IsRange;
            [FieldOffset(13), MarshalAs(UnmanagedType.U1)]
            public bool IsStringRange;
            [FieldOffset(14), MarshalAs(UnmanagedType.U1)]
            public bool IsDesignatorRange;
            [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
            public bool IsAbsolute;
            [FieldOffset(16), MarshalAs(UnmanagedType.U1)]
            public bool HasNull;
            [FieldOffset(17)]
            public byte Reserved;
            [FieldOffset(18)]
            public short BitSize;
            [FieldOffset(20)]
            public short ReportCount;
            [FieldOffset(22)]
            public ushort Reserved2a;
            [FieldOffset(24)]
            public ushort Reserved2b;
            [FieldOffset(26)]
            public ushort Reserved2c;
            [FieldOffset(28)]
            public ushort Reserved2d;
            [FieldOffset(30)]
            public ushort Reserved2e;
            [FieldOffset(32)]
            public int UnitsExp;
            [FieldOffset(36)]
            public int Units;
            [FieldOffset(40)]
            public int LogicalMin;
            [FieldOffset(44)]
            public int LogicalMax;
            [FieldOffset(48)]
            public int PhysicalMin;
            [FieldOffset(52)]
            public int PhysicalMax;
            [FieldOffset(56)]
            public HIDP_Range Range;
            [FieldOffset(56)]
            public HIDP_NotRange NotRange;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct HID_DEVICE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DEVICE_PATH)]
            public string DevicePath;
            public IntPtr HidDevice;
            public bool OpenedForRead;
            public bool OpenedForWrite;
            public bool OpenedOverlapped;
            public bool OpenedExclusive;

            public IntPtr Ppd;
            public HIDP_CAPS Caps;
            public HIDD_ATTRIBUTES Attributes;

            public IntPtr[] InputReportBuffer;
            public HID_DATA[] InputData;
            public int InputDataLength;
            public HIDP_BUTTON_CAPS[] InputButtonCaps;
            public HIDP_VALUE_CAPS[] InputValueCaps;

            public IntPtr[] OutputReportBuffer;
            public HID_DATA[] OutputData;
            public int OutputDataLength;
            public HIDP_BUTTON_CAPS[] OutputButtonCaps;
            public HIDP_VALUE_CAPS[] OutputValueCaps;

            public IntPtr[] FeatureReportBuffer;
            public HID_DATA[] FeatureData;
            public int FeatureDataLength;
            public HIDP_BUTTON_CAPS[] FeatureButtonCaps;
            public HIDP_VALUE_CAPS[] FeatureValueCaps;
        }

        #endregion

        /// <summary>
        /// Writes a request for a short vibration pulse to selected device
        /// </summary>
        /// <param name="HidDevice">Device to vibrate</param>
        public static void Write(HID_DEVICE HidDevice)
        {
            byte[] Report = new byte[16];
            uint tmp = 0;

            Report[0] = 0x03; // HID report ID (3 for bluetooth, any for USB)
            Report[1] = 0x0F; // Motor flag mask(?)
            Report[2] = 0x10; // Left trigger
            Report[3] = 0x10; // Right trigger
            Report[4] = 0x05; // Left rumble
            Report[5] = 0x05; // Right rumble
            // "Pulse"
            Report[6] = 0x0F; // On time
            Report[7] = 0x00; // Off time 
            Report[8] = 0x00; // Number of repeats

            WriteFile(HidDevice.HidDevice, Report, 16, ref tmp, IntPtr.Zero);
        }

        public static string GetProductString(HID_DEVICE HidDevice)
        {
            var chars = new byte[255];

            if (HidD_GetProductString(HidDevice.HidDevice, chars, 255))
                return Encoding.UTF8.GetString(chars);
            
            return null;
        }

        public static void CloseHidDevice(HID_DEVICE HidDevice)
        {
            CloseHandle(HidDevice.HidDevice);
        }

        static HID_DEVICE OpenHidDevice(string DevicePath)
        {
            /*++
            RoutineDescription:
            Given the HardwareDeviceInfo, representing a handle to the plug and
            play information, and deviceInfoData, representing a specific hid device,
            open that device and fill in all the relivant information in the given
            HID_DEVICE structure.
            --*/

            var HidDevice = new HID_DEVICE();

            HidDevice.DevicePath = DevicePath;

            //
            //  The hid.dll api's do not pass the overlapped structure into deviceiocontrol
            //  so to use them we must have a non overlapped device.  If the request is for
            //  an overlapped device we will close the device below and get a handle to an
            //  overlapped device
            //
            HidDevice.HidDevice = CreateFile(HidDevice.DevicePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, 0, OPEN_EXISTING, 0, IntPtr.Zero);
            HidDevice.Caps = new HIDP_CAPS();
            HidDevice.Attributes = new HIDD_ATTRIBUTES();

            //
            // If the device was not opened as overlapped, then fill in the rest of the
            //  HidDevice structure.  However, if opened as overlapped, this handle cannot
            //  be used in the calls to the HidD_ exported functions since each of these
            //  functions does synchronous I/O.
            //
            HidD_FreePreparsedData(ref HidDevice.Ppd);
            HidDevice.Ppd = IntPtr.Zero;
            HidD_GetPreparsedData(HidDevice.HidDevice, ref HidDevice.Ppd);
            HidD_GetAttributes(HidDevice.HidDevice, ref HidDevice.Attributes);
            if(HidDevice.HidDevice != IntPtr.Zero)
                HidP_GetCaps(HidDevice.Ppd, ref HidDevice.Caps);

            //MessageBox.Show(GetLastError().ToString());

            //
            // At this point the client has a choice.  It may chose to look at the
            // Usage and Page of the top level collection found in the HIDP_CAPS
            // structure.  In this way --------*it could just use the usages it knows about.
            // If either HidP_GetUsages or HidP_GetUsageValue return an error then
            // that particular usage does not exist in the report.
            // This is most likely the preferred method as the application can only
            // use usages of which it already knows.
            // In this case the app need not even call GetButtonCaps or GetValueCaps.
            //
            // In this example, however, we will call FillDeviceInfo to look for all
            //    of the usages in the device.
            //
            //FillDeviceInfo(ref HidDevice);

            return HidDevice;
        }

        public static HID_DEVICE[] FindKnownHidDevices()
        {
            int iHIDD;
            int RequiredLength;

            Guid hidGuid = new Guid();
            SP_DEVICE_INTERFACE_DATA deviceInfoData = new SP_DEVICE_INTERFACE_DATA();
            SP_DEVICE_INTERFACE_DETAIL_DATA functionClassDeviceData = new SP_DEVICE_INTERFACE_DETAIL_DATA();

            HidD_GetHidGuid(ref hidGuid);

            //
            // Open a handle to the plug and play dev node.
            //
            SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
            hardwareDeviceInfo = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            deviceInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

            iHIDD = 0;

            var HidDevices = new List<HID_DEVICE>();

            while (SetupDiEnumDeviceInterfaces(hardwareDeviceInfo, IntPtr.Zero, ref hidGuid, iHIDD, ref deviceInfoData))
            {
                RequiredLength = 0;

                //
                // allocate a function class device data structure to receive the
                // goods about this particular device.
                //
                SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, IntPtr.Zero, 0, ref RequiredLength, IntPtr.Zero);

                if (IntPtr.Size == 8)
                    functionClassDeviceData.cbSize = 8;
                else if (IntPtr.Size == 4)
                    functionClassDeviceData.cbSize = 5;

                //
                // Retrieve the information from Plug and Play.
                //
                SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, ref functionClassDeviceData, RequiredLength, ref RequiredLength, IntPtr.Zero);

                //
                // Open device with just generic query abilities to begin with
                //
                try
                {
                    HidDevices.Add(OpenHidDevice(functionClassDeviceData.DevicePath));
                }
                catch
                {
                    // Can cause a crash on Windows 7. Not that it works anyway, but ¯\_(ツ)_/¯
                }

                iHIDD++;
            }

            return HidDevices.ToArray();
        }
    }
}
