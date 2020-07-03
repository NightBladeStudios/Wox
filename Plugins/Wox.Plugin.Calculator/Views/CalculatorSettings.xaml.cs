namespace Wox.Plugin.Calculator.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using ViewModels;

    /// <summary>
    /// Interaction logic for CalculatorSettings.xaml
    /// </summary>
    public partial class CalculatorSettings : UserControl
    {
        private readonly Settings _settings;
        private readonly SettingsViewModel _viewModel;

        public CalculatorSettings(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;
            _settings = viewModel.Settings;
            DataContext = viewModel;
            InitializeComponent();
        }

        #region Private

        private void CalculatorSettings_Loaded(object sender, RoutedEventArgs e)
        {
            DecimalSeparatorComboBox.SelectedItem = _settings.DecimalSeparator;
            MaxDecimalPlaces.SelectedItem = _settings.MaxDecimalPlaces;
        }

        #endregion
    }
}