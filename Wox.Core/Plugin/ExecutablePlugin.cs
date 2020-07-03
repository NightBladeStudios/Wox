namespace Wox.Core.Plugin
{
    using System.Diagnostics;
    using Wox.Plugin;

    internal class ExecutablePlugin : JsonRPCPlugin
    {
        public override string SupportedLanguage { get; set; } = AllowedLanguage.Executable;
        private readonly ProcessStartInfo _startInfo;

        public ExecutablePlugin(string filename)
        {
            _startInfo = new ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }

        #region Protected

        protected override string ExecuteQuery(Query query)
        {
            var request = new JsonRPCServerRequestModel
            {
                Method = "query",
                Parameters = new object[] {query.Search}
            };

            _startInfo.Arguments = $"\"{request}\"";

            return Execute(_startInfo);
        }

        protected override string ExecuteCallback(JsonRPCRequestModel rpcRequest)
        {
            _startInfo.Arguments = $"\"{rpcRequest}\"";
            return Execute(_startInfo);
        }

        protected override string ExecuteContextMenu(Result selectedResult)
        {
            var request = new JsonRPCServerRequestModel
            {
                Method = "contextmenu",
                Parameters = new[] {selectedResult.ContextData}
            };

            _startInfo.Arguments = $"\"{request}\"";

            return Execute(_startInfo);
        }

        #endregion
    }
}