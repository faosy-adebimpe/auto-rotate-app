using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MonitorRotator;

public static class DisplayManager
{
    private const int ENUM_CURRENT_SETTINGS = -1;

    private const int DMDO_DEFAULT = 0;
    private const int DMDO_90 = 1;
    private const int DMDO_180 = 2;
    private const int DMDO_270 = 3;

    private const int DM_POSITION = 0x00000020;
    private const int DM_PELSWIDTH = 0x00080000;
    private const int DM_PELSHEIGHT = 0x00100000;
    private const int DM_DISPLAYORIENTATION = 0x00000080;

    private const int CDS_UPDATEREGISTRY = 0x00000001;
    private const int CDS_TEST = 0x00000002;

    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DISP_CHANGE_RESTART = 1;

    private const int DISPLAY_DEVICE_ACTIVE = 0x00000001;
    private const int DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(
        string? lpDevice,
        uint iDevNum,
        ref DISPLAY_DEVICE lpDisplayDevice,
        uint dwFlags);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(
        string? lpszDeviceName,
        int iModeNum,
        ref DEVMODE lpDevMode);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(
        string? lpszDeviceName,
        ref DEVMODE lpDevMode,
        IntPtr hwnd,
        uint dwflags,
        IntPtr lParam);

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;

        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;

        public int dmPositionX;
        public int dmPositionY;

        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;

        public short dmLogPixels;

        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;

        public int dmDisplayFlags;
        public int dmDisplayFrequency;

        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;

        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    /// <summary>
    /// Finds the first active non-primary display and toggles
    /// between landscape and portrait orientation.
    /// </summary>
    public static void RotateExternalMonitor()
    {
        string? primaryDisplayName = GetPrimaryDisplayName();

        DISPLAY_DEVICE device = new()
        {
            cb = Marshal.SizeOf<DISPLAY_DEVICE>()
        };

        for (uint i = 0;
             EnumDisplayDevices(null, i, ref device, 0);
             i++)
        {
            bool isActive =
                (device.StateFlags & DISPLAY_DEVICE_ACTIVE) != 0;

            if (!isActive)
            {
                device = new DISPLAY_DEVICE
                {
                    cb = Marshal.SizeOf<DISPLAY_DEVICE>()
                };

                continue;
            }

            // Never rotate the primary/laptop display.
            if (string.Equals(
                    device.DeviceName,
                    primaryDisplayName,
                    StringComparison.OrdinalIgnoreCase))
            {
                device = new DISPLAY_DEVICE
                {
                    cb = Marshal.SizeOf<DISPLAY_DEVICE>()
                };

                continue;
            }

            DEVMODE mode = new()
            {
                dmSize = (short)Marshal.SizeOf<DEVMODE>()
            };

            bool settingsFound = EnumDisplaySettings(
                device.DeviceName,
                ENUM_CURRENT_SETTINGS,
                ref mode);

            if (!settingsFound)
            {
                device = new DISPLAY_DEVICE
                {
                    cb = Marshal.SizeOf<DISPLAY_DEVICE>()
                };

                continue;
            }

            int currentOrientation = mode.dmDisplayOrientation;

            int newOrientation = currentOrientation switch
            {
                DMDO_DEFAULT => DMDO_90,
                DMDO_90 => DMDO_DEFAULT,
                DMDO_180 => DMDO_270,
                DMDO_270 => DMDO_180,
                _ => DMDO_DEFAULT
            };

            bool currentlyPortrait =
                currentOrientation == DMDO_90 ||
                currentOrientation == DMDO_270;

            bool willBePortrait =
                newOrientation == DMDO_90 ||
                newOrientation == DMDO_270;

            // Width and height need to be exchanged when moving
            // between landscape and portrait.
            if (currentlyPortrait != willBePortrait)
            {
                int originalWidth = mode.dmPelsWidth;

                mode.dmPelsWidth = mode.dmPelsHeight;
                mode.dmPelsHeight = originalWidth;
            }

            mode.dmDisplayOrientation = newOrientation;

            // Explicitly tell Windows which DEVMODE fields changed.
            mode.dmFields |=
                DM_DISPLAYORIENTATION |
                DM_PELSWIDTH |
                DM_PELSHEIGHT |
                DM_POSITION;

            // First test whether Windows accepts the new configuration.
            int testResult = ChangeDisplaySettingsEx(
                device.DeviceName,
                ref mode,
                IntPtr.Zero,
                CDS_TEST,
                IntPtr.Zero);

            if (testResult != DISP_CHANGE_SUCCESSFUL)
            {
                throw new InvalidOperationException(
                    $"Windows rejected the display rotation. " +
                    $"Error code: {testResult}");
            }

            // Apply the configuration and save it.
            int result = ChangeDisplaySettingsEx(
                device.DeviceName,
                ref mode,
                IntPtr.Zero,
                CDS_UPDATEREGISTRY,
                IntPtr.Zero);

            if (result == DISP_CHANGE_SUCCESSFUL)
            {
                return;
            }

            if (result == DISP_CHANGE_RESTART)
            {
                throw new InvalidOperationException(
                    "Windows requires a restart to apply this display rotation.");
            }

            throw new InvalidOperationException(
                $"Could not rotate the monitor. " +
                $"Windows error code: {result}");
        }

        throw new InvalidOperationException(
            "No active external monitor was found.");
    }

    /// <summary>
    /// Gets the Windows device name of the primary display.
    /// </summary>
    private static string? GetPrimaryDisplayName()
    {
        foreach (Screen screen in Screen.AllScreens)
        {
            if (screen.Primary)
            {
                return screen.DeviceName;
            }
        }

        return null;
    }
}