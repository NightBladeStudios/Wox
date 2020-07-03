namespace Wox.Plugin.PluginManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using Infrastructure;
    using Infrastructure.Http;
    using Infrastructure.Logger;
    using Newtonsoft.Json;
    using NLog;

    public class Main : IPlugin, IPluginI18n
    {
        private const string ListCommand = "list";
        private const string InstallCommand = "install";
        private const string UninstallCommand = "uninstall";
        private static readonly string APIBASE = "http://api.wox.one";
        private static readonly string pluginSearchUrl = APIBASE + "/plugin/search/";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private PluginInitContext context;

        #region Public

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            if (string.IsNullOrEmpty(query.Search))
            {
                results.Add(ResultForListCommandAutoComplete(query));
                results.Add(ResultForInstallCommandAutoComplete(query));
                results.Add(ResultForUninstallCommandAutoComplete(query));
                return results;
            }

            var command = query.FirstSearch.ToLower();
            if (string.IsNullOrEmpty(command)) return results;

            if (command == ListCommand) return ResultForListInstalledPlugins();
            if (command == UninstallCommand) return ResultForUnInstallPlugin(query);
            if (command == InstallCommand) return ResultForInstallPlugin(query);

            if (InstallCommand.Contains(command)) results.Add(ResultForInstallCommandAutoComplete(query));
            if (UninstallCommand.Contains(command)) results.Add(ResultForUninstallCommandAutoComplete(query));
            if (ListCommand.Contains(command)) results.Add(ResultForListCommandAutoComplete(query));

            return results;
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
        }

        public string GetTranslatedPluginTitle()
        {
            return context.API.GetTranslation("wox_plugin_plugin_management_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return context.API.GetTranslation("wox_plugin_plugin_management_plugin_description");
        }

        #endregion

        #region Private

        private Result ResultForListCommandAutoComplete(Query query)
        {
            var title = ListCommand;
            var subtitle = "list installed plugins";
            return ResultForCommand(query, ListCommand, title, subtitle);
        }

        private Result ResultForInstallCommandAutoComplete(Query query)
        {
            var title = $"{InstallCommand} <Package Name>";
            var subtitle = "list installed plugins";
            return ResultForCommand(query, InstallCommand, title, subtitle);
        }

        private Result ResultForUninstallCommandAutoComplete(Query query)
        {
            var title = $"{UninstallCommand} <Package Name>";
            var subtitle = "list installed plugins";
            return ResultForCommand(query, UninstallCommand, title, subtitle);
        }

        private Result ResultForCommand(Query query, string command, string title, string subtitle)
        {
            const string separator = Plugin.Query.TermSeparator;
            var result = new Result
            {
                Title = title,
                IcoPath = "Images\\plugin.png",
                SubTitle = subtitle,
                Action = e =>
                {
                    context.API.ChangeQuery($"{query.ActionKeyword}{separator}{command}{separator}");
                    return false;
                }
            };
            return result;
        }

        private List<Result> ResultForInstallPlugin(Query query)
        {
            var results = new List<Result>();
            var pluginName = query.SecondSearch;
            if (string.IsNullOrEmpty(pluginName)) return results;
            string json;
            try
            {
                json = Http.Get(pluginSearchUrl + pluginName).Result;
            }
            catch (WebException e)
            {
                //todo add option in log to decide give user prompt or not
                context.API.ShowMsg("PluginManagement.ResultForInstallPlugin: Can't connect to Wox plugin website, check your conenction");
                Logger.WoxError("Can't connect to Wox plugin website, check your conenction", e);
                return new List<Result>();
            }

            List<WoxPluginResult> searchedPlugins;
            try
            {
                searchedPlugins = JsonConvert.DeserializeObject<List<WoxPluginResult>>(json);
            }
            catch (JsonSerializationException e)
            {
                context.API.ShowMsg("PluginManagement.ResultForInstallPlugin: Coundn't parse api search results, Please update your Wox!");
                Logger.WoxError("Coundn't parse api search results, Please update your Wox!", e);
                return results;
            }

            foreach (var r in searchedPlugins)
            {
                var r1 = r;
                results.Add(new Result
                {
                    Title = r.name,
                    SubTitle = r.description,
                    IcoPath = "Images\\plugin.png",
                    TitleHighlightData = StringMatcher.FuzzySearch(query.SecondSearch, r.name).MatchData,
                    SubTitleHighlightData = StringMatcher.FuzzySearch(query.SecondSearch, r.description).MatchData,
                    Action = c =>
                    {
                        var result = MessageBox.Show("Are you sure you wish to install the \'" + r.name + "\' plugin",
                            "Install plugin", MessageBoxButton.YesNo);

                        if (result == MessageBoxResult.Yes)
                        {
                            var folder = Path.Combine(Path.GetTempPath(), "WoxPluginDownload");
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                            var filePath = Path.Combine(folder, Guid.NewGuid() + ".wox");

                            var pluginUrl = APIBASE + "/media/" + r1.plugin_file;

                            try
                            {
                                Http.Download(pluginUrl, filePath);
                            }
                            catch (WebException e)
                            {
                                context.API.ShowMsg($"PluginManagement.ResultForInstallPlugin: download failed for <{r.name}>");
                                Logger.WoxError($"download failed for <{r.name}>", e);
                                return false;
                            }

                            context.API.InstallPlugin(filePath);
                        }

                        return false;
                    }
                });
            }

            return results;
        }

        private List<Result> ResultForUnInstallPlugin(Query query)
        {
            var results = new List<Result>();
            var allInstalledPlugins = context.API.GetAllPlugins().Select(o => o.Metadata).ToList();
            if (!string.IsNullOrEmpty(query.SecondSearch))
                allInstalledPlugins =
                    allInstalledPlugins.Where(o => o.Name.ToLower().Contains(query.SecondSearch.ToLower())).ToList();

            foreach (var plugin in allInstalledPlugins)
                results.Add(new Result
                {
                    Title = plugin.Name,
                    SubTitle = plugin.Description,
                    IcoPath = plugin.IcoPath,
                    TitleHighlightData = StringMatcher.FuzzySearch(query.SecondSearch, plugin.Name).MatchData,
                    SubTitleHighlightData = StringMatcher.FuzzySearch(query.SecondSearch, plugin.Description).MatchData,
                    Action = e =>
                    {
                        UnInstallPlugin(plugin);
                        return false;
                    }
                });
            return results;
        }

        private void UnInstallPlugin(PluginMetadata plugin)
        {
            var content = $"Do you want to uninstall following plugin?{Environment.NewLine}{Environment.NewLine}" +
                          $"Name: {plugin.Name}{Environment.NewLine}" +
                          $"Version: {plugin.Version}{Environment.NewLine}" +
                          $"Author: {plugin.Author}";
            if (MessageBox.Show(content, "Wox", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                File.Create(Path.Combine(plugin.PluginDirectory, "NeedDelete.txt")).Close();
                var result = MessageBox.Show($"You have uninstalled plugin {plugin.Name} successfully.{Environment.NewLine}" +
                                             "Restart Wox to take effect?",
                    "Install plugin", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) context.API.RestartApp();
            }
        }

        private List<Result> ResultForListInstalledPlugins()
        {
            var results = new List<Result>();
            foreach (var plugin in context.API.GetAllPlugins().Select(o => o.Metadata))
            {
                var actionKeywordString = string.Join(" or ", plugin.ActionKeywords.ToArray());
                results.Add(new Result
                {
                    Title = $"{plugin.Name} - Action Keywords: {actionKeywordString}",
                    SubTitle = plugin.Description,
                    IcoPath = plugin.IcoPath
                });
            }

            return results;
        }

        #endregion
    }
}