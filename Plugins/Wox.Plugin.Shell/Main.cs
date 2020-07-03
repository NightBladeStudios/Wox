namespace Wox.Plugin.Shell
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using WindowsInput;
    using WindowsInput.Native;
    using Infrastructure;
    using Infrastructure.Hotkey;
    using Infrastructure.Logger;
    using Infrastructure.Storage;
    using NLog;
    using Application = System.Windows.Application;
    using Control = System.Windows.Controls.Control;

    public class Main : IPlugin, ISettingProvider, IPluginI18n, IContextMenu, ISavable
    {
        private const string Image = "Images/shell.png";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly KeyboardSimulator _keyboardSimulator = new KeyboardSimulator(new InputSimulator());

        private readonly Settings _settings;
        private readonly PluginJsonStorage<Settings> _storage;
        private PluginInitContext _context;
        private bool _winRStroked;

        public Main()
        {
            _storage = new PluginJsonStorage<Settings>();
            _settings = _storage.Load();
        }

        #region Public

        public void Save()
        {
            _storage.Save();
        }


        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            var cmd = query.Search;
            if (string.IsNullOrEmpty(cmd)) return ResultsFromHistory();

            var queryCmd = GetCurrentCmd(cmd);
            results.Add(queryCmd);
            var history = GetHistoryCmds(cmd, queryCmd);
            results.AddRange(history);

            try
            {
                string basedir = null;
                string dir = null;
                var excmd = Environment.ExpandEnvironmentVariables(cmd);
                if (Directory.Exists(excmd) && (cmd.EndsWith("/") || cmd.EndsWith(@"\")))
                {
                    basedir = excmd;
                    dir = cmd;
                }
                else if (Directory.Exists(Path.GetDirectoryName(excmd) ?? string.Empty))
                {
                    basedir = Path.GetDirectoryName(excmd);
                    var directoryName = Path.GetDirectoryName(cmd);
                    dir = directoryName.EndsWith("/") || directoryName.EndsWith(@"\") ? directoryName : cmd.Substring(0, directoryName.Length + 1);
                }

                if (basedir != null)
                {
                    var autocomplete = Directory.GetFileSystemEntries(basedir).Select(o => dir + Path.GetFileName(o)).Where(o => o.StartsWith(cmd, StringComparison.OrdinalIgnoreCase) &&
                                                                                                                                 !results.Any(p => o.Equals(p.Title, StringComparison.OrdinalIgnoreCase)) &&
                                                                                                                                 !results.Any(p => o.Equals(p.Title, StringComparison.OrdinalIgnoreCase))).ToList();
                    autocomplete.Sort();
                    results.AddRange(autocomplete.ConvertAll(m => new Result
                    {
                        Title = m,
                        IcoPath = Image,
                        Action = c =>
                        {
                            Execute(Process.Start, PrepareProcessStartInfo(m));
                            return true;
                        }
                    }));
                }
            }
            catch (Exception e)
            {
                Logger.WoxError($"Exception when query for <{query}>", e);
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            context.API.GlobalKeyboardEvent += API_GlobalKeyboardEvent;
        }

        public Control CreateSettingPanel()
        {
            return new CMDSetting(_settings);
        }

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_cmd_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_cmd_plugin_description");
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>
            {
                new Result
                {
                    Title = _context.API.GetTranslation("wox_plugin_cmd_run_as_different_user"),
                    Action = c =>
                    {
                        Task.Run(() => Execute(ShellCommand.RunAsDifferentUser, PrepareProcessStartInfo(selectedResult.Title)));
                        return true;
                    },
                    IcoPath = "Images/app.png"
                },
                new Result
                {
                    Title = _context.API.GetTranslation("wox_plugin_cmd_run_as_administrator"),
                    Action = c =>
                    {
                        Execute(Process.Start, PrepareProcessStartInfo(selectedResult.Title, true));
                        return true;
                    },
                    IcoPath = Image
                }
            };

            return results;
        }

        #endregion

        #region Private

        private List<Result> GetHistoryCmds(string cmd, Result result)
        {
            var history = _settings.Count.Where(o => o.Key.Contains(cmd))
                .OrderByDescending(o => o.Value)
                .Select(m =>
                {
                    if (m.Key == cmd)
                    {
                        result.SubTitle = string.Format(_context.API.GetTranslation("wox_plugin_cmd_cmd_has_been_executed_times"), m.Value);
                        return null;
                    }

                    var ret = new Result
                    {
                        Title = m.Key,
                        SubTitle = string.Format(_context.API.GetTranslation("wox_plugin_cmd_cmd_has_been_executed_times"), m.Value),
                        IcoPath = Image,
                        Action = c =>
                        {
                            Execute(Process.Start, PrepareProcessStartInfo(m.Key));
                            return true;
                        }
                    };
                    return ret;
                }).Where(o => o != null).Take(4);
            return history.ToList();
        }

        private Result GetCurrentCmd(string cmd)
        {
            var result = new Result
            {
                Title = cmd,
                Score = 5000,
                SubTitle = _context.API.GetTranslation("wox_plugin_cmd_execute_through_shell"),
                IcoPath = Image,
                Action = c =>
                {
                    Execute(Process.Start, PrepareProcessStartInfo(cmd));
                    return true;
                }
            };

            return result;
        }

        private List<Result> ResultsFromHistory()
        {
            var history = _settings.Count.OrderByDescending(o => o.Value)
                .Select(m => new Result
                {
                    Title = m.Key,
                    SubTitle = string.Format(_context.API.GetTranslation("wox_plugin_cmd_cmd_has_been_executed_times"), m.Value),
                    IcoPath = Image,
                    Action = c =>
                    {
                        Execute(Process.Start, PrepareProcessStartInfo(m.Key));
                        return true;
                    }
                }).Take(5);
            return history.ToList();
        }

        private ProcessStartInfo PrepareProcessStartInfo(string command, bool runAsAdministrator = false)
        {
            command = command.Trim();
            command = Environment.ExpandEnvironmentVariables(command);
            var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var runAsAdministratorArg = !runAsAdministrator && !_settings.RunAsAdministrator ? "" : "runas";

            ProcessStartInfo info;
            if (_settings.Shell == Shell.Cmd)
            {
                var arguments = _settings.LeaveShellOpen ? $"/k \"{command}\"" : $"/c \"{command}\" & pause";

                info = "cmd.exe".SetProcessStartInfo(workingDirectory, arguments, runAsAdministratorArg);
            }
            else if (_settings.Shell == Shell.Powershell)
            {
                string arguments;
                if (_settings.LeaveShellOpen)
                    arguments = $"-NoExit \"{command}\"";
                else
                    arguments = $"\"{command} ; Read-Host -Prompt \\\"Press Enter to continue\\\"\"";

                info = "powershell.exe".SetProcessStartInfo(workingDirectory, arguments, runAsAdministratorArg);
            }
            else if (_settings.Shell == Shell.RunCommand)
            {
                var parts = command.Split(new[] {' '}, 2);
                if (parts.Length == 2)
                {
                    var filename = parts[0];
                    if (ExistInPath(filename))
                    {
                        var arguments = parts[1];
                        info = filename.SetProcessStartInfo(workingDirectory, arguments, runAsAdministratorArg);
                    }
                    else
                    {
                        info = command.SetProcessStartInfo(verb: runAsAdministratorArg);
                    }
                }
                else
                {
                    info = command.SetProcessStartInfo(verb: runAsAdministratorArg);
                }
            }
            else if (_settings.Shell == Shell.Bash && _settings.SupportWSL)
            {
                string arguments;
                if (_settings.LeaveShellOpen)
                    // FIXME: How to deal with commands containing single quote?
                    arguments = $"-c \'{command} ; $SHELL\'";
                else
                    arguments = $"-c \'{command} ; echo -n Press any key to exit... ; read -n1\'";
                info = new ProcessStartInfo
                {
                    FileName = "bash.exe",
                    Arguments = arguments
                };
            }
            else
            {
                throw new NotImplementedException();
            }

            info.UseShellExecute = true;

            _settings.AddCmdHistory(command);

            return info;
        }

        private void Execute(Func<ProcessStartInfo, Process> startProcess, ProcessStartInfo info)
        {
            try
            {
                startProcess(info);
            }
            catch (FileNotFoundException e)
            {
                var name = "Plugin: Shell";
                var message = $"Command not found: {e.Message}";
                _context.API.ShowMsg(name, message);
            }
            catch (Win32Exception e)
            {
                var name = "Plugin: Shell";
                var message = $"Error running the command: {e.Message}";
                _context.API.ShowMsg(name, message);
            }
        }

        private bool ExistInPath(string filename)
        {
            if (File.Exists(filename)) return true;

            var values = Environment.GetEnvironmentVariable("PATH");
            if (values != null)
            {
                foreach (var path in values.Split(';'))
                {
                    var path1 = Path.Combine(path, filename);
                    var path2 = Path.Combine(path, filename + ".exe");
                    if (File.Exists(path1) || File.Exists(path2)) return true;
                }

                return false;
            }

            return false;
        }

        private bool API_GlobalKeyboardEvent(int keyEvent, int vkCode, SpecialKeyState state)
        {
            if (_settings.ReplaceWinR)
            {
                if (keyEvent == (int) KeyEvent.WM_KEYDOWN && vkCode == (int) Keys.R && state.WinPressed)
                {
                    _winRStroked = true;
                    OnWinRPressed();
                    return false;
                }

                if (keyEvent == (int) KeyEvent.WM_KEYUP && _winRStroked && vkCode == (int) Keys.LWin)
                {
                    _winRStroked = false;
                    _keyboardSimulator.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.CONTROL);
                    return false;
                }
            }

            return true;
        }

        private void OnWinRPressed()
        {
            _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeywords[0]}{Plugin.Query.TermSeparator}");
            Application.Current.MainWindow.Visibility = Visibility.Visible;
        }

        #endregion
    }
}