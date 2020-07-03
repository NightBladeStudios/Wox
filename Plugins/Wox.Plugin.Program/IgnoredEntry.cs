namespace Wox.Plugin.Program
{
    public class IgnoredEntry
    {
        public string EntryString { get; set; }
        public bool IsRegex { get; set; }

        #region Public

        public override string ToString()
        {
            return string.Format("{0} {1}", EntryString, IsRegex ? "(regex)" : "");
        }

        #endregion
    }
}