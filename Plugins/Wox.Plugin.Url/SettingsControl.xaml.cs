﻿namespace Wox.Plugin.Url
{
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Win32;

    /// <summary>
    /// SettingsControl.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private readonly Settings _settings;
        private readonly IPublicAPI _woxAPI;

        public SettingsControl(IPublicAPI woxAPI, Settings settings)
        {
            InitializeComponent();
            _settings = settings;
            _woxAPI = woxAPI;
            browserPathBox.Text = _settings.BrowserPath;
            NewWindowBrowser.IsChecked = _settings.OpenInNewBrowserWindow;
            NewTabInBrowser.IsChecked = !_settings.OpenInNewBrowserWindow;
        }

        #region Private

        private void OnChooseClick(object sender, RoutedEventArgs e)
        {
            var fileBrowserDialog = new OpenFileDialog();
            fileBrowserDialog.Filter = _woxAPI.GetTranslation("wox_plugin_url_plugin_filter");
            ;
            fileBrowserDialog.CheckFileExists = true;
            fileBrowserDialog.CheckPathExists = true;
            if (fileBrowserDialog.ShowDialog() == true)
            {
                browserPathBox.Text = fileBrowserDialog.FileName;
                _settings.BrowserPath = fileBrowserDialog.FileName;
            }
        }

        private void OnNewBrowserWindowClick(object sender, RoutedEventArgs e)
        {
            _settings.OpenInNewBrowserWindow = true;
        }

        private void OnNewTabClick(object sender, RoutedEventArgs e)
        {
            _settings.OpenInNewBrowserWindow = false;
        }

        #endregion
    }
}