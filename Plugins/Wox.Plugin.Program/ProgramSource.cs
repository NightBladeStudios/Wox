namespace Wox.Plugin.Program
{
    public class ProgramSource
    {
        public string Location { get; set; }

        #region Public

        public override bool Equals(object obj)
        {
            var s = obj as ProgramSource;
            return Location.Equals(s?.Location);
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode();
        }

        #endregion
    }
}