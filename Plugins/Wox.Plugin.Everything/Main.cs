﻿namespace Wox.Plugin.Everything
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using Everything;
    using Infrastructure;
    using Infrastructure.Logger;
    using Infrastructure.Storage;
    using NLog;

    public class Main : IPlugin, ISettingProvider, IPluginI18n, IContextMenu, ISavable
    {
        public const string DLL = "Everything.dll";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly EverythingApi _api = new EverythingApi();


        private PluginInitContext _context;

        private Settings _settings;
        private PluginJsonStorage<Settings> _storage;
        private CancellationTokenSource _updateSource;

        #region Public

        public void Save()
        {
            _storage.Save();
        }

        public List<Result> Query(Query query)
        {
            if (_updateSource != null && !_updateSource.IsCancellationRequested)
            {
                _updateSource.Cancel();
                Logger.WoxDebug($"cancel init {_updateSource.Token.GetHashCode()} {Thread.CurrentThread.ManagedThreadId} {query.RawQuery}");
                _updateSource.Dispose();
            }

            var source = new CancellationTokenSource();
            _updateSource = source;
            var token = source.Token;

            var results = new List<Result>();
            if (!string.IsNullOrEmpty(query.Search))
            {
                var keyword = query.Search;

                try
                {
                    if (token.IsCancellationRequested) return results;
                    var searchList = _api.Search(keyword, token, _settings.MaxSearchCount);
                    if (token.IsCancellationRequested) return results;
                    for (var i = 0; i < searchList.Count; i++)
                    {
                        if (token.IsCancellationRequested) return results;
                        var searchResult = searchList[i];
                        var r = CreateResult(keyword, searchResult, i);
                        results.Add(r);
                    }
                }
                catch (IPCErrorException)
                {
                    results.Add(new Result
                    {
                        Title = _context.API.GetTranslation("wox_plugin_everything_is_not_running"),
                        IcoPath = "Images\\warning.png"
                    });
                }
                catch (Exception e)
                {
                    Logger.WoxError("Query Error", e);
                    results.Add(new Result
                    {
                        Title = _context.API.GetTranslation("wox_plugin_everything_query_error"),
                        SubTitle = e.Message,
                        Action = _ =>
                        {
                            Clipboard.SetText(e.Message + "\r\n" + e.StackTrace);
                            _context.API.ShowMsg(_context.API.GetTranslation("wox_plugin_everything_copied"), null, string.Empty);
                            return false;
                        },
                        IcoPath = "Images\\error.png"
                    });
                }
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            _storage = new PluginJsonStorage<Settings>();
            _settings = _storage.Load();
            if (_settings.MaxSearchCount <= 0) _settings.MaxSearchCount = Settings.DefaultMaxSearchCount;

            var pluginDirectory = context.CurrentPluginMetadata.PluginDirectory;
            const string sdk = "EverythingSDK";
            var sdkDirectory = Path.Combine(pluginDirectory, sdk, CpuType());
            var sdkPath = Path.Combine(sdkDirectory, DLL);
            Logger.WoxDebug($"sdk path <{sdkPath}>");
            Constant.EverythingSDKPath = sdkPath;
            _api.Load(sdkPath);
        }

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_everything_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_everything_plugin_description");
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var record = selectedResult.ContextData as SearchResult;
            var contextMenus = new List<Result>();
            if (record == null) return contextMenus;

            var availableContextMenus = new List<ContextMenu>();
            availableContextMenus.AddRange(GetDefaultContextMenu());
            availableContextMenus.AddRange(_settings.ContextMenus);

            if (record.Type == ResultType.File)
                foreach (var contextMenu in availableContextMenus)
                {
                    var menu = contextMenu;
                    contextMenus.Add(new Result
                    {
                        Title = contextMenu.Name,
                        Action = _ =>
                        {
                            var argument = menu.Argument.Replace("{path}", record.FullPath);
                            try
                            {
                                Process.Start(menu.Command, argument);
                            }
                            catch
                            {
                                _context.API.ShowMsg(string.Format(_context.API.GetTranslation("wox_plugin_everything_canot_start"), record.FullPath), string.Empty, string.Empty);
                                return false;
                            }

                            return true;
                        },
                        IcoPath = contextMenu.ImagePath
                    });
                }

            var icoPath = record.Type == ResultType.File ? "Images\\file.png" : "Images\\folder.png";
            contextMenus.Add(new Result
            {
                Title = _context.API.GetTranslation("wox_plugin_everything_copy_path"),
                Action = context =>
                {
                    Clipboard.SetText(record.FullPath);
                    return true;
                },
                IcoPath = icoPath
            });

            contextMenus.Add(new Result
            {
                Title = _context.API.GetTranslation("wox_plugin_everything_copy"),
                Action = context =>
                {
                    Clipboard.SetFileDropList(new StringCollection {record.FullPath});
                    return true;
                },
                IcoPath = icoPath
            });

            if (record.Type == ResultType.File || record.Type == ResultType.Folder)
                contextMenus.Add(new Result
                {
                    Title = _context.API.GetTranslation("wox_plugin_everything_delete"),
                    Action = context =>
                    {
                        try
                        {
                            if (record.Type == ResultType.File)
                                File.Delete(record.FullPath);
                            else
                                Directory.Delete(record.FullPath);
                        }
                        catch
                        {
                            _context.API.ShowMsg(string.Format(_context.API.GetTranslation("wox_plugin_everything_canot_delete"), record.FullPath), string.Empty, string.Empty);
                            return false;
                        }

                        return true;
                    },
                    IcoPath = icoPath
                });

            return contextMenus;
        }

        public Control CreateSettingPanel()
        {
            return new EverythingSettings(_settings);
        }

        #endregion

        #region Private

        private Result CreateResult(string keyword, SearchResult searchResult, int index)
        {
            var path = searchResult.FullPath;

            string workingDir = null;
            if (_settings.UseLocationAsWorkingDir)
                workingDir = Path.GetDirectoryName(path);

            var r = new Result
            {
                Score = _settings.MaxSearchCount - index,
                Title = searchResult.FileName,
                TitleHighlightData = searchResult.FileNameHighlightData,
                SubTitle = searchResult.FullPath,
                SubTitleHighlightData = searchResult.FullPathHighlightData,
                IcoPath = searchResult.FullPath,
                Action = c =>
                {
                    bool hide;
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true,
                            WorkingDirectory = workingDir
                        });
                        hide = true;
                    }
                    catch (Win32Exception)
                    {
                        var name = $"Plugin: {_context.CurrentPluginMetadata.Name}";
                        var message = "Can't open this file";
                        _context.API.ShowMsg(name, message, string.Empty);
                        hide = false;
                    }

                    return hide;
                },
                ContextData = searchResult
            };
            return r;
        }


        private List<ContextMenu> GetDefaultContextMenu()
        {
            var defaultContextMenus = new List<ContextMenu>();
            var openFolderContextMenu = new ContextMenu
            {
                Name = _context.API.GetTranslation("wox_plugin_everything_open_containing_folder"),
                Command = "explorer.exe",
                Argument = " /select,\"{path}\"",
                ImagePath = "Images\\folder.png"
            };

            defaultContextMenus.Add(openFolderContextMenu);

            var editorPath = string.IsNullOrEmpty(_settings.EditorPath) ? "notepad.exe" : _settings.EditorPath;

            var openWithEditorContextMenu = new ContextMenu
            {
                Name = string.Format(_context.API.GetTranslation("wox_plugin_everything_open_with_editor"), Path.GetFileNameWithoutExtension(editorPath)),
                Command = editorPath,
                Argument = " \"{path}\"",
                ImagePath = editorPath
            };

            defaultContextMenus.Add(openWithEditorContextMenu);

            return defaultContextMenus;
        }

        private static string CpuType()
        {
            if (!Environment.Is64BitProcess)
                return "x86";
            return "x64";
        }

        #endregion
    }
}