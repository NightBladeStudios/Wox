namespace Wox.Helper
{
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Media;
    using Microsoft.Win32;

    public static class WallpaperPathRetrieval
    {
        private static readonly uint SPI_GETDESKWALLPAPER = 0x73;
        private static readonly int MAX_PATH = 260;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SystemParametersInfo(uint action,
            int uParam, StringBuilder vParam, uint winIni);

        #region Public

        public static string GetWallpaperPath()
        {
            var wallpaper = new StringBuilder(MAX_PATH);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaper, 0);

            var str = wallpaper.ToString();
            if (string.IsNullOrEmpty(str))
                return null;

            return str;
        }

        public static Color GetWallpaperColor()
        {
            var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Colors", true);
            var result = key.GetValue(@"Background", null);
            if (result != null && result is string)
                try
                {
                    var parts = result.ToString().Trim().Split(new[] {' '}, 3).Select(byte.Parse).ToList();
                    return Color.FromRgb(parts[0], parts[1], parts[2]);
                }
                catch
                {
                }

            return Colors.Transparent;
        }

        #endregion
    }
}