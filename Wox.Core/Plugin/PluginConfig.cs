namespace Wox.Core.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Infrastructure.Logger;
    using Newtonsoft.Json;
    using NLog;
    using Wox.Plugin;

    internal abstract class PluginConfig
    {
        internal const string PluginConfigName = "plugin.json";
        private static readonly List<PluginMetadata> PluginMetadatas = new List<PluginMetadata>();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Public

        /// <summary>
        /// Parse plugin metadata in giving directories
        /// </summary>
        /// <param name="pluginDirectories"></param>
        /// <returns></returns>
        public static List<PluginMetadata> Parse(string[] pluginDirectories)
        {
            PluginMetadatas.Clear();
            var directories = pluginDirectories.SelectMany(Directory.GetDirectories);
            ParsePluginConfigs(directories);
            return PluginMetadatas;
        }

        #endregion

        #region Private

        private static void ParsePluginConfigs(IEnumerable<string> directories)
        {
            // todo use linq when diable plugin is implmented since parallel.foreach + list is not thread saft
            foreach (var directory in directories)
                if (File.Exists(Path.Combine(directory, "NeedDelete.txt")))
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch (Exception e)
                    {
                        Logger.WoxError($"Can't delete <{directory}>", e);
                    }
                }
                else
                {
                    var metadata = GetPluginMetadata(directory);
                    if (metadata != null) PluginMetadatas.Add(metadata);
                }
        }

        private static PluginMetadata GetPluginMetadata(string pluginDirectory)
        {
            var configPath = Path.Combine(pluginDirectory, PluginConfigName);
            if (!File.Exists(configPath))
            {
                Logger.WoxError($"Didn't find config file <{configPath}>");
                return null;
            }

            PluginMetadata metadata;
            try
            {
                metadata = JsonConvert.DeserializeObject<PluginMetadata>(File.ReadAllText(configPath));
                metadata.PluginDirectory = pluginDirectory;
                // for plugins which doesn't has ActionKeywords key
                metadata.ActionKeywords = metadata.ActionKeywords ?? new List<string> {metadata.ActionKeyword};
                // for plugin still use old ActionKeyword
                metadata.ActionKeyword = metadata.ActionKeywords?[0];
            }
            catch (Exception e)
            {
                e.Data.Add(nameof(configPath), configPath);
                e.Data.Add(nameof(pluginDirectory), pluginDirectory);
                Logger.WoxError($"invalid json for config <{configPath}>", e);
                return null;
            }


            if (!AllowedLanguage.IsAllowed(metadata.Language))
            {
                Logger.WoxError($"Invalid language <{metadata.Language}> for config <{configPath}>");
                return null;
            }

            if (!File.Exists(metadata.ExecuteFilePath))
            {
                Logger.WoxError($"execute file path didn't exist <{metadata.ExecuteFilePath}> for conifg <{configPath}");
                return null;
            }

            return metadata;
        }

        #endregion
    }
}