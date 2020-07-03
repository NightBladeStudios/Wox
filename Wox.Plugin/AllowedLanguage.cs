namespace Wox.Plugin
{
    using System;

    public static class AllowedLanguage
    {
        public static string Python => "PYTHON";

        public static string CSharp => "CSHARP";

        public static string Executable => "EXECUTABLE";

        #region Public

        public static bool IsAllowed(string language)
        {
            return string.Equals(language, Python, StringComparison.CurrentCultureIgnoreCase)
                   || string.Equals(language, CSharp, StringComparison.CurrentCultureIgnoreCase)
                   || string.Equals(language, Executable, StringComparison.CurrentCultureIgnoreCase);
        }

        #endregion
    }
}