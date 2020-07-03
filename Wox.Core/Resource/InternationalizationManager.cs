namespace Wox.Core.Resource
{
    public static class InternationalizationManager
    {
        public static Internationalization Instance
        {
            get
            {
                if (instance == null)
                    lock (syncObject)
                    {
                        if (instance == null) instance = new Internationalization();
                    }

                return instance;
            }
        }

        private static Internationalization instance;
        private static readonly object syncObject = new object();
    }
}