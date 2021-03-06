namespace Wox.Plugin.WebSearch
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using Infrastructure;
    using Infrastructure.Storage;

    public class Main : IPlugin, ISettingProvider, IPluginI18n, ISavable, IResultUpdated
    {
        public event ResultUpdatedEventHandler ResultsUpdated;

        public const string Images = "Images";
        public static string ImagesDirectory;

        private readonly Settings _settings;
        private readonly SettingsViewModel _viewModel;

        private readonly string SearchSourceGlobalPluginWildCardSign = "*";
        private PluginInitContext _context;
        private CancellationTokenSource _updateSource;
        private CancellationToken _updateToken;

        public Main()
        {
            _viewModel = new SettingsViewModel();
            _settings = _viewModel.Settings;
        }

        #region Public

        public void Save()
        {
            _viewModel.Save();
        }

        public List<Result> Query(Query query)
        {
            var searchSourceList = new List<SearchSource>();
            var results = new List<Result>();

            _updateSource?.Cancel();
            _updateSource = new CancellationTokenSource();
            _updateToken = _updateSource.Token;

            _settings.SearchSources.Where(o => (o.ActionKeyword == query.ActionKeyword || o.ActionKeyword == SearchSourceGlobalPluginWildCardSign)
                                               && o.Enabled)
                .ToList()
                .ForEach(x => searchSourceList.Add(x));

            if (searchSourceList.Any())
                foreach (var searchSource in searchSourceList)
                {
                    var keyword = string.Empty;
                    keyword = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? query.ToString() : query.Search;
                    var title = keyword;
                    var subtitle = _context.API.GetTranslation("wox_plugin_websearch_search") + " " + searchSource.Title;

                    if (string.IsNullOrEmpty(keyword))
                    {
                        var result = new Result
                        {
                            Score = 100,
                            Title = subtitle,
                            SubTitle = string.Empty,
                            IcoPath = searchSource.IconPath
                        };
                        results.Add(result);
                    }
                    else
                    {
                        var result = new Result
                        {
                            Title = title,
                            SubTitle = subtitle,
                            Score = 100,
                            IcoPath = searchSource.IconPath,
                            ActionKeywordAssigned = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? string.Empty : searchSource.ActionKeyword,
                            Action = c =>
                            {
                                if (_settings.OpenInNewBrowser)
                                    searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)).NewBrowserWindow(_settings.BrowserPath);
                                else
                                    searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)).NewTabInBrowser(_settings.BrowserPath);

                                return true;
                            }
                        };

                        results.Add(result);
                        UpdateResultsFromSuggestion(results, keyword, subtitle, searchSource, query);
                    }
                }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            var pluginDirectory = _context.CurrentPluginMetadata.PluginDirectory;
            var bundledImagesDirectory = Path.Combine(pluginDirectory, Images);
            ImagesDirectory = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, Images);
            Helper.ValidateDataDirectory(bundledImagesDirectory, ImagesDirectory);
        }

        public Control CreateSettingPanel()
        {
            return new SettingsControl(_context, _viewModel);
        }

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_description");
        }

        #endregion

        #region Private

        private void UpdateResultsFromSuggestion(List<Result> results, string keyword, string subtitle,
            SearchSource searchSource, Query query)
        {
            if (_settings.EnableSuggestion)
            {
                const int waitTime = 300;
                var task = Task.Run(async () =>
                {
                    var suggestions = await Suggestions(keyword, subtitle, searchSource);
                    results.AddRange(suggestions);
                }, _updateToken);

                if (!task.Wait(waitTime))
                    task.ContinueWith(_ => ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
                    {
                        Results = results,
                        Query = query
                    }), _updateToken);
            }
        }

        private async Task<IEnumerable<Result>> Suggestions(string keyword, string subtitle, SearchSource searchSource)
        {
            var source = _settings.SelectedSuggestion;
            if (source != null)
            {
                var suggestions = await source.Suggestions(keyword);
                var resultsFromSuggestion = suggestions.Select(o => new Result
                {
                    Title = o,
                    SubTitle = subtitle,
                    Score = 5,
                    IcoPath = searchSource.IconPath,
                    ActionKeywordAssigned = searchSource.ActionKeyword == SearchSourceGlobalPluginWildCardSign ? string.Empty : searchSource.ActionKeyword,
                    Action = c =>
                    {
                        if (_settings.OpenInNewBrowser)
                            searchSource.Url.Replace("{q}", Uri.EscapeDataString(o)).NewBrowserWindow(_settings.BrowserPath);
                        else
                            searchSource.Url.Replace("{q}", Uri.EscapeDataString(o)).NewTabInBrowser(_settings.BrowserPath);

                        return true;
                    }
                });
                return resultsFromSuggestion;
            }

            return new List<Result>();
        }

        #endregion
    }
}