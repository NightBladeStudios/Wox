namespace Wox.Plugin.BrowserBookmark.Commands
{
    using Infrastructure;

    internal static class Bookmarks
    {
        #region Internal

        internal static bool MatchProgram(Bookmark bookmark, string queryString)
        {
            if (StringMatcher.FuzzySearch(queryString, bookmark.Name).IsSearchPrecisionScoreMet()) return true;
            //if (StringMatcher.FuzzySearch(queryString, bookmark.PinyinName).IsSearchPrecisionScoreMet()) return true;
            if (StringMatcher.FuzzySearch(queryString, bookmark.Url).IsSearchPrecisionScoreMet()) return true;

            return false;
        }

        #endregion
    }
}