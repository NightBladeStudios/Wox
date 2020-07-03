namespace Wox.Infrastructure.Exception
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.Win32;
    using UserSettings;

    public class ExceptionFormatter
    {
        private static string _systemLanguage;
        private static string _woxLanguage;

        #region Public

        public static void Initialize(string systemLanguage, string woxLanguage)
        {
            _systemLanguage = systemLanguage;
            _woxLanguage = woxLanguage;
        }

        public static string FormattedException(Exception ex)
        {
            return FormattedAllExceptions(ex).ToString();
        }

        public static StringBuilder RuntimeInfoFull()
        {
            var sb = new StringBuilder();
            sb.AppendLine(RuntimeInfo());
            sb.AppendLine(SDKInfo());
            sb.Append(AssemblyInfo());
            return sb;
        }

        public static string RuntimeInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Runtime Info");
            sb.AppendLine($"* Command Line: {Environment.CommandLine}");
            sb.AppendLine($"* Portable Mode: {DataLocation.PortableDataLocationInUse()}");
            sb.AppendLine($"* Timestamp: {DateTime.Now.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine($"* Wox version: {Constant.Version}");
            sb.AppendLine($"* OS Version: {Environment.OSVersion.VersionString}");
            sb.AppendLine($"* x64 OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"* x64 Process: {Environment.Is64BitProcess}");
            sb.AppendLine($"* System Language: {_systemLanguage}");
            sb.AppendLine($"* Wox Language: {_woxLanguage}");
            sb.AppendLine($"* CLR Version: {Environment.Version}");
            sb.AppendLine("* Installed .NET Framework: ");
            foreach (var result in GetFrameworkVersionFromRegistry())
            {
                sb.Append("   * ");
                sb.AppendLine(result);
            }

            return sb.ToString();
        }

        public static string SDKInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## SDK Info");
            sb.AppendLine($"* Python Path: {Constant.PythonPath}");
            sb.AppendLine($"* Everything SDK Path: {Constant.EverythingSDKPath}");
            return sb.ToString();
        }

        public static string ExceptionWithRuntimeInfo(Exception ex, string id)
        {
            var sb = new StringBuilder();
            sb.Append("Error id: ");
            sb.AppendLine(id);
            var formatted = FormattedAllExceptions(ex);
            sb.Append(formatted);
            var info = RuntimeInfoFull();
            sb.Append(info);

            return sb.ToString();
        }

        #endregion

        #region Private

        private static StringBuilder FormattedAllExceptions(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Exception begin --------------------");
            var index = 1;
            FormattedSingleException(ex, sb, 1);
            ex = ex.InnerException;
            while (ex != null)
            {
                sb.Append(Indent(index));
                sb.Append("InnerException ");
                sb.Append(index);
                sb.AppendLine(": ------------------------------------------------------------");
                index = index + 1;
                FormattedSingleException(ex, sb, index);
                ex = ex.InnerException;
            }

            sb.AppendLine("Exception end ------------------------------------------------------------");
            sb.AppendLine();
            return sb;
        }

        private static string Indent(int indentLevel)
        {
            return new string(' ', indentLevel * 2);
        }

        private static void FormattedSingleException(Exception ex, StringBuilder sb, int indentLevel)
        {
            sb.Append(Indent(indentLevel));
            sb.Append(ex.GetType().FullName);
            sb.Append(": ");
            sb.AppendLine(ex.Message);
            sb.Append(Indent(indentLevel));
            sb.Append("HResult: ");
            sb.AppendLine(ex.HResult.ToString());
            foreach (var key in ex.Data.Keys)
            {
                var value = ex.Data[key];
                sb.Append(Indent(indentLevel));
                sb.Append("Data: <");
                sb.Append(key);
                sb.Append("> -> <");
                sb.Append(value);
                sb.AppendLine(">");
            }

            if (ex.Source != null)
            {
                sb.Append(Indent(indentLevel));
                sb.Append("Source: ");
                sb.AppendLine(ex.Source);
            }

            if (ex.TargetSite != null)
            {
                sb.Append(Indent(indentLevel));
                sb.Append("TargetAssembly: ");
                sb.AppendLine(ex.TargetSite.Module.Assembly.ToString());
                sb.Append(Indent(indentLevel));
                sb.Append("TargetModule: ");
                sb.AppendLine(ex.TargetSite.Module.ToString());
                sb.Append(Indent(indentLevel));
                sb.Append("TargetSite: ");
                sb.AppendLine(ex.TargetSite.ToString());
            }

            sb.Append(Indent(indentLevel));
            sb.AppendLine("StackTrace: --------------------");
            sb.AppendLine(ex.StackTrace);
        }

        private static string AssemblyInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine("## Assemblies - " + AppDomain.CurrentDomain.FriendlyName);
            sb.AppendLine();
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().OrderBy(o => o.GlobalAssemblyCache ? 50 : 0))
            {
                sb.Append("* ");
                sb.Append(ass.FullName);
                sb.Append(" (");

                if (ass.IsDynamic)
                    sb.Append("dynamic assembly doesn't has location");
                else if (string.IsNullOrEmpty(ass.Location))
                    sb.Append("location is null or empty");
                else
                    sb.Append(ass.Location);
                sb.AppendLine(")");
            }

            return sb.ToString();
        }


        // http://msdn.microsoft.com/en-us/library/hh925568%28v=vs.110%29.aspx
        private static List<string> GetFrameworkVersionFromRegistry()
        {
            try
            {
                var result = new List<string>();
                using (var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
                {
                    foreach (var versionKeyName in ndpKey.GetSubKeyNames())
                        if (versionKeyName.StartsWith("v"))
                        {
                            var versionKey = ndpKey.OpenSubKey(versionKeyName);
                            var name = (string) versionKey.GetValue("Version", "");
                            var sp = versionKey.GetValue("SP", "").ToString();
                            var install = versionKey.GetValue("Install", "").ToString();
                            if (install != "")
                                if (sp != "" && install == "1")
                                    result.Add(string.Format("{0} {1} SP{2}", versionKeyName, name, sp));
                                else
                                    result.Add(string.Format("{0} {1}", versionKeyName, name));

                            if (name != "") continue;
                            foreach (var subKeyName in versionKey.GetSubKeyNames())
                            {
                                var subKey = versionKey.OpenSubKey(subKeyName);
                                name = (string) subKey.GetValue("Version", "");
                                if (name != "")
                                    sp = subKey.GetValue("SP", "").ToString();
                                install = subKey.GetValue("Install", "").ToString();
                                if (install != "")
                                {
                                    if (sp != "" && install == "1")
                                        result.Add(string.Format("{0} {1} {2} SP{3}", versionKeyName, subKeyName, name, sp));
                                    else if (install == "1")
                                        result.Add(string.Format("{0} {1} {2}", versionKeyName, subKeyName, name));
                                }
                            }
                        }
                }

                using (var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                {
                    var releaseKey = (int) ndpKey.GetValue("Release");
                    {
                        if (releaseKey == 394806 || releaseKey == 394806) result.Add("v4.6.2");
                        if (releaseKey == 460798 || releaseKey == 460805) result.Add("v4.7");
                        if (releaseKey == 461308 || releaseKey == 461310) result.Add("v4.7.1");
                        if (releaseKey == 461808 || releaseKey == 461814) result.Add("v4.7.2");
                        if (releaseKey == 528040 || releaseKey == 528209 || releaseKey == 528049) result.Add("v4.8");
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        #endregion
    }
}