namespace Wox.Plugin
{
    using System;
    using System.Collections.Generic;

    public interface IFeatures
    {
    }

    public interface IContextMenu : IFeatures
    {
        #region Public

        List<Result> LoadContextMenus(Result selectedResult);

        #endregion
    }

    [Obsolete("If a plugin has a action keyword, then it is exclusive. This interface will be remove in v1.4.0")]
    public interface IExclusiveQuery : IFeatures
    {
        [Obsolete("If a plugin has a action keyword, then it is exclusive. This method will be remove in v1.4.0")]
        bool IsExclusiveQuery(Query query);
    }

    /// <summary>
    /// Represent plugin query will be executed in UI thread directly. Don't do long-running operation in Query method if you
    /// implement this interface
    /// <remarks>This will improve the performance of instant search like websearch or cmd plugin</remarks>
    /// </summary>
    [Obsolete("Wox is fast enough now, executed on ui thread is no longer needed")]
    public interface IInstantQuery : IFeatures
    {
        #region Public

        bool IsInstantQuery(string query);

        #endregion
    }

    /// <summary>
    /// Represent plugins that support internationalization
    /// </summary>
    public interface IPluginI18n : IFeatures
    {
        #region Public

        string GetTranslatedPluginTitle();

        string GetTranslatedPluginDescription();

        #endregion
    }

    public interface IResultUpdated : IFeatures
    {
        event ResultUpdatedEventHandler ResultsUpdated;
    }

    public delegate void ResultUpdatedEventHandler(IResultUpdated sender, ResultUpdatedEventArgs e);

    public class ResultUpdatedEventArgs : EventArgs
    {
        public Query Query;
        public List<Result> Results;
    }
}