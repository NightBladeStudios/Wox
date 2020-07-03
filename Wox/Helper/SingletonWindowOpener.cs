namespace Wox.Helper
{
    using System;
    using System.Linq;
    using System.Windows;

    public static class SingletonWindowOpener
    {
        #region Public

        public static T Open<T>(params object[] args) where T : Window
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.GetType() == typeof(T))
                         ?? (T) Activator.CreateInstance(typeof(T), args);
            Application.Current.MainWindow.Hide();
            window.Show();
            window.Focus();

            return (T) window;
        }

        #endregion
    }
}