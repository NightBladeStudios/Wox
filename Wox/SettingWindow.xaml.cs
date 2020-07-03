// may be removed later https://github.com/dotnet/wpf/issues/438

namespace Wox
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Navigation;
    using Core.Plugin;
    using Core.Resource;
    using Infrastructure;
    using Infrastructure.Hotkey;
    using Infrastructure.UserSettings;
    using Microsoft.Win32;
    using NHotkey;
    using NHotkey.Wpf;
    using Ookii.Dialogs.Wpf;
    using Plugin;
    using ViewModel;

    public partial class SettingWindow
    {
        public readonly IPublicAPI _api;
        private const string StartupPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private readonly Settings _settings;
        private readonly SettingWindowViewModel _viewModel;

        public SettingWindow(IPublicAPI api, SettingWindowViewModel viewModel)
        {
            InitializeComponent();
            _settings = Settings.Instance;
            DataContext = viewModel;
            _viewModel = viewModel;
            _api = api;
        }

        #region Public

        public static void SetStartup()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupPath, true))
            {
                key?.SetValue(Constant.Wox, Constant.ExecutablePath);
            }
        }

        public static bool StartupSet()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupPath, true))
            {
                var path = key?.GetValue(Constant.Wox) as string;
                if (path != null)
                    return path == Constant.ExecutablePath;
                return false;
            }
        }

        #endregion

        #region Private

        private void OnAutoStartupChecked(object sender, RoutedEventArgs e)
        {
            SetStartup();
        }

        private void OnAutoStartupUncheck(object sender, RoutedEventArgs e)
        {
            RemoveStartup();
        }

        private void RemoveStartup()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupPath, true))
            {
                key?.DeleteValue(Constant.Wox, false);
            }
        }

        private void OnSelectPythonDirectoryClick(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            var result = dlg.ShowDialog();
            if (result == true)
            {
                var pythonDirectory = dlg.SelectedPath;
                if (!string.IsNullOrEmpty(pythonDirectory))
                {
                    var pythonPath = Path.Combine(pythonDirectory, PluginsLoader.PythonExecutable);
                    if (File.Exists(pythonPath))
                    {
                        _settings.PluginSettings.PythonDirectory = pythonDirectory;
                        MessageBox.Show("Remember to restart Wox use new Python path");
                    }
                    else
                    {
                        MessageBox.Show("Can't find python in given directory");
                    }
                }
            }
        }

        private void OnHotkeyControlLoaded(object sender, RoutedEventArgs e)
        {
            HotkeyControl.SetHotkey(_viewModel.Settings.Hotkey, false);
        }

        private void OnHotkeyChanged(object sender, EventArgs e)
        {
            if (HotkeyControl.CurrentHotkeyAvailable)
            {
                SetHotkey(HotkeyControl.CurrentHotkey, (o, args) =>
                {
                    if (!Application.Current.MainWindow.IsVisible)
                        Application.Current.MainWindow.Visibility = Visibility.Visible;
                    else
                        Application.Current.MainWindow.Visibility = Visibility.Hidden;
                });
                RemoveHotkey(_settings.Hotkey);
                _settings.Hotkey = HotkeyControl.CurrentHotkey.ToString();
            }
        }

        private void SetHotkey(HotkeyModel hotkey, EventHandler<HotkeyEventArgs> action)
        {
            var hotkeyStr = hotkey.ToString();
            try
            {
                HotkeyManager.Current.AddOrReplace(hotkeyStr, hotkey.CharKey, hotkey.ModifierKeys, action);
            }
            catch (Exception)
            {
                var errorMsg =
                    string.Format(InternationalizationManager.Instance.GetTranslation("registerHotkeyFailed"), hotkeyStr);
                MessageBox.Show(errorMsg);
            }
        }

        private void RemoveHotkey(string hotkeyStr)
        {
            if (!string.IsNullOrEmpty(hotkeyStr)) HotkeyManager.Current.Remove(hotkeyStr);
        }

        private void OnDeleteCustomHotkeyClick(object sender, RoutedEventArgs e)
        {
            var item = _viewModel.SelectedCustomPluginHotkey;
            if (item == null)
            {
                MessageBox.Show(InternationalizationManager.Instance.GetTranslation("pleaseSelectAnItem"));
                return;
            }

            var deleteWarning =
                string.Format(InternationalizationManager.Instance.GetTranslation("deleteCustomHotkeyWarning"),
                    item.Hotkey);
            if (
                MessageBox.Show(deleteWarning, InternationalizationManager.Instance.GetTranslation("delete"),
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _settings.CustomPluginHotkeys.Remove(item);
                RemoveHotkey(item.Hotkey);
            }
        }

        private void OnnEditCustomHotkeyClick(object sender, RoutedEventArgs e)
        {
            var item = _viewModel.SelectedCustomPluginHotkey;
            if (item != null)
            {
                var window = new CustomQueryHotkeySetting(this, _settings);
                window.UpdateItem(item);
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show(InternationalizationManager.Instance.GetTranslation("pleaseSelectAnItem"));
            }
        }

        private void OnAddCustomHotkeyClick(object sender, RoutedEventArgs e)
        {
            new CustomQueryHotkeySetting(this, _settings).ShowDialog();
        }

        private void OnPluginToggled(object sender, RoutedEventArgs e)
        {
            var id = _viewModel.SelectedPlugin.PluginPair.Metadata.ID;
            // used to sync the current status from the plugin manager into the setting to keep consistency after save
            _settings.PluginSettings.Plugins[id].Disabled = _viewModel.SelectedPlugin.PluginPair.Metadata.Disabled;
        }

        private void OnPluginActionKeywordsClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var id = _viewModel.SelectedPlugin.PluginPair.Metadata.ID;
                var changeKeywordsWindow = new ActionKeywords(id, _settings);
                changeKeywordsWindow.ShowDialog();
            }
        }

        private void OnPluginNameClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var website = _viewModel.SelectedPlugin.PluginPair.Metadata.Website;
                if (!string.IsNullOrEmpty(website))
                {
                    var uri = new Uri(website);
                    if (Uri.CheckSchemeName(uri.Scheme)) Process.Start(website);
                }
            }
        }

        private void OnPluginDirectoryClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var directory = _viewModel.SelectedPlugin.PluginPair.Metadata.PluginDirectory;
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Process.Start(directory);
            }
        }

        private void OnTestProxyClick(object sender, RoutedEventArgs e)
        {
            // TODO: change to command
            var msg = _viewModel.TestProxy();
            MessageBox.Show(msg); // TODO: add message box service
        }

        private void OnCheckUpdates(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateApp(); // TODO: change to command
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _viewModel.Save();
            PluginManager.Save();
        }

        private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}