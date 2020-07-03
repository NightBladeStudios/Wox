namespace Wox.Core.Plugin
{
    using System;
    using System.IO;
    using System.Windows;
    using ICSharpCode.SharpZipLib.Zip;
    using Infrastructure.UserSettings;
    using Newtonsoft.Json;
    using Wox.Plugin;

    internal class PluginInstaller
    {
        #region Internal

        internal static void Install(string path)
        {
            if (File.Exists(path))
            {
                var tempFoler = Path.Combine(Path.GetTempPath(), "wox\\plugins");
                if (Directory.Exists(tempFoler)) Directory.Delete(tempFoler, true);
                UnZip(path, tempFoler, true);

                var iniPath = Path.Combine(tempFoler, PluginConfig.PluginConfigName);
                if (!File.Exists(iniPath))
                {
                    MessageBox.Show("Install failed: plugin config is missing");
                    return;
                }

                var plugin = GetMetadataFromJson(tempFoler);
                if (plugin == null || plugin.Name == null)
                {
                    MessageBox.Show("Install failed: plugin config is invalid");
                    return;
                }

                var pluginFolerPath = DataLocation.PluginsDirectory;

                var newPluginName = plugin.Name
                                        .Replace("/", "_")
                                        .Replace("\\", "_")
                                        .Replace(":", "_")
                                        .Replace("<", "_")
                                        .Replace(">", "_")
                                        .Replace("?", "_")
                                        .Replace("*", "_")
                                        .Replace("|", "_")
                                    + "-" + Guid.NewGuid();
                var newPluginPath = Path.Combine(pluginFolerPath, newPluginName);
                var content = $"Do you want to install following plugin?{Environment.NewLine}{Environment.NewLine}" +
                              $"Name: {plugin.Name}{Environment.NewLine}" +
                              $"Version: {plugin.Version}{Environment.NewLine}" +
                              $"Author: {plugin.Author}";
                var existingPlugin = PluginManager.GetPluginForId(plugin.ID);

                if (existingPlugin != null)
                    content = $"Do you want to update following plugin?{Environment.NewLine}{Environment.NewLine}" +
                              $"Name: {plugin.Name}{Environment.NewLine}" +
                              $"Old Version: {existingPlugin.Metadata.Version}" +
                              $"{Environment.NewLine}New Version: {plugin.Version}" +
                              $"{Environment.NewLine}Author: {plugin.Author}";

                var result = MessageBox.Show(content, "Install plugin", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (existingPlugin != null && Directory.Exists(existingPlugin.Metadata.PluginDirectory))
                        //when plugin is in use, we can't delete them. That's why we need to make plugin folder a random name
                        File.Create(Path.Combine(existingPlugin.Metadata.PluginDirectory, "NeedDelete.txt")).Close();

                    UnZip(path, newPluginPath, true);
                    Directory.Delete(tempFoler, true);

                    //exsiting plugins may be has loaded by application,
                    //if we try to delelte those kind of plugins, we will get a  error that indicate the
                    //file is been used now.
                    //current solution is to restart wox. Ugly.
                    //if (MainWindow.Initialized)
                    //{
                    //    Plugins.Initialize();
                    //}
                    if (MessageBox.Show($"You have installed plugin {plugin.Name} successfully.{Environment.NewLine}" +
                                        "Restart Wox to take effect?",
                            "Install plugin", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        PluginManager.API.RestartApp();
                }
            }
        }

        #endregion

        #region Private

        private static PluginMetadata GetMetadataFromJson(string pluginDirectory)
        {
            var configPath = Path.Combine(pluginDirectory, PluginConfig.PluginConfigName);
            PluginMetadata metadata;

            if (!File.Exists(configPath)) return null;

            try
            {
                metadata = JsonConvert.DeserializeObject<PluginMetadata>(File.ReadAllText(configPath));
                metadata.PluginDirectory = pluginDirectory;
            }
            catch (Exception)
            {
                var error = $"Parse plugin config {configPath} failed: json format is not valid";
#if (DEBUG)
                {
                    throw new Exception(error);
                }
#endif
                return null;
            }


            if (!AllowedLanguage.IsAllowed(metadata.Language))
            {
                var error = $"Parse plugin config {configPath} failed: invalid language {metadata.Language}";
#if (DEBUG)
                {
                    throw new Exception(error);
                }
#endif
                return null;
            }

            if (!File.Exists(metadata.ExecuteFilePath))
            {
                var error = $"Parse plugin config {configPath} failed: ExecuteFile {metadata.ExecuteFilePath} didn't exist";
#if (DEBUG)
                {
                    throw new Exception(error);
                }
#endif
                return null;
            }

            return metadata;
        }

        /// <summary>
        /// unzip
        /// </summary>
        /// <param name="zipedFile">The ziped file.</param>
        /// <param name="strDirectory">The STR directory.</param>
        /// <param name="overWrite">overwirte</param>
        private static void UnZip(string zipedFile, string strDirectory, bool overWrite)
        {
            if (strDirectory == "")
                strDirectory = Directory.GetCurrentDirectory();
            if (!strDirectory.EndsWith("\\"))
                strDirectory = strDirectory + "\\";

            using (var s = new ZipInputStream(File.OpenRead(zipedFile)))
            {
                ZipEntry theEntry;

                while ((theEntry = s.GetNextEntry()) != null)
                {
                    var directoryName = "";
                    var pathToZip = "";
                    pathToZip = theEntry.Name;

                    if (pathToZip != "")
                        directoryName = Path.GetDirectoryName(pathToZip) + "\\";

                    var fileName = Path.GetFileName(pathToZip);

                    Directory.CreateDirectory(strDirectory + directoryName);

                    if (fileName != "")
                        if (File.Exists(strDirectory + directoryName + fileName) && overWrite || !File.Exists(strDirectory + directoryName + fileName))
                            using (var streamWriter = File.Create(strDirectory + directoryName + fileName))
                            {
                                var data = new byte[2048];
                                while (true)
                                {
                                    var size = s.Read(data, 0, data.Length);

                                    if (size > 0)
                                        streamWriter.Write(data, 0, size);
                                    else
                                        break;
                                }

                                streamWriter.Close();
                            }
                }

                s.Close();
            }
        }

        #endregion
    }
}