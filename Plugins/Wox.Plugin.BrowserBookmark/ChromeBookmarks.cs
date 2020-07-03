namespace Wox.Plugin.BrowserBookmark
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public class ChromeBookmarks
    {
        private readonly List<Bookmark> bookmarks = new List<Bookmark>();

        #region Public

        public List<Bookmark> GetBookmarks()
        {
            LoadChromeBookmarks();
            return bookmarks;
        }

        #endregion

        #region Private

        private IEnumerable<JObject> GetNestedChildren(JObject jo)
        {
            var nested = new List<JObject>();
            var children = (JArray) jo["children"];
            foreach (JObject c in children)
            {
                var type = c["type"].ToString();
                if (type == "folder")
                {
                    var nc = GetNestedChildren(c);
                    nested.AddRange(nc);
                }
                else if (type == "url")
                {
                    nested.Add(c);
                }
            }

            return nested;
        }


        private void ParseChromeBookmarks(string path, string source)
        {
            if (!File.Exists(path)) return;
            var all = File.ReadAllText(path);
            var json = JObject.Parse(all);
            var items = (JObject) json["roots"]["bookmark_bar"];
            var flatted = GetNestedChildren(items);
            var bs = from item in flatted
                select new Bookmark
                {
                    Name = (string) item["name"],
                    Url = (string) item["url"],
                    Source = source
                };
            var filtered = bs.Where(b =>
            {
                var c = !b.Url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) &&
                        !b.Url.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase);
                return c;
            });
            bookmarks.AddRange(filtered);
        }

        private void LoadChromeBookmarks(string path, string name)
        {
            if (!Directory.Exists(path)) return;
            var paths = Directory.GetDirectories(path);

            foreach (var profile in paths)
                if (File.Exists(Path.Combine(profile, "Bookmarks")))
                    ParseChromeBookmarks(Path.Combine(profile, "Bookmarks"), name + (Path.GetFileName(profile) == "Default" ? "" : " (" + Path.GetFileName(profile) + ")"));
        }

        private void LoadChromeBookmarks()
        {
            var platformPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LoadChromeBookmarks(Path.Combine(platformPath, @"Google\Chrome\User Data"), "Google Chrome");
            LoadChromeBookmarks(Path.Combine(platformPath, @"Google\Chrome SxS\User Data"), "Google Chrome Canary");
            LoadChromeBookmarks(Path.Combine(platformPath, @"Chromium\User Data"), "Chromium");
        }

        #endregion
    }
}