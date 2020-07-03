namespace Wox.Image
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Infrastructure.Logger;
    using NLog;

    public static class EmbeddedIcon
    {
        private delegate bool EnumResNameDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

        private const uint GROUP_ICON = 14;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EnumResourceNamesWithID(IntPtr hModule, uint lpszType, EnumResNameDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        #region Public

        public static ImageSource GetImage(string key, string path, int iconSize)
        {
            // https://github.com/CoenraadS/Windows-Control-Panel-Items/
            // https://gist.github.com/jnm2/79ed8330ceb30dea44793e3aa6c03f5b

            var iconStringRaw = path.Substring(key.Length);
            var iconString = new List<string>(iconStringRaw.Split(new[] {','}, 2));
            var iconPtr = IntPtr.Zero;
            IntPtr dataFilePointer;
            IntPtr iconIndex;
            uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

            Logger.WoxTrace($"{nameof(iconStringRaw)}: {iconStringRaw}");

            if (string.IsNullOrEmpty(iconString[0]))
            {
                var e = new ArgumentException($"iconString empty {path}");
                e.Data.Add(nameof(path), path);
                throw e;
            }

            if (iconString[0][0] == '@') iconString[0] = iconString[0].Substring(1);

            dataFilePointer = LoadLibraryEx(iconString[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (iconString.Count == 2)
            {
                // C:\WINDOWS\system32\mblctr.exe,0
                // %SystemRoot%\System32\FirewallControlPanel.dll,-1
                var index = Math.Abs(int.Parse(iconString[1]));
                iconIndex = (IntPtr) index;
                iconPtr = LoadImage(dataFilePointer, iconIndex, 1, iconSize, iconSize, 0);
            }

            if (iconPtr == IntPtr.Zero)
            {
                var defaultIconPtr = IntPtr.Zero;
                var callback = new EnumResNameDelegate((hModule, lpszType, lpszName, lParam) =>
                {
                    defaultIconPtr = lpszName;
                    return false;
                });
                var result = EnumResourceNamesWithID(dataFilePointer, GROUP_ICON, callback, IntPtr.Zero); //Iterate through resources. 
                if (!result)
                {
                    var error = Marshal.GetLastWin32Error();
                    var userStoppedResourceEnumeration = 0x3B02;
                    if (error != userStoppedResourceEnumeration)
                    {
                        var exception = new Win32Exception(error);
                        exception.Data.Add(nameof(path), path);
                        throw exception;
                    }
                }

                iconPtr = LoadImage(dataFilePointer, defaultIconPtr, 1, iconSize, iconSize, 0);
            }

            FreeLibrary(dataFilePointer);
            BitmapSource image;
            if (iconPtr != IntPtr.Zero)
            {
                image = Imaging.CreateBitmapSourceFromHIcon(iconPtr, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                image.CloneCurrentValue(); //Remove pointer dependency.
                image.Freeze();
                DestroyIcon(iconPtr);
                return image;
            }

            {
                var e = new ArgumentException($"iconPtr zero {path}");
                e.Data.Add(nameof(path), path);
                throw e;
            }
        }

        #endregion
    }
}