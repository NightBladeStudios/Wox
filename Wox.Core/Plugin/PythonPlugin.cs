﻿namespace Wox.Core.Plugin
{
    using System.Diagnostics;
    using System.IO;
    using Infrastructure;
    using Wox.Plugin;

    internal class PythonPlugin : JsonRPCPlugin
    {
        public override string SupportedLanguage { get; set; } = AllowedLanguage.Python;
        private readonly ProcessStartInfo _startInfo;

        public PythonPlugin(string filename)
        {
            _startInfo = new ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // temp fix for issue #667
            var path = Path.Combine(Constant.ProgramDirectory, JsonRPC);
            _startInfo.EnvironmentVariables["PYTHONPATH"] = path;
        }

        #region Protected

        protected override string ExecuteQuery(Query query)
        {
            var request = new JsonRPCServerRequestModel
            {
                Method = "query",
                Parameters = new object[] {query.Search}
            };
            //Add -B flag to tell python don't write .py[co] files. Because .pyc contains location infos which will prevent python portable
            _startInfo.Arguments = $"-B \"{context.CurrentPluginMetadata.ExecuteFilePath}\" \"{request}\"";
            // todo why context can't be used in constructor
            _startInfo.WorkingDirectory = context.CurrentPluginMetadata.PluginDirectory;

            return Execute(_startInfo);
        }

        protected override string ExecuteCallback(JsonRPCRequestModel rpcRequest)
        {
            _startInfo.Arguments = $"-B \"{context.CurrentPluginMetadata.ExecuteFilePath}\" \"{rpcRequest}\"";
            _startInfo.WorkingDirectory = context.CurrentPluginMetadata.PluginDirectory;
            return Execute(_startInfo);
        }

        protected override string ExecuteContextMenu(Result selectedResult)
        {
            var request = new JsonRPCServerRequestModel
            {
                Method = "context_menu",
                Parameters = new[] {selectedResult.ContextData}
            };
            _startInfo.Arguments = $"-B \"{context.CurrentPluginMetadata.ExecuteFilePath}\" \"{request}\"";
            _startInfo.WorkingDirectory = context.CurrentPluginMetadata.PluginDirectory;

            return Execute(_startInfo);
        }

        #endregion
    }
}