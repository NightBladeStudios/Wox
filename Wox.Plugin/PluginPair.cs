﻿namespace Wox.Plugin
{
    public class PluginPair
    {
        public IPlugin Plugin { get; internal set; }
        public PluginMetadata Metadata { get; internal set; }

        #region Public

        public override string ToString()
        {
            return Metadata.Name;
        }

        public override bool Equals(object obj)
        {
            var r = obj as PluginPair;
            if (r != null)
                return string.Equals(r.Metadata.ID, Metadata.ID);
            return false;
        }

        public override int GetHashCode()
        {
            var hashcode = Metadata.ID?.GetHashCode() ?? 0;
            return hashcode;
        }

        #endregion
    }
}