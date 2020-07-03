namespace Wox.ViewModel
{
    using System.Windows;
    using System.Windows.Media;
    using Core.Resource;
    using Image;
    using Plugin;

    public class PluginViewModel : BaseModel
    {
        public PluginPair PluginPair { get; set; }

        public ImageSource Image => ImageLoader.Load(PluginPair.Metadata.IcoPath);
        public Visibility ActionKeywordsVisibility => PluginPair.Metadata.ActionKeywords.Count > 1 ? Visibility.Collapsed : Visibility.Visible;
        public string InitializationTime => string.Format(_translator.GetTranslation("plugin_init_time"), PluginPair.Metadata.InitTime);
        public string QueryTime => string.Format(_translator.GetTranslation("plugin_query_time"), PluginPair.Metadata.AvgQueryTime);
        public string ActionKeywordsText => string.Join(Query.ActionKeywordSeparator, PluginPair.Metadata.ActionKeywords);

        private readonly Internationalization _translator = InternationalizationManager.Instance;
    }
}