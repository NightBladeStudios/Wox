namespace Wox.Plugin.Program.Programs
{
    using System.Collections.Generic;

    public interface IProgram
    {
        string Name { get; }
        string Location { get; }

        #region Public

        List<Result> ContextMenus(IPublicAPI api);
        Result Result(string query, IPublicAPI api);

        #endregion
    }
}