namespace Wox.Plugin.Folder
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class Settings
    {
        [JsonProperty]
        public List<FolderLink> FolderLinks { get; set; } = new List<FolderLink>();
    }
}