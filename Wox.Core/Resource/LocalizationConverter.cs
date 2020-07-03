namespace Wox.Core
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows.Data;

    public class LocalizationConverter : IValueConverter
    {
        #region Public

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(string) && value != null)
            {
                var fi = value.GetType().GetField(value.ToString());
                if (fi != null)
                {
                    var localizedDescription = string.Empty;
                    var attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (attributes.Length > 0 && !string.IsNullOrEmpty(attributes[0].Description)) localizedDescription = attributes[0].Description;

                    return !string.IsNullOrEmpty(localizedDescription) ? localizedDescription : value.ToString();
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}