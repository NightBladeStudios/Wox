namespace Wox.Infrastructure
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    public static class Constant
    {
        public const string Wox = "Wox";
        public const string Plugins = "Plugins";
        public const string Issue = "https://github.com/Wox-launcher/Wox/issues/new";
        public static readonly string WoxExecutable = $"{Wox}.exe";
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        public static string ExecutablePath = Path.Combine(Path.GetDirectoryName(Assembly.Location), WoxExecutable);
        public static string Version = FileVersionInfo.GetVersionInfo(ExecutablePath).ProductVersion;

        public static string ProgramDirectory = Directory.GetParent(ExecutablePath).ToString();
        public static string ApplicationDirectory = Directory.GetParent(ProgramDirectory).ToString();
        public static string RootDirectory = Directory.GetParent(ApplicationDirectory).ToString();

        public static string PreinstalledDirectory = Path.Combine(ProgramDirectory, Plugins);

        public static readonly int ThumbnailSize = 64;
        public static string ImagesDirectory = Path.Combine(ProgramDirectory, "Images");
        public static string DefaultIcon = Path.Combine(ImagesDirectory, "app.png");
        public static string ErrorIcon = Path.Combine(ImagesDirectory, "app_error.png");

        public static string PythonPath;
        public static string EverythingSDKPath;
    }
}