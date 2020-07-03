namespace HelloWorldCSharp
{
    using System.Collections.Generic;
    using System.IO;
    using Wox.Plugin;

    internal class Main : IPlugin
    {
        #region Public

        public List<Result> Query(Query query)
        {
            var result = new Result
            {
                Title = "Hello World from CSharp",
                SubTitle = $"Query: {query.Search}",
                IcoPath = Path.Combine("Images", "app.png")
            };
            return new List<Result> {result};
        }

        public void Init(PluginInitContext context)
        {
        }

        #endregion
    }
}