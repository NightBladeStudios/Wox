namespace Wox.Plugin.Program
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Markup;

    public class SuffixesConvert : MarkupExtension, IValueConverter
    {
        #region Public

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string[];
            if (text != null)
                return string.Join(";", text);
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        #endregion
    }
}