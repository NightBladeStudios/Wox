namespace Wox
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;
    using Infrastructure;
    using Infrastructure.Exception;
    using Infrastructure.Logger;
    using Plugin;

    internal partial class ReportWindow
    {
        public ReportWindow(Exception exception, string id)
        {
            InitializeComponent();
            ErrorTextBox.Document.Blocks.FirstBlock.Margin = new Thickness(0);
            SetException(exception, id);
        }

        #region Private

        private void SetException(Exception exception, string id)
        {
            var path = Log.CurrentLogDirectory;
            var directory = new DirectoryInfo(path);
            var log = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).First();

            Paragraph paragraph;
            var websiteKey = nameof(PluginPair.Metadata.Website);
            if (exception.Data.Contains(websiteKey))
            {
                paragraph = Hyperlink("You can help plugin author to fix this issue by opening issue in: ", exception.Data[websiteKey].ToString());
                var nameKey = nameof(PluginPair.Metadata.Name);
                if (exception.Data.Contains(nameKey)) paragraph.Inlines.Add($"Plugin Name {exception.Data[nameKey]}");
                var pluginDirectoryKey = nameof(PluginPair.Metadata.PluginDirectory);
                if (exception.Data.Contains(pluginDirectoryKey)) paragraph.Inlines.Add($"Plugin Directory {exception.Data[pluginDirectoryKey]}");
                var idKey = nameof(PluginPair.Metadata.ID);
                if (exception.Data.Contains(idKey)) paragraph.Inlines.Add($"Plugin ID {exception.Data[idKey]}");
            }
            else
            {
                paragraph = Hyperlink("You can help us to fix this issue by opening issue in: ", Constant.Issue);
            }

            paragraph.Inlines.Add($"1. upload log file: {log.FullName}\n");
            paragraph.Inlines.Add("2. copy below exception message");
            ErrorTextBox.Document.Blocks.Add(paragraph);

            var content = ExceptionFormatter.ExceptionWithRuntimeInfo(exception, id);
            paragraph = new Paragraph();
            paragraph.Inlines.Add(content);
            ErrorTextBox.Document.Blocks.Add(paragraph);
        }

        private Paragraph Hyperlink(string textBeforeUrl, string url)
        {
            var paragraph = new Paragraph();
            paragraph.Margin = new Thickness(0);

            var link = new Hyperlink {IsEnabled = true};
            link.Inlines.Add(url);
            link.NavigateUri = new Uri(url);
            link.RequestNavigate += (s, e) => Process.Start(e.Uri.ToString());
            link.Click += (s, e) => Process.Start(url);

            paragraph.Inlines.Add(textBeforeUrl);
            paragraph.Inlines.Add(link);
            paragraph.Inlines.Add("\n");

            return paragraph;
        }

        #endregion
    }
}