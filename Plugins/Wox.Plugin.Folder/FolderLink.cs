﻿namespace Wox.Plugin.Folder
{
    using System;
    using System.Linq;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class FolderLink
    {
        [JsonProperty]
        public string Path { get; set; }

        public string Nickname =>
            Path.Split(new[] {System.IO.Path.DirectorySeparatorChar}, StringSplitOptions.None)
                .Last()
            + " (" + System.IO.Path.GetDirectoryName(Path) + ")";
    }
}