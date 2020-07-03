namespace Wox.Plugin.Shell
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Settings
    {
        public Shell Shell { get; set; } = Shell.Cmd;
        public bool ReplaceWinR { get; set; } = true;
        public bool LeaveShellOpen { get; set; }
        public bool RunAsAdministrator { get; set; } = true;
        public bool SupportWSL { get; }

        public Dictionary<string, int> Count = new Dictionary<string, int>();

        public Settings()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var wslRoot = localAppData + @"\lxss\rootfs";
                SupportWSL = Directory.Exists(wslRoot);
            }
            catch
            {
                SupportWSL = false;
            }
        }

        #region Public

        public void AddCmdHistory(string cmdName)
        {
            if (Count.ContainsKey(cmdName))
                Count[cmdName] += 1;
            else
                Count.Add(cmdName, 1);
        }

        #endregion
    }

    public enum Shell
    {
        Cmd = 0,
        Powershell = 1,
        RunCommand = 2,
        Bash = 3
    }
}