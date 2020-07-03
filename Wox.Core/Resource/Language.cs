namespace Wox.Core.Resource
{
    public class Language
    {
        /// <summary>
        /// E.g. En or Zh-CN
        /// </summary>
        public string LanguageCode { get; set; }

        public string Display { get; set; }

        public Language(string code, string display)
        {
            LanguageCode = code;
            Display = display;
        }
    }
}