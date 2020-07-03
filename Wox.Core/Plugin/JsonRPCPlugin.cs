namespace Wox.Core.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Forms;
    using Infrastructure.Logger;
    using Newtonsoft.Json;
    using NLog;
    using Wox.Plugin;

    /// <summary>
    /// Represent the plugin that using JsonPRC
    /// every JsonRPC plugin should has its own plugin instance
    /// </summary>
    internal abstract class JsonRPCPlugin : IPlugin, IContextMenu
    {
        public const string JsonRPC = "JsonRPC";

        /// <summary>
        /// The language this JsonRPCPlugin support
        /// </summary>
        public abstract string SupportedLanguage { get; set; }

        protected PluginInitContext context;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Public

        public List<Result> Query(Query query)
        {
            var output = ExecuteQuery(query);
            try
            {
                return DeserializedResult(output);
            }
            catch (Exception e)
            {
                Logger.WoxError($"Exception when query <{query}>", e);
                return null;
            }
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var output = ExecuteContextMenu(selectedResult);
            try
            {
                return DeserializedResult(output);
            }
            catch (Exception e)
            {
                Logger.WoxError($"Exception on result <{selectedResult}>", e);
                return null;
            }
        }

        public void Init(PluginInitContext ctx)
        {
            context = ctx;
        }

        #endregion

        #region Protected

        protected abstract string ExecuteQuery(Query query);
        protected abstract string ExecuteCallback(JsonRPCRequestModel rpcRequest);
        protected abstract string ExecuteContextMenu(Result selectedResult);

        /// <summary>
        /// Execute external program and return the output
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected string Execute(string fileName, string arguments)
        {
            var start = new ProcessStartInfo();
            start.FileName = fileName;
            start.Arguments = arguments;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            return Execute(start);
        }

        protected string Execute(ProcessStartInfo startInfo)
        {
            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                        using (var standardOutput = process.StandardOutput)
                        {
                            var result = standardOutput.ReadToEnd();
                            if (string.IsNullOrEmpty(result))
                                using (var standardError = process.StandardError)
                                {
                                    var error = standardError.ReadToEnd();
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        Logger.WoxError($"{error}");
                                        return string.Empty;
                                    }

                                    Logger.WoxError("Empty standard output and standard error.");
                                    return string.Empty;
                                }

                            if (result.StartsWith("DEBUG:"))
                            {
                                MessageBox.Show(new Form {TopMost = true}, result.Substring(6));
                                return string.Empty;
                            }

                            return result;
                        }

                    Logger.WoxError("Can't start new process");
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                Logger.WoxError($"Exception for filename <{startInfo.FileName}> with argument <{startInfo.Arguments}>", e);
                return string.Empty;
            }
        }

        #endregion

        #region Private

        private List<Result> DeserializedResult(string output)
        {
            if (!string.IsNullOrEmpty(output))
            {
                var results = new List<Result>();

                var queryResponseModel = JsonConvert.DeserializeObject<JsonRPCQueryResponseModel>(output);
                if (queryResponseModel.Result == null) return null;

                foreach (var result in queryResponseModel.Result)
                {
                    var result1 = result;
                    result.Action = c =>
                    {
                        if (result1.JsonRPCAction == null) return false;

                        if (!string.IsNullOrEmpty(result1.JsonRPCAction.Method))
                        {
                            if (result1.JsonRPCAction.Method.StartsWith("Wox."))
                            {
                                ExecuteWoxAPI(result1.JsonRPCAction.Method.Substring(4), result1.JsonRPCAction.Parameters);
                            }
                            else
                            {
                                var actionResponse = ExecuteCallback(result1.JsonRPCAction);
                                var jsonRpcRequestModel = JsonConvert.DeserializeObject<JsonRPCRequestModel>(actionResponse);
                                if (jsonRpcRequestModel != null
                                    && !string.IsNullOrEmpty(jsonRpcRequestModel.Method)
                                    && jsonRpcRequestModel.Method.StartsWith("Wox."))
                                    ExecuteWoxAPI(jsonRpcRequestModel.Method.Substring(4), jsonRpcRequestModel.Parameters);
                            }
                        }

                        return !result1.JsonRPCAction.DontHideAfterAction;
                    };
                    results.Add(result);
                }

                return results;
            }

            return null;
        }

        private void ExecuteWoxAPI(string method, object[] parameters)
        {
            var methodInfo = PluginManager.API.GetType().GetMethod(method);
            if (methodInfo != null)
                try
                {
                    methodInfo.Invoke(PluginManager.API, parameters);
                }
                catch (Exception)
                {
#if (DEBUG)
                    {
                        throw;
                    }
#endif
                }
        }

        #endregion
    }
}