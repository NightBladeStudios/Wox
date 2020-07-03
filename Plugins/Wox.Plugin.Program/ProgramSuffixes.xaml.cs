namespace Wox.Plugin.Program
{
    using System.Windows;

    /// <summary>
    /// ProgramSuffixes.xaml 的交互逻辑
    /// </summary>
    public partial class ProgramSuffixes
    {
        private readonly Settings _settings;
        private readonly PluginInitContext context;

        public ProgramSuffixes(PluginInitContext context, Settings settings)
        {
            this.context = context;
            InitializeComponent();
            _settings = settings;
            tbSuffixes.Text = string.Join(Settings.SuffixSeparator.ToString(), _settings.ProgramSuffixes);
        }

        #region Private

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbSuffixes.Text))
            {
                var warning = context.API.GetTranslation("wox_plugin_program_suffixes_cannot_empty");
                MessageBox.Show(warning);
                return;
            }

            _settings.ProgramSuffixes = tbSuffixes.Text.Split(Settings.SuffixSeparator);
            var msg = context.API.GetTranslation("wox_plugin_program_update_file_suffixes");
            MessageBox.Show(msg);

            DialogResult = true;
        }

        #endregion
    }
}