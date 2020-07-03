namespace Wox.Plugin.ControlPanel
{
    using System.Diagnostics;

    //from:https://raw.githubusercontent.com/CoenraadS/Windows-Control-Panel-Items
    public class ControlPanelItem
    {
        public string LocalizedString { get; }
        public string InfoTip { get; }
        public string GUID { get; }
        public ProcessStartInfo ExecutablePath { get; }
        public string IconPath { get; }
        public int Score { get; set; }

        public ControlPanelItem(string newLocalizedString, string newInfoTip, string newGUID, ProcessStartInfo newExecutablePath, string iconPath)
        {
            LocalizedString = newLocalizedString;
            InfoTip = newInfoTip;
            ExecutablePath = newExecutablePath;
            GUID = newGUID;
            var key = "EmbeddedIcon:";
            IconPath = $"{key}{iconPath}";
        }
    }
}