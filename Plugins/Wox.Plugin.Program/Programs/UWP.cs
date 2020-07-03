namespace Wox.Plugin.Program.Programs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Xml;
    using AppxPackaing;
    using Infrastructure;
    using Infrastructure.Logger;
    using Microsoft.Win32;
    using NLog;
    using Shell;
    using IStream = AppxPackaing.IStream;

    [Serializable]
    public class UWP
    {
        public enum PackageVersion
        {
            Windows10,
            Windows81,
            Windows8,
            Unknown
        }

        public string FullName { get; }
        public string FamilyName { get; }
        public string Name { get; set; }
        public string Location { get; set; }

        public Application[] Apps { get; set; }

        public PackageVersion Version { get; set; }

        [Serializable]
        public class Application : IProgram
        {
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string UserModelId { get; set; }
            public string BackgroundColor { get; set; }

            public string Name => DisplayName;
            public string Location => Package.Location;

            public bool Enabled { get; set; }

            public string LogoPath { get; set; }
            public UWP Package { get; set; }

            public Application(UWP package, string userModelID, string fullName, string name, string displayname, string description, string logoUri, string backgroundColor)
            {
                UserModelId = userModelID;
                Enabled = true;
                Package = package;
                DisplayName = ResourcesFromPri(fullName, name, displayname);
                Description = ResourcesFromPri(fullName, name, description);
                LogoPath = PathFromUri(fullName, name, Location, logoUri);
                BackgroundColor = backgroundColor;
            }

            #region Public

            public Result Result(string query, IPublicAPI api)
            {
                var result = new Result
                {
                    SubTitle = Package.Location,
                    Icon = Logo,
                    ContextData = this,
                    Action = e =>
                    {
                        Launch(api);
                        return true;
                    }
                };

                string title;
                if (Description.Length >= DisplayName.Length &&
                    Description.Substring(0, DisplayName.Length) == DisplayName)
                {
                    title = Description;
                    result.Title = title;
                    var match = StringMatcher.FuzzySearch(query, title);
                    result.Score = match.Score;
                    result.TitleHighlightData = match.MatchData;
                }
                else if (!string.IsNullOrEmpty(Description))
                {
                    title = $"{DisplayName}: {Description}";
                    var match1 = StringMatcher.FuzzySearch(query, DisplayName);
                    var match2 = StringMatcher.FuzzySearch(query, title);
                    if (match1.Score > match2.Score)
                    {
                        result.Score = match1.Score;
                        result.TitleHighlightData = match1.MatchData;
                    }
                    else
                    {
                        result.Score = match2.Score;
                        result.TitleHighlightData = match2.MatchData;
                    }

                    result.Title = title;
                }
                else
                {
                    title = DisplayName;
                    result.Title = title;
                    var match = StringMatcher.FuzzySearch(query, title);
                    result.Score = match.Score;
                    result.TitleHighlightData = match.MatchData;
                }


                return result;
            }

            public List<Result> ContextMenus(IPublicAPI api)
            {
                var contextMenus = new List<Result>
                {
                    new Result
                    {
                        Title = api.GetTranslation("wox_plugin_program_open_containing_folder"),

                        Action = _ =>
                        {
                            Main.StartProcess(Process.Start, new ProcessStartInfo(Package.Location));

                            return true;
                        },

                        IcoPath = "Images/folder.png"
                    }
                };
                return contextMenus;
            }

            public ImageSource Logo()
            {
                var logo = ImageFromPath(LogoPath);
                var plated = PlatedImage(logo);

                // todo magic! temp fix for cross thread object
                plated.Freeze();
                return plated;
            }

            public override string ToString()
            {
                return $"{DisplayName}: {Description}";
            }

            #endregion

            #region Internal

            internal string ResourcesFromPri(string packageFullName, string packageName, string resourceReference)
            {
                const string prefix = "ms-resource:";
                var result = "";
                Logger.WoxTrace($"package: <{packageFullName}> res ref: <{resourceReference}>");
                if (!string.IsNullOrWhiteSpace(resourceReference) && resourceReference.StartsWith(prefix))
                {
                    var key = resourceReference.Substring(prefix.Length);
                    string parsed;
                    // DisplayName
                    // Microsoft.ScreenSketch_10.1907.2471.0_x64__8wekyb3d8bbwe -> ms-resource:AppName/Text
                    // Microsoft.OneConnect_5.2002.431.0_x64__8wekyb3d8bbwe -> ms-resource:/OneConnectStrings/OneConnect/AppDisplayName
                    // ImmersiveControlPanel -> ms-resource:DisplayName
                    // Microsoft.ConnectivityStore_1.1604.4.0_x64__8wekyb3d8bbwe -> ms-resource://Microsoft.ConnectivityStore/MSWifiResources/AppDisplayName
                    if (key.StartsWith("//"))
                    {
                        parsed = $"{prefix}{key}";
                    }
                    else
                    {
                        if (!key.StartsWith("/")) key = $"/{key}";

                        if (!key.ToLower().Contains("resources") && key.Count(c => c == '/') < 3) key = $"/Resources{key}";
                        parsed = $"{prefix}//{packageName}{key}";
                    }

                    Logger.WoxTrace($"resourceReference {resourceReference} parsed <{parsed}> package <{packageFullName}>");
                    try
                    {
                        result = ResourceFromPriInternal(packageFullName, parsed);
                    }
                    catch (Exception e)
                    {
                        e.Data.Add(nameof(resourceReference), resourceReference);
                        e.Data.Add(nameof(ResourcesFromPri) + nameof(parsed), parsed);
                        e.Data.Add(nameof(ResourcesFromPri) + nameof(packageFullName), packageFullName);
                        throw e;
                    }
                }
                else
                {
                    result = resourceReference;
                }

                Logger.WoxTrace($"package: <{packageFullName}> pri resource result: <{result}>");
                return result;
            }

            internal string LogoUriFromManifest(IAppxManifestApplication app)
            {
                var logoKeyFromVersion = new Dictionary<PackageVersion, string>
                {
                    {PackageVersion.Windows10, "Square44x44Logo"},
                    {PackageVersion.Windows81, "Square30x30Logo"},
                    {PackageVersion.Windows8, "SmallLogo"}
                };
                if (logoKeyFromVersion.ContainsKey(Package.Version))
                {
                    var key = logoKeyFromVersion[Package.Version];
                    var logoUri = app.GetStringValue(key);
                    return logoUri;
                }

                return string.Empty;
            }

            #endregion

            #region Private

            private async void Launch(IPublicAPI api)
            {
                var appManager = new ApplicationActivationManager();
                uint unusedPid;
                const string noArgs = "";
                const ACTIVATEOPTIONS noFlags = ACTIVATEOPTIONS.AO_NONE;
                await Task.Run(() =>
                {
                    try
                    {
                        appManager.ActivateApplication(UserModelId, noArgs, noFlags, out unusedPid);
                    }
                    catch (Exception)
                    {
                        var name = "Plugin: Program";
                        var message = $"Can't start UWP: {DisplayName}";
                        api.ShowMsg(name, message, string.Empty);
                    }
                });
            }

            private string PathFromUri(string packageFullName, string packageName, string packageLocation, string fileReference)
            {
                // all https://msdn.microsoft.com/windows/uwp/controls-and-patterns/tiles-and-notifications-app-assets
                // windows 10 https://msdn.microsoft.com/en-us/library/windows/apps/dn934817.aspx
                // windows 8.1 https://msdn.microsoft.com/en-us/library/windows/apps/hh965372.aspx#target_size
                // windows 8 https://msdn.microsoft.com/en-us/library/windows/apps/br211475.aspx

                Logger.WoxTrace($"package: <{packageFullName}> file ref: <{fileReference}>");
                var path = Path.Combine(packageLocation, fileReference);
                if (File.Exists(path))
                    // for 28671Petrroll.PowerPlanSwitcher_0.4.4.0_x86__ge82akyxbc7z4
                    return path;

                // https://docs.microsoft.com/en-us/windows/uwp/app-resources/pri-apis-scenario-1
                var parsed = $"ms-resource:///Files/{fileReference.Replace("\\", "/")}";
                try
                {
                    var result = ResourceFromPriInternal(packageFullName, parsed);
                    Logger.WoxTrace($"package: <{packageFullName}> pri file result: <{result}>");
                    return result;
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(fileReference), fileReference);
                    e.Data.Add(nameof(PathFromUri) + nameof(parsed), parsed);
                    e.Data.Add(nameof(PathFromUri) + nameof(packageFullName), packageFullName);
                    throw e;
                }
            }

            /// https://docs.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-shloadindirectstring
            /// use makepri to check whether the resource can be get, the error message is usually useless
            /// makepri.exe dump /if "a\resources.pri" /of b.xml
            private string ResourceFromPriInternal(string packageFullName, string parsed)
            {
                Logger.WoxTrace($"package: <{packageFullName}> pri parsed: <{parsed}>");
                // following error probally due to buffer to small
                // '200' violates enumeration constraint of '100 120 140 160 180'.
                // 'Microsoft Corporation' violates pattern constraint of '\bms-resource:.{1,256}'.
                var outBuffer = new StringBuilder(512);
                var source = $"@{{{packageFullName}? {parsed}}}";
                var capacity = (uint) outBuffer.Capacity;
                var hResult = SHLoadIndirectString(source, outBuffer, capacity, IntPtr.Zero);
                if (hResult == HResult.Ok)
                {
                    var loaded = outBuffer.ToString();
                    if (!string.IsNullOrEmpty(loaded)) return loaded;

                    Logger.WoxError($"Can't load null or empty result pri {source} in uwp location {Package.Location}");
                    return string.Empty;
                }

                var e = Marshal.GetExceptionForHR((int) hResult);
                e.Data.Add(nameof(source), source);
                e.Data.Add(nameof(packageFullName), packageFullName);
                e.Data.Add(nameof(parsed), parsed);
                Logger.WoxError($"Load pri failed {source} location {Package.Location}", e);
                return string.Empty;
            }


            private BitmapImage ImageFromPath(string path)
            {
                if (File.Exists(path))
                {
                    var image = new BitmapImage(new Uri(path));
                    return image;
                }

                Logger.WoxError($"|Unable to get logo for {UserModelId} from {path} and located in {Package.Location}");
                return new BitmapImage(new Uri(Constant.ErrorIcon));
            }

            private ImageSource PlatedImage(BitmapImage image)
            {
                if (!string.IsNullOrEmpty(BackgroundColor) && BackgroundColor != "transparent")
                {
                    var width = image.Width;
                    var height = image.Height;
                    var x = 0;
                    var y = 0;

                    var group = new DrawingGroup();

                    var converted = ColorConverter.ConvertFromString(BackgroundColor);
                    if (converted != null)
                    {
                        var color = (Color) converted;
                        var brush = new SolidColorBrush(color);
                        var pen = new Pen(brush, 1);
                        var backgroundArea = new Rect(0, 0, width, width);
                        var rectangle = new RectangleGeometry(backgroundArea);
                        var rectDrawing = new GeometryDrawing(brush, pen, rectangle);
                        group.Children.Add(rectDrawing);

                        var imageArea = new Rect(x, y, image.Width, image.Height);
                        var imageDrawing = new ImageDrawing(image, imageArea);
                        group.Children.Add(imageDrawing);

                        // http://stackoverflow.com/questions/6676072/get-system-drawing-bitmap-of-a-wpf-area-using-visualbrush
                        var visual = new DrawingVisual();
                        var context = visual.RenderOpen();
                        context.DrawDrawing(group);
                        context.Close();
                        const int dpiScale100 = 96;
                        var bitmap = new RenderTargetBitmap(
                            Convert.ToInt32(width), Convert.ToInt32(height),
                            dpiScale100, dpiScale100,
                            PixelFormats.Pbgra32
                        );
                        bitmap.Render(visual);
                        return bitmap;
                    }

                    Logger.WoxError($"Unable to convert background string {BackgroundColor} to color for {Package.Location}");
                    return new BitmapImage(new Uri(Constant.ErrorIcon));
                }

                // todo use windows theme as background
                return image;
            }

            #endregion
        }

        private enum HResult : uint
        {
            Ok = 0x0000
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Flags]
        private enum Stream : uint
        {
            Read = 0x0,
            ShareExclusive = 0x10,
            ShareDenyNone = 0x40
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern HResult SHCreateStreamOnFileEx(string fileName, Stream grfMode, uint attributes, bool create,
            IStream reserved, out IStream stream);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern HResult SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, uint cchOutBuf,
            IntPtr ppvReserved);

        public UWP(string id, string location)
        {
            FullName = id;
            var parts = id.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries);
            FamilyName = $"{parts[0]}_{parts[parts.Length - 1]}";
            Location = location;
        }

        #region Public

        public static Application[] All()
        {
            var bag = new ConcurrentBag<Application>();
            Parallel.ForEach(PackageFoldersFromRegistry(), (package, state) =>
                {
                    try
                    {
                        package.InitializeAppInfo();
                        foreach (var a in package.Apps) bag.Add(a);
                    }
                    catch (Exception e)
                    {
                        e.Data.Add(nameof(package.FullName), package.FullName);
                        e.Data.Add(nameof(package.Location), package.Location);
                        Logger.WoxError($"Cannot parse UWP {package.Location}", e);
                    }
                }
            );
            return bag.ToArray();
        }

        public static List<UWP> PackageFoldersFromRegistry()
        {
            var activable = new HashSet<string>();
            var activableReg = @"Software\Classes\ActivatableClasses\Package";
            var activableRegSubKey = Registry.CurrentUser.OpenSubKey(activableReg);
            foreach (var name in activableRegSubKey.GetSubKeyNames()) activable.Add(name);

            var packages = new List<UWP>();
            var packageReg = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";
            var packageRegSubKey = Registry.CurrentUser.OpenSubKey(packageReg);
            foreach (var name in packageRegSubKey.GetSubKeyNames())
            {
                var packageKey = packageRegSubKey.OpenSubKey(name);
                var framework = packageKey.GetValue("Framework");
                if (framework != null)
                    if ((int) framework == 1)
                        continue;
                var valueFolder = packageKey.GetValue("PackageRootFolder");
                var valueID = packageKey.GetValue("PackageID");
                if (valueID != null && valueFolder != null && activable.Contains(valueID))
                {
                    var location = (string) valueFolder;
                    var id = (string) valueID;
                    var uwp = new UWP(id, location);
                    packages.Add(uwp);
                }
            }

            // only exception windows.immersivecontrolpanel_10.0.2.1000_neutral_neutral_cw5n1h2txyewy
            var settingsID = activable.First(a => a.StartsWith("windows.immersivecontrolpanel"));
            var settingsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "ImmersiveControlPanel");
            var settings = new UWP(settingsID, settingsLocation);
            packages.Add(settings);

            return packages;
        }

        public override string ToString()
        {
            return FullName;
        }

        public override bool Equals(object obj)
        {
            if (obj is UWP uwp)
                return FullName.Equals(uwp.FullName);
            return false;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        #endregion

        #region Private

        /// <exception cref="ArgumentException"
        private void InitializeAppInfo()
        {
            var path = Path.Combine(Location, "AppxManifest.xml");
            using (var reader = XmlReader.Create(path))
            {
                var success = reader.ReadToFollowing("Package");
                if (!success) throw new ArgumentException($"Cannot read Package key from {path}");

                Version = PackageVersion.Unknown;
                for (var i = 0; i < reader.AttributeCount; i++)
                {
                    var schema = reader.GetAttribute(i);
                    if (schema != null)
                    {
                        if (schema == "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
                            Version = PackageVersion.Windows10;
                        else if (schema == "http://schemas.microsoft.com/appx/2013/manifest")
                            Version = PackageVersion.Windows81;
                        else if (schema == "http://schemas.microsoft.com/appx/2010/manifest")
                            Version = PackageVersion.Windows8;
                        else
                            continue;
                    }
                }

                if (Version == PackageVersion.Unknown) throw new ArgumentException($"Unknowen schema version {path}");

                success = reader.ReadToFollowing("Identity");
                if (!success) throw new ArgumentException($"Cannot read Identity key from {path}");
                if (success) Name = reader.GetAttribute("Name");

                success = reader.ReadToFollowing("Applications");
                if (!success) throw new ArgumentException($"Cannot read Applications key from {path}");
                success = reader.ReadToDescendant("Application");
                if (!success) throw new ArgumentException($"Cannot read Applications key from {path}");
                var apps = new List<Application>();
                do
                {
                    var id = reader.GetAttribute("Id");

                    reader.ReadToFollowing("uap:VisualElements");
                    var displayName = reader.GetAttribute("DisplayName");
                    var description = reader.GetAttribute("Description");
                    var backgroundColor = reader.GetAttribute("BackgroundColor");
                    var appListEntry = reader.GetAttribute("AppListEntry");

                    if (appListEntry == "none") continue;

                    var logoUri = string.Empty;
                    if (Version == PackageVersion.Windows10)
                        logoUri = reader.GetAttribute("Square44x44Logo");
                    else if (Version == PackageVersion.Windows81)
                        logoUri = reader.GetAttribute("Square30x30Logo");
                    else if (Version == PackageVersion.Windows8)
                        logoUri = reader.GetAttribute("SmallLogo");
                    else
                        throw new ArgumentException($"Unknowen schema version {path}");

                    if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(id)) continue;

                    var userModelId = $"{FamilyName}!{id}";
                    var app = new Application(this, userModelId, FullName, Name, displayName, description, logoUri, backgroundColor);

                    apps.Add(app);
                } while (reader.ReadToFollowing("Application"));

                Apps = apps.ToArray();
            }
        }

        #endregion
    }
}