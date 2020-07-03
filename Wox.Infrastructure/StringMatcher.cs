using static Wox.Infrastructure.StringMatcher;

namespace Wox.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Runtime.Caching;
    using NLog;

    public class StringMatcher
    {
        public enum SearchPrecisionScore
        {
            Regular = 50,
            Low = 20,
            None = 0
        }

        public SearchPrecisionScore UserSettingSearchPrecision { get; set; }

        public static StringMatcher Instance { get; internal set; }

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Alphabet _alphabet;
        private readonly MemoryCache _cache;

        public StringMatcher()
        {
            _alphabet = new Alphabet();
            _alphabet.Initialize();

            var config = new NameValueCollection();
            config.Add("pollingInterval", "00:05:00");
            config.Add("physicalMemoryLimitPercentage", "1");
            config.Add("cacheMemoryLimitMegabytes", "30");
            _cache = new MemoryCache("StringMatcherCache", config);
        }

        #region Public

        public static MatchResult FuzzySearch(string query, string stringToCompare)
        {
            return Instance.FuzzyMatch(query, stringToCompare);
        }

        public MatchResult FuzzyMatch(string query, string stringToCompare)
        {
            query = query.Trim();
            if (string.IsNullOrEmpty(stringToCompare) || string.IsNullOrEmpty(query)) return new MatchResult(false, UserSettingSearchPrecision);
            var queryWithoutCase = query.ToLower();
            var translated = _alphabet.Translate(stringToCompare);

            var key = $"{queryWithoutCase}|{translated}";
            var match = _cache[key] as MatchResult;
            if (match == null)
            {
                match = FuzzyMatchRecursive(
                    queryWithoutCase, translated, 0, 0, new List<int>()
                );
                var policy = new CacheItemPolicy();
                policy.SlidingExpiration = new TimeSpan(12, 0, 0);
                _cache.Set(key, match, policy);
            }

            return match;
        }

        public MatchResult FuzzyMatchRecursive(
            string query, string stringToCompare, int queryCurrentIndex, int stringCurrentIndex, List<int> sourceMatchData
        )
        {
            if (queryCurrentIndex == query.Length || stringCurrentIndex == stringToCompare.Length) return new MatchResult(false, UserSettingSearchPrecision);

            var recursiveMatch = false;
            var bestRecursiveMatchData = new List<int>();
            var bestRecursiveScore = 0;

            var matches = new List<int>();
            if (sourceMatchData.Count > 0)
                foreach (var data in sourceMatchData)
                    matches.Add(data);

            while (queryCurrentIndex < query.Length && stringCurrentIndex < stringToCompare.Length)
            {
                var queryLower = char.ToLower(query[queryCurrentIndex]);
                var stringToCompareLower = char.ToLower(stringToCompare[stringCurrentIndex]);
                if (queryLower == stringToCompareLower)
                {
                    var match = FuzzyMatchRecursive(
                        query, stringToCompare, queryCurrentIndex, stringCurrentIndex + 1, matches
                    );

                    if (match.Success)
                    {
                        if (!recursiveMatch || match.RawScore > bestRecursiveScore)
                        {
                            bestRecursiveMatchData = new List<int>();
                            foreach (var data in match.MatchData) bestRecursiveMatchData.Add(data);
                            bestRecursiveScore = match.Score;
                        }

                        recursiveMatch = true;
                    }

                    matches.Add(stringCurrentIndex);
                    queryCurrentIndex += 1;
                }

                stringCurrentIndex += 1;
            }

            var matched = queryCurrentIndex == query.Length;
            int outScore;
            if (matched)
            {
                outScore = 100;
                var penalty = 3 * matches[0];
                outScore = outScore - penalty;

                var unmatched = stringToCompare.Length - matches.Count;
                outScore = outScore - 5 * unmatched;

                var consecutiveMatch = 0;
                for (var i = 0; i < matches.Count; i++)
                {
                    var currentIndex = matches[i];
                    if (i > 0)
                    {
                        var indexPrevious = matches[i - 1];
                        if (currentIndex == indexPrevious + 1)
                        {
                            consecutiveMatch += 1;
                            outScore += 10 * consecutiveMatch;
                        }
                        else
                        {
                            consecutiveMatch = 0;
                        }
                    }

                    var current = stringToCompare[currentIndex];
                    var currentUpper = char.IsUpper(current);
                    if (currentIndex > 0)
                    {
                        var neighbor = stringToCompare[currentIndex - 1];
                        if (currentUpper && char.IsLower(neighbor)) outScore += 30;

                        var isNeighborSeparator = neighbor == '_' || neighbor == ' ';
                        if (isNeighborSeparator)
                        {
                            outScore += 50;
                            if (currentUpper) outScore += 50;
                        }
                    }
                    else
                    {
                        outScore += 50;
                        if (currentUpper) outScore += 50;
                    }
                }
            }
            else
            {
                outScore = 0;
            }

            if (recursiveMatch && (!matched || bestRecursiveScore > outScore))
            {
                matches = new List<int>();
                foreach (var data in bestRecursiveMatchData) matches.Add(data);
                outScore = bestRecursiveScore;
                return new MatchResult(true, UserSettingSearchPrecision, matches, outScore);
            }

            if (matched)
                return new MatchResult(true, UserSettingSearchPrecision, matches, outScore);
            return new MatchResult(false, UserSettingSearchPrecision);
        }

        #endregion
    }

    public class MatchResult
    {
        public bool Success { get; set; }

        /// <summary>
        /// The final score of the match result with search precision filters applied.
        /// </summary>
        public int Score { get; private set; }

        public int RawScore
        {
            get => _rawScore;
            set
            {
                _rawScore = value;
                Score = ScoreAfterSearchPrecisionFilter(_rawScore);
            }
        }

        /// <summary>
        /// Matched data to highlight.
        /// </summary>
        public List<int> MatchData { get; set; }

        public SearchPrecisionScore SearchPrecision { get; set; }

        /// <summary>
        /// The raw calculated search score without any search precision filtering applied.
        /// </summary>
        private int _rawScore;

        public MatchResult(bool success, SearchPrecisionScore searchPrecision)
        {
            Success = success;
            SearchPrecision = searchPrecision;
        }

        public MatchResult(bool success, SearchPrecisionScore searchPrecision, List<int> matchData, int rawScore)
        {
            Success = success;
            SearchPrecision = searchPrecision;
            MatchData = matchData;
            RawScore = rawScore;
        }

        #region Public

        public bool IsSearchPrecisionScoreMet()
        {
            return IsSearchPrecisionScoreMet(_rawScore);
        }

        #endregion

        #region Private

        private bool IsSearchPrecisionScoreMet(int rawScore)
        {
            return rawScore >= (int) SearchPrecision;
        }

        private int ScoreAfterSearchPrecisionFilter(int rawScore)
        {
            return IsSearchPrecisionScoreMet(rawScore) ? rawScore : 0;
        }

        #endregion
    }
}