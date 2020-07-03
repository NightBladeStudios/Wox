namespace Wox.Core.Resource
{
    public class ThemeManager
    {
        public static Theme Instance
        {
            get
            {
                if (instance == null)
                    lock (syncObject)
                    {
                        if (instance == null) instance = new Theme();
                    }

                return instance;
            }
        }

        private static Theme instance;
        private static readonly object syncObject = new object();
    }
}