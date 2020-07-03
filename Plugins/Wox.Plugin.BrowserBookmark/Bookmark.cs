//using BinaryAnalysis.UnidecodeSharp;

namespace Wox.Plugin.BrowserBookmark
{
    using System;
    using System.Collections.Generic;

    public class Bookmark : IEquatable<Bookmark>, IEqualityComparer<Bookmark>
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
            //PinyinName = m_Name.Unidecode();
        }

        public string PinyinName { get; private set; }
        public string Url { get; set; }
        public string Source { get; set; }
        public int Score { get; set; }
        private string m_Name;

        #region Public

        /* TODO: since Source maybe unimportant, we just need to compare Name and Url */
        public bool Equals(Bookmark other)
        {
            return Equals(this, other);
        }

        public bool Equals(Bookmark x, Bookmark y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            return x.Name == y.Name && x.Url == y.Url;
        }

        public int GetHashCode(Bookmark bookmark)
        {
            if (ReferenceEquals(bookmark, null)) return 0;
            var hashName = bookmark.Name == null ? 0 : bookmark.Name.GetHashCode();
            var hashUrl = bookmark.Url == null ? 0 : bookmark.Url.GetHashCode();
            return hashName ^ hashUrl;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        #endregion
    }
}