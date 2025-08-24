using System;
using System.Runtime.InteropServices;

namespace OpenFrp.Launcher.Win32;

public partial class UserUxtheme
{
	public const string User32 = "user32.dll";

	public const string Uxtheme = "uxtheme.dll";

	public static bool IsSupportDarkMode
	{
		get
		{
			if (Environment.OSVersion.Version.Major == 10)
			{
				return Environment.OSVersion.Version.Build >= 17763;
			}
			return false;
		}
	}

#if NET
    [LibraryImport("uxtheme.dll", EntryPoint = "#104")]
    public static partial void RefreshImmersiveColorPolicyState();

    [LibraryImport("uxtheme.dll", EntryPoint = "#132")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ShouldAppsUseDarkMode();

    [LibraryImport("uxtheme.dll", EntryPoint = "#133")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AllowDarkModeForWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.U1)] bool allow);

    [LibraryImport("uxtheme.dll", EntryPoint = "#135")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AllowDarkModeForApp([MarshalAs(UnmanagedType.U1)] bool allow);

    [LibraryImport("uxtheme.dll", EntryPoint = "#137")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool IsDarkModeAllowedForWindow(IntPtr hWnd);

    [LibraryImport("uxtheme.dll", EntryPoint = "#138")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ShouldSystemUseDarkMode();
#else
	[DllImport("uxtheme.dll", EntryPoint = "#104")]
	public static extern void RefreshImmersiveColorPolicyState();

	[DllImport("uxtheme.dll", EntryPoint = "#132")]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool ShouldAppsUseDarkMode();

	[DllImport("uxtheme.dll", EntryPoint = "#133")]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool AllowDarkModeForWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.U1)] bool allow);

	[DllImport("uxtheme.dll", EntryPoint = "#135")]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool AllowDarkModeForApp([MarshalAs(UnmanagedType.U1)] bool allow);

	[DllImport("uxtheme.dll", EntryPoint = "#137")]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool IsDarkModeAllowedForWindow(IntPtr hWnd);

	[DllImport("uxtheme.dll", EntryPoint = "#138")]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool ShouldSystemUseDarkMode();
#endif
}
