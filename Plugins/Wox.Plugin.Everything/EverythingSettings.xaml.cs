﻿namespace Wox.Plugin.Everything
{
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Win32;

    public partial class EverythingSettings : UserControl
    {
        private readonly Settings _settings;

        public EverythingSettings(Settings settings)
        {
            InitializeComponent();
            _settings = settings;
        }

        #region Private

        private void View_Loaded(object sender, RoutedEventArgs re)
        {
            UseLocationAsWorkingDir.IsChecked = _settings.UseLocationAsWorkingDir;

            UseLocationAsWorkingDir.Checked += (o, e) => { _settings.UseLocationAsWorkingDir = true; };

            UseLocationAsWorkingDir.Unchecked += (o, e) => { _settings.UseLocationAsWorkingDir = false; };

            EditorPath.Content = _settings.EditorPath;
        }

        private void EditorPath_Clicked(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable File(*.exe)| *.exe";
            if (!string.IsNullOrEmpty(_settings.EditorPath))
                openFileDialog.InitialDirectory = Path.GetDirectoryName(_settings.EditorPath);

            if (openFileDialog.ShowDialog() == true) _settings.EditorPath = openFileDialog.FileName;

            EditorPath.Content = _settings.EditorPath;
        }

        #endregion
    }
}