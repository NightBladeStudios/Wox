namespace Wox.Plugin.WebSearch.SuggestionSources
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public abstract class SuggestionSource
    {
        #region Public

        public abstract Task<List<string>> Suggestions(string query);

        #endregion
    }
}