namespace Wox.Plugin.PluginIndicator
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Plugin;

    public class Main : IPlugin, IPluginI18n
    {
        private PluginInitContext context;

        #region Public

        public List<Result> Query(Query query)
        {
            var results = from keyword in PluginManager.NonGlobalPlugins.Keys
                where keyword.StartsWith(query.Terms[0])
                let metadata = PluginManager.NonGlobalPlugins[keyword].Metadata
                where metadata.Disabled
                select new Result
                {
                    Title = keyword,
                    SubTitle = $"Activate {metadata.Name} plugin",
                    Score = 100,
                    IcoPath = metadata.IcoPath,
                    Action = c =>
                    {
                        context.API.ChangeQuery($"{keyword}{Plugin.Query.TermSeparator}");
                        return false;
                    }
                };
            return results.ToList();
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
        }

        public string GetTranslatedPluginTitle()
        {
            return context.API.GetTranslation("wox_plugin_pluginindicator_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return context.API.GetTranslation("wox_plugin_pluginindicator_plugin_description");
        }

        #endregion
    }
}