namespace Wox.Infrastructure.UserSettings
{
    using Wox.Plugin;

    public class CustomPluginHotkey : BaseModel
    {
        public string Hotkey { get; set; }
        public string ActionKeyword { get; set; }
    }
}