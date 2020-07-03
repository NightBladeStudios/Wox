

// may be removed later https://github.com/dotnet/wpf/issues/438


namespace Wox.Plugin.Program
{
    using System.Windows;
    using Ookii.Dialogs.Wpf;

    /// <summary>
    /// Interaction logic for AddProgramSource.xaml
    /// </summary>
    public partial class AddProgramSource
    {
        private readonly PluginInitContext _context;
        private readonly ProgramSource _editing;
        private readonly Settings _settings;

        public AddProgramSource(PluginInitContext context, Settings settings)
        {
            InitializeComponent();
            _context = context;
            _settings = settings;
            Directory.Focus();
        }

        public AddProgramSource(ProgramSource edit, Settings settings)
        {
            _editing = edit;
            _settings = settings;

            InitializeComponent();
            Directory.Text = _editing.Location;
        }

        #region Private

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == true) Directory.Text = dialog.SelectedPath;
        }

        private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
        {
            var s = Directory.Text;
            if (!System.IO.Directory.Exists(s))
            {
                MessageBox.Show(_context.API.GetTranslation("wox_plugin_program_invalid_path"));
                return;
            }

            if (_editing == null)
            {
                var source = new ProgramSource
                {
                    Location = Directory.Text
                };

                _settings.ProgramSources.Insert(0, source);
            }
            else
            {
                _editing.Location = Directory.Text;
            }

            DialogResult = true;
            Close();
        }

        #endregion
    }
}