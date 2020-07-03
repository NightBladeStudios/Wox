namespace Wox.Plugin.Calculator.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.Storage;

    public class SettingsViewModel : BaseModel, ISavable
    {
        public Settings Settings { get; set; }

        public IEnumerable<int> MaxDecimalPlacesRange => Enumerable.Range(1, 20);
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