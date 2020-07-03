namespace Wox.Plugin.Everything.Everything
{
    using System.Collections.Generic;

    public class SearchResult
    {
        public string FileName { get; set; }
        public List<int> FileNameHighlightData { get; set; }
        public string FullPath { get; set; }
        public List<int> FullPathHighlightData { get; set; }
        public ResultType Type { get; set; }
    }
}