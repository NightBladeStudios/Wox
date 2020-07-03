﻿namespace Wox.Plugin.ControlPanel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using Infrastructure;
    using Infrastructure.Logger;
    using Microsoft.Win32;
    using NLog;

    //from:https://raw.githubusercontent.com/CoenraadS/Windows-Control-Panel-Items
    public static class ControlPanelList
    {
        private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        private const string CONTROL = @"%SystemRoot%\System32\control.exe";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private static readonly RegistryKey nameSpace = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace");
        private static readonly RegistryKey clsid = Registry.ClassesRoot.OpenSubKey("CLSID");


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);


        [DllImport("kernel32.dll")]
        private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        #region Public

        public static List<ControlPanelItem> Create()
        {
            RegistryKey currentKey;
            ProcessStartInfo executablePath;
            var controlPanelItems = new List<ControlPanelItem>();
            string localizedString;
            string infoTip;

            foreach (var guid in nameSpace.GetSubKeyNames())
                try
                {
                    currentKey = clsid.OpenSubKey(guid);
                    if (currentKey != null)
                    {
                        executablePath = getExecutablePath(currentKey);

                        if (!(executablePath == null)) //Cannot have item without executable path
                        {
                            localizedString = getLocalizedString(currentKey);

                            if (!string.IsNullOrEmpty(localizedString)) //Cannot have item without Title
                            {
                                infoTip = getInfoTip(currentKey);

                                string iconPath;
                                if (currentKey.OpenSubKey("DefaultIcon") != null && currentKey.OpenSubKey("DefaultIcon").GetValue(null) != null)
                                    iconPath = currentKey.OpenSubKey("DefaultIcon").GetValue(null).ToString();
                                else
                                    iconPath = Constant.ErrorIcon;
                                controlPanelItems.Add(new ControlPanelItem(localizedString, infoTip, guid, executablePath, iconPath));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    e.Data.Add(nameof(guid), guid);
                    Logger.WoxError($"cannot parse control panel item {guid}", e);
                }

            return controlPanelItems;
        }

        #endregion

        #region Private

        private static ProcessStartInfo getExecutablePath(RegistryKey currentKey)
        {
            var executablePath = new ProcessStartInfo();
            string applicationName;

            if (currentKey.GetValue("System.ApplicationName") != null)
            {
                //CPL Files (usually native MS items)
                applicationName = currentKey.GetValue("System.ApplicationName").ToString();
                executablePath.FileName = Environment.ExpandEnvironmentVariables(CONTROL);
                executablePath.Arguments = "-name " + applicationName;
            }
            else if (currentKey.OpenSubKey("Shell\\Open\\Command") != null && currentKey.OpenSubKey("Shell\\Open\\Command").GetValue(null) != null)
            {
                //Other files (usually third party items)
                var input = "\"" + Environment.ExpandEnvironmentVariables(currentKey.OpenSubKey("Shell\\Open\\Command").GetValue(null).ToString()) + "\"";
                executablePath.FileName = "cmd.exe";
                executablePath.Arguments = "/C " + input;
                executablePath.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else
            {
                return null;
            }

            return executablePath;
        }

        private static string getLocalizedString(RegistryKey currentKey)
        {
            IntPtr dataFilePointer;
            string[] localizedStringRaw;
            uint stringTableIndex;
            StringBuilder resource;
            string localizedString;

            if (currentKey.GetValue("LocalizedString") != null)
            {
                localizedStringRaw = currentKey.GetValue("LocalizedString").ToString().Split(new[] {",-"}, StringSplitOptions.None);

                if (localizedStringRaw.Length > 1)
                {
                    if (localizedStringRaw[0][0] == '@') localizedStringRaw[0] = localizedStringRaw[0].Substring(1);

                    localizedStringRaw[0] = Environment.ExpandEnvironmentVariables(localizedStringRaw[0]);

                    dataFilePointer = LoadLibraryEx(localizedStringRaw[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE); //Load file with strings

                    stringTableIndex = sanitizeUint(localizedStringRaw[1]);

                    resource = new StringBuilder(255);
                    LoadString(dataFilePointer, stringTableIndex, resource, resource.Capacity + 1); //Extract needed string
                    FreeLibrary(dataFilePointer);

                    localizedString = resource.ToString();

                    //Some apps don't return a string, although they do have a stringIndex. Use Default value.

                    if (string.IsNullOrEmpty(localizedString))
                    {
                        if (currentKey.GetValue(null) != null)
                            localizedString = currentKey.GetValue(null).ToString();
                        else
                            return null; //Cannot have item without title.
                    }
                }
                else
                {
                    localizedString = localizedStringRaw[0];
                }
            }
            else if (currentKey.GetValue(null) != null)
            {
                localizedString = currentKey.GetValue(null).ToString();
            }
            else
            {
                return null; //Cannot have item without title.
            }

            return localizedString;
        }

        private static string getInfoTip(RegistryKey currentKey)
        {
            IntPtr dataFilePointer;
            string[] infoTipRaw;
            uint stringTableIndex;
            StringBuilder resource;
            var infoTip = "";

            if (currentKey.GetValue("InfoTip") != null)
            {
                infoTipRaw = currentKey.GetValue("InfoTip").ToString().Split(new[] {",-"}, StringSplitOptions.None);

                if (infoTipRaw.Length == 2)
                {
                    if (infoTipRaw[0][0] == '@') infoTipRaw[0] = infoTipRaw[0].Substring(1);
                    infoTipRaw[0] = Environment.ExpandEnvironmentVariables(infoTipRaw[0]);

                    dataFilePointer = LoadLibraryEx(infoTipRaw[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE); //Load file with strings

                    stringTableIndex = sanitizeUint(infoTipRaw[1]);

                    resource = new StringBuilder(255);
                    LoadString(dataFilePointer, stringTableIndex, resource, resource.Capacity + 1); //Extract needed string
                    FreeLibrary(dataFilePointer);

                    infoTip = resource.ToString();
                }
                else
                {
                    infoTip = currentKey.GetValue("InfoTip").ToString();
                }
            }
            else
            {
                infoTip = "";
            }

            return infoTip;
        }

        private static uint sanitizeUint(string args) //Remove all chars before and after first set of digits.
        {
            var x = 0;

            while (x < args.Length && !char.IsDigit(args[x])) args = args.Substring(1);

            x = 0;

            while (x < args.Length && char.IsDigit(args[x])) x++;

            if (x < args.Length) args = args.Remove(x);

            /*If the logic is correct, this should never through an exception.
             * If there is an exception, then need to analyze what the input is.
             * Returning the wrong number will cause more errors */
            return Convert.ToUInt32(args);
        }

        private static bool IS_INTRESOURCE(IntPtr value)
        {
            if ((uint) value > ushort.MaxValue)
                return false;
            return true;
        }

        private static uint GET_RESOURCE_ID(IntPtr value)
        {
            if (IS_INTRESOURCE(value))
                return (uint) value;
            throw new NotSupportedException("value is not an ID!");
        }

        private static string GET_RESOURCE_NAME(IntPtr value)
        {
            if (IS_INTRESOURCE(value))
                return value.ToString();
            return Marshal.PtrToStringUni(value);
        }

        #endregion
    }
}