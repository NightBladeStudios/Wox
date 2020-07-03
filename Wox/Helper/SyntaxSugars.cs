namespace Wox.Helper
{
    using System;

    public static class SyntaxSugars
    {
        #region Public

        public static TResult CallOrRescueDefault<TResult>(Func<TResult> callback)
        {
            return CallOrRescueDefault(callback, default);
        }

        public static TResult CallOrRescueDefault<TResult>(Func<TResult> callback, TResult def)
        {
            try
            {
                return callback();
            }
            catch
            {
                return def;
            }
        }

        #endregion
    }
}