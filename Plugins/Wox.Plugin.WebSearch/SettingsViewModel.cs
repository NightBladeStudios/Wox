namespace Wox.Plugin.WebSearch
{
    using Infrastructure.Storage;

    public class SettingsViewModel
    {
        public Settings Settings { get; set; }
        private readonly PluginJsonStorage<Settings> _storage;

        public SettingsViewModel()
        {
            _storage = new PluginJsonStorage<Settings>();
            Settings = _storage.Load();
        }

        #region Public

        public void Save()
        {
            _storage.Save();
        }

        #endregion
    }
}