namespace Wox.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Documents;
    using Core.Resource;

    public class HighlightTextConverter : IMultiValueConverter
    {
        private static HighlightStyle Default => ThemeManager.Instance.HighLightStyle;
        private static HighlightStyle Selected => ThemeManager.Instance.HighLightSelectedStyle;

        #region Public

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo cultureInfo)
        {
            var text = value[0] as string;
            var highlightData = value[1] as List<int>;
            var selected = value[2] as bool? == true;


            if (highlightData == null || !highlightData.Any())
                // No highlight data, just return the text
                return new Run(text);

            var style = selected ? Selected : Default;

            var textBlock = new Span();
            for (var i = 0; i < text.Length; i++)
            {
                var currentCharacter = text.Substring(i, 1);
                if (ShouldHighlight(highlightData, i))
                    textBlock.Inlines.Add(new Run(currentCharacter)
                    {
                        Foreground = style.Color,
                        FontWeight = style.FontWeight,
                        FontStyle = style.FontStyle,
                        FontStretch = style.FontStretch
                    });
                else
                    textBlock.Inlines.Add(new Run(currentCharacter));
            }

            return textBlock;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return new[] {DependencyProperty.UnsetValue, DependencyProperty.UnsetValue};
        }

        #endregion

        #region Private

        private bool ShouldHighlight(List<int> highlightData, int index)
        {
            return highlightData.Contains(index);
        }

        #endregion
    }
}