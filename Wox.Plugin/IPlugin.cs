namespace Wox.Plugin
{
    using System.Collections.Generic;

    public interface IPlugin
    {
        #region Public

        List<Result> Query(Query query);
        void Init(PluginInitContext context);

        #endregion
    }
}