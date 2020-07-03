namespace Wox.Infrastructure
{
    using System;
    using System.Collections.Specialized;
    using System.Runtime.Caching;
    using NLog;
    using ToolGood.Words;
    using UserSettings;

    public class Alphabet
    {
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
        private MemoryCache _cache;
        private Settings _settings;

        #region Public

        public void Initialize()
        {
            _settings = Settings.Instance;
            var config = new NameValueCollection();
            config.Add("pollingInterval", "00:05:00");
            config.Add("physicalMemoryLimitPercentage", "1");
            config.Add("cacheMemoryLimitMegabytes", "30");
            _cache = new MemoryCache("AlphabetCache", config);
        }

        public string Translate(string content)
        {
            if (_settings.ShouldUsePinyin)
            {
                var result = _cache[content] as string;
                if (result == null)
                {
                    if (WordsHelper.HasChinese(content))
                        result = WordsHelper.GetFirstPinyin(content);
                    else
                        result = content;
                    var policy = new CacheItemPolicy();
                    policy.SlidingExpiration = new TimeSpan(12, 0, 0);
                    _cache.Set(content, result, policy);
                }

                return result;
            }

            return content;
        }

        #endregion
    }
}