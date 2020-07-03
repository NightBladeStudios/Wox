namespace Wox.Plugin
{
    using System.Windows.Controls;

    public interface ISettingProvider
    {
        #region Public

        Control CreateSettingPanel();

        #endregion
    }
}