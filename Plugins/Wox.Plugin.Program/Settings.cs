namespace Wox.Plugin.Program
{
    using System;
    using System.Collections.Generic;

    public class Settings
    {
        public DateTime LastIndexTime { get; set; }
        public List<ProgramSource> ProgramSources { get; set; } = new List<ProgramSource>();
        public List<IgnoredEntry> IgnoredSequence { get; set; } = new List<IgnoredEntry>();
        public string[] ProgramSuffixes { get; set; } = {"bat", "appref-ms", "exe", "lnk"};

        public bool EnableStartMenuSource { get; set; } = true;

        public bool EnableRegistrySource { get; set; } = false;

        internal const char SuffixSeparator = ';';
    }
}