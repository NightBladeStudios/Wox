namespace Wox.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptOut)]
    public class PluginMetadata : BaseModel
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public bool Disabled { get; set; }
        public string ExecuteFilePath { get; private set; }

        public string ExecuteFileName { get; set; }

        public string PluginDirectory
        {
            get => _pluginDirectory;
            internal set
            {
                _pluginDirectory = value;
                ExecuteFilePath = Path.Combine(value, ExecuteFileName);
                IcoPath = Path.Combine(value, IcoPath);
            }
        }

        public string ActionKeyword { get; set; }

        public List<string> ActionKeywords { get; set; }

        public string IcoPath { get; set; }

        // keep plugin raw score by not multiply selected counts
        public bool KeepResultRawScore { get; set; }

        [Obsolete("Use IcoPath")]
        public string FullIcoPath => IcoPath;

        /// <summary>
        /// Init time include both plugin load time and init time
        /// </summary>
        [JsonIgnore]
        public long InitTime { get; set; }

        [JsonIgnore]
        public long AvgQueryTime { get; set; }

        [JsonIgnore]
        public int QueryCount { get; set; }

        private string _pluginDirectory;

        #region Public

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}