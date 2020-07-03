namespace Wox.Plugin.Everything
{
    using System.Collections.Generic;

    public class Settings
    {
        public const int DefaultMaxSearchCount = 30;

        public string EditorPath { get; set; } = "";

        public int MaxSearchCount { get; set; } = DefaultMaxSearchCount;

        public bool UseLocationAsWorkingDir { get; set; } = false;

        public List<ContextMenu> ContextMenus = new List<ContextMenu>();
    }

    public class ContextMenu
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string Argument { get; set; }
        public string ImagePath { get; set; }
    }
}