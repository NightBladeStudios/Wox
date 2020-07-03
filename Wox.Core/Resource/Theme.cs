namespace Wox.Core.Resource
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Interop;
    using System.Windows.Markup;
    using System.Windows.Media;
    using Windows.UI.ViewManagement;
    using Infrastructure;
    using Infrastructure.Logger;
    using Infrastructure.UserSettings;
    using NLog;

    public class Theme
    {
        public Settings Settings { get; set; }
        public HighlightStyle HighLightSelectedStyle = new HighlightStyle();

        public HighlightStyle HighLightStyle = new HighlightStyle();

        /*
        Found on https://github.com/riverar/sample-win10-aeroglass
        */
        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public readonly int AccentFlags;
            public readonly int GradientColor;
            public readonly int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private const string Folder = "Themes";
        private const string Extension = ".xaml";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private string DirectoryPath => Path.Combine(Constant.ProgramDirectory, Folder);
        private string UserDirectoryPath => Path.Combine(DataLocation.DataDirectory(), Folder);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private readonly List<string> _themeDirectories = new List<string>();
        private ResourceDictionary _oldResource;
        private string _oldTheme;

        private object UISettings;

        public Theme()
        {
            Settings = Settings.Instance;
            _themeDirectories.Add(DirectoryPath);
            _themeDirectories.Add(UserDirectoryPath);
            MakeSureThemeDirectoriesExist();

            var dicts = Application.Current.Resources.MergedDictionaries;
            _oldResource = dicts.First(d =>
            {
                var p = d.Source.AbsolutePath;
                var dir = Path.GetDirectoryName(p).NonNull();
                var info = new DirectoryInfo(dir);
                var f = info.Name;
                var e = Path.GetExtension(p);
                var found = f == Folder && e == Extension;
                return found;
            });
            _oldTheme = Path.GetFileNameWithoutExtension(_oldResource.Source.AbsolutePath);

            // https://github.com/Wox-launcher/Wox/issues/2935
            var support = Environment.OSVersion.Version.Major >= new Version(10, 0).Major;
            Logger.WoxInfo($"Runtime Version {Environment.OSVersion.Version} {support}");
            if (support) AutoReload();
        }

        #region Public

        public bool ChangeTheme(string theme)
        {
            const string defaultTheme = "Dark";

            var path = GetThemePath(theme);
            try
            {
                if (string.IsNullOrEmpty(path))
                    throw new DirectoryNotFoundException("Theme path can't be found <{path}>");

                Settings.Theme = theme;

                var dicts = Application.Current.Resources.MergedDictionaries;

                dicts.Remove(_oldResource);
                var newResource = GetResourceDictionary();
                dicts.Add(newResource);
                _oldResource = newResource;
                _oldTheme = Path.GetFileNameWithoutExtension(_oldResource.Source.AbsolutePath);
                HighLightStyle = new HighlightStyle(false);
                HighLightSelectedStyle = new HighlightStyle(true);

                SetBlurForWindow();
            }
            catch (DirectoryNotFoundException)
            {
                Logger.WoxError($"Theme <{theme}> path can't be found");
                if (theme != defaultTheme)
                {
                    MessageBox.Show(string.Format(InternationalizationManager.Instance.GetTranslation("theme_load_failure_path_not_exists"), theme));
                    ChangeTheme(defaultTheme);
                }

                return false;
            }
            catch (XamlParseException)
            {
                Logger.WoxError($"Theme <{theme}> fail to parse");
                if (theme != defaultTheme)
                {
                    MessageBox.Show(string.Format(InternationalizationManager.Instance.GetTranslation("theme_load_failure_parse_error"), theme));
                    ChangeTheme(defaultTheme);
                }

                return false;
            }

            return true;
        }

        public ResourceDictionary GetResourceDictionary()
        {
            var uri = GetThemePath(Settings.Theme);
            var dict = new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Absolute)
            };

            var queryBoxStyle = dict["QueryBoxStyle"] as Style;
            if (queryBoxStyle != null)
            {
                queryBoxStyle.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily(Settings.QueryBoxFont)));
                queryBoxStyle.Setters.Add(new Setter(Control.FontStyleProperty, FontHelper.GetFontStyleFromInvariantStringOrNormal(Settings.QueryBoxFontStyle)));
                queryBoxStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontHelper.GetFontWeightFromInvariantStringOrNormal(Settings.QueryBoxFontWeight)));
                queryBoxStyle.Setters.Add(new Setter(Control.FontStretchProperty, FontHelper.GetFontStretchFromInvariantStringOrNormal(Settings.QueryBoxFontStretch)));

                var caretBrushPropertyValue = queryBoxStyle.Setters.OfType<Setter>().Any(x => x.Property == TextBoxBase.CaretBrushProperty);
                var foregroundPropertyValue = queryBoxStyle.Setters.OfType<Setter>().FirstOrDefault(x => x.Property == Control.ForegroundProperty)?.Value;
                if (!caretBrushPropertyValue && foregroundPropertyValue != null)
                    queryBoxStyle.Setters.Add(new Setter(TextBoxBase.CaretBrushProperty, foregroundPropertyValue));
            }

            var queryTextSuggestionBoxStyle = new Style(typeof(TextBox), queryBoxStyle);
            var hasSuggestion = false;
            if (dict.Contains("QueryTextSuggestionBoxStyle"))
            {
                queryTextSuggestionBoxStyle = dict["QueryTextSuggestionBoxStyle"] as Style;
                hasSuggestion = true;
            }

            dict["QueryTextSuggestionBoxStyle"] = queryTextSuggestionBoxStyle;
            if (queryTextSuggestionBoxStyle != null)
            {
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily(Settings.QueryBoxFont)));
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(Control.FontStyleProperty, FontHelper.GetFontStyleFromInvariantStringOrNormal(Settings.QueryBoxFontStyle)));
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontHelper.GetFontWeightFromInvariantStringOrNormal(Settings.QueryBoxFontWeight)));
                queryTextSuggestionBoxStyle.Setters.Add(new Setter(Control.FontStretchProperty, FontHelper.GetFontStretchFromInvariantStringOrNormal(Settings.QueryBoxFontStretch)));
            }

            var queryBoxStyleSetters = queryBoxStyle.Setters.OfType<Setter>().ToList();
            var queryTextSuggestionBoxStyleSetters = queryTextSuggestionBoxStyle.Setters.OfType<Setter>().ToList();
            foreach (var setter in queryBoxStyleSetters)
            {
                if (setter.Property == Control.BackgroundProperty)
                    continue;
                if (setter.Property == Control.ForegroundProperty)
                    continue;
                if (queryTextSuggestionBoxStyleSetters.All(x => x.Property != setter.Property))
                    queryTextSuggestionBoxStyle.Setters.Add(setter);
            }

            if (!hasSuggestion)
            {
                var backgroundBrush = queryBoxStyle.Setters.OfType<Setter>().FirstOrDefault(x => x.Property == Control.BackgroundProperty)?.Value ??
                                      (dict["BaseQuerySuggestionBoxStyle"] as Style).Setters.OfType<Setter>().FirstOrDefault(x => x.Property == Control.BackgroundProperty).Value;
                queryBoxStyle.Setters.OfType<Setter>().FirstOrDefault(x => x.Property == Control.BackgroundProperty).Value = Brushes.Transparent;
                if (queryTextSuggestionBoxStyle.Setters.OfType<Setter>().Any(x => x.Property == Control.BackgroundProperty))
                    queryTextSuggestionBoxStyle.Setters.OfType<Setter>().First(x => x.Property == Control.BackgroundProperty).Value = backgroundBrush;
                else
                    queryTextSuggestionBoxStyle.Setters.Add(new Setter(Control.BackgroundProperty, backgroundBrush));
            }

            var resultItemStyle = dict["ItemTitleStyle"] as Style;
            var resultSubItemStyle = dict["ItemSubTitleStyle"] as Style;
            var resultItemSelectedStyle = dict["ItemTitleSelectedStyle"] as Style;
            var resultSubItemSelectedStyle = dict["ItemSubTitleSelectedStyle"] as Style;
            if (resultItemStyle != null && resultSubItemStyle != null && resultSubItemSelectedStyle != null && resultItemSelectedStyle != null)
            {
                var fontFamily = new Setter(TextBlock.FontFamilyProperty, new FontFamily(Settings.ResultFont));
                var fontStyle = new Setter(TextBlock.FontStyleProperty, FontHelper.GetFontStyleFromInvariantStringOrNormal(Settings.ResultFontStyle));
                var fontWeight = new Setter(TextBlock.FontWeightProperty, FontHelper.GetFontWeightFromInvariantStringOrNormal(Settings.ResultFontWeight));
                var fontStretch = new Setter(TextBlock.FontStretchProperty, FontHelper.GetFontStretchFromInvariantStringOrNormal(Settings.ResultFontStretch));

                Setter[] setters = {fontFamily, fontStyle, fontWeight, fontStretch};
                Array.ForEach(new[] {resultItemStyle, resultSubItemStyle, resultItemSelectedStyle, resultSubItemSelectedStyle}, o => Array.ForEach(setters, p => o.Setters.Add(p)));
            }

            return dict;
        }

        public List<string> LoadAvailableThemes()
        {
            var themes = new List<string>();
            foreach (var themeDirectory in _themeDirectories)
                themes.AddRange(
                    Directory.GetFiles(themeDirectory)
                        .Where(filePath => filePath.EndsWith(Extension) && !filePath.EndsWith("Base.xaml"))
                        .ToList());
            return themes.OrderBy(o => o).ToList();
        }

        /// <summary>
        /// Sets the blur for a window via SetWindowCompositionAttribute
        /// </summary>
        public void SetBlurForWindow()
        {
            // Exception of FindResource can't be cathed if global exception handle is set
            if (Environment.OSVersion.Version >= new Version(6, 2))
            {
                var resource = Application.Current.TryFindResource("ThemeBlurEnabled");
                var blur = false;
                if (resource is bool b)
                    blur = b;

                var accent = blur ? AccentState.ACCENT_ENABLE_BLURBEHIND : AccentState.ACCENT_DISABLED;
                SetWindowAccent(Application.Current.MainWindow, accent);
            }
        }

        #endregion

        #region Private

        private void MakeSureThemeDirectoriesExist()
        {
            foreach (var dir in _themeDirectories)
                if (!Directory.Exists(dir))
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (Exception e)
                    {
                        Logger.WoxError($"Exception when create directory <{dir}>", e);
                    }
        }

        private string GetThemePath(string themeName)
        {
            foreach (var themeDirectory in _themeDirectories)
            {
                var path = Path.Combine(themeDirectory, themeName + Extension);
                if (File.Exists(path)) return path;
            }

            return string.Empty;
        }

        private void AutoReload()
        {
            var uiSettings = new UISettings();
            uiSettings.ColorValuesChanged +=
                (sender, args) =>
                {
                    Application.Current.Dispatcher.Invoke(
                        () => { ChangeTheme(Settings.Theme); });
                };
            UISettings = uiSettings;
        }

        private void SetWindowAccent(Window w, AccentState state)
        {
            var windowHelper = new WindowInteropHelper(w);
            var accent = new AccentPolicy {AccentState = state};
            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        #endregion
    }

    public class HighlightStyle
    {
        public Brush Color { get; set; }
        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }
        public FontStretch FontStretch { get; set; }

        public HighlightStyle()
        {
            Color = Brushes.Black;
            FontStyle = FontStyles.Normal;
            FontWeight = FontWeights.Normal;
            FontStretch = FontStretches.Normal;
        }

        public HighlightStyle(bool selected)
        {
            var resources = ThemeManager.Instance.GetResourceDictionary();

            Color = (Brush) (selected ? resources.Contains("ItemSelectedHighlightColor") ? resources["ItemSelectedHighlightColor"] : resources["BaseItemSelectedHighlightColor"] :
                resources.Contains("ItemHighlightColor") ? resources["ItemHighlightColor"] : resources["BaseItemHighlightColor"]);
            FontStyle = FontHelper.GetFontStyleFromInvariantStringOrNormal(Settings.Instance.ResultHighlightFontStyle);
            FontWeight = FontHelper.GetFontWeightFromInvariantStringOrNormal(Settings.Instance.ResultHighlightFontWeight);
            FontStretch = FontHelper.GetFontStretchFromInvariantStringOrNormal(Settings.Instance.ResultHighlightFontStretch);
        }
    }
}