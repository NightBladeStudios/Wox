namespace Wox
{
    using System.Windows;
    using Core.Plugin;
    using Core.Resource;
    using Infrastructure.UserSettings;
    using Plugin;

    public partial class ActionKeywords : Window
    {
        private readonly Internationalization _translator = InternationalizationManager.Instance;
        private readonly PluginPair _plugin;
        private Settings _settings;

        public ActionKeywords(string pluginId, Settings settings)
        {
            InitializeComponent();
            _plugin = PluginManager.GetPluginForId(pluginId);
            _settings = settings;
            if (_plugin == null)
            {
                MessageBox.Show(_translator.GetTranslation("cannotFindSpecifiedPlugin"));
                Close();
            }
        }

        #region Private

        private void ActionKeyword_OnLoaded(object sender, RoutedEventArgs e)
        {
            tbOldActionKeyword.Text = string.Join(Query.ActionKeywordSeparator, _plugin.Metadata.ActionKeywords.ToArray());
            tbAction.Focus();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnDone_OnClick(object sender, RoutedEventArgs _)
        {
            var oldActionKeyword = _plugin.Metadata.ActionKeywords[0];
            var newActionKeyword = tbAction.Text.Trim();
            newActionKeyword = newActionKeyword.Length > 0 ? newActionKeyword : "*";
            if (!PluginManager.ActionKeywordRegistered(newActionKeyword))
            {
                var id = _plugin.Metadata.ID;
                PluginManager.ReplaceActionKeyword(id, oldActionKeyword, newActionKeyword);
                MessageBox.Show(_translator.GetTranslation("success"));
                Close();
            }
            else
            {
                var msg = _translator.GetTranslation("newActionKeywordsHasBeenAssigned");
                MessageBox.Show(msg);
            }
        }

        #endregion
    }
}