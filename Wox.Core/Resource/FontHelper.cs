namespace Wox.Core.Resource
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;

    public static class FontHelper
    {
        private static readonly FontWeightConverter fontWeightConverter = new FontWeightConverter();

        private static readonly FontStyleConverter fontStyleConverter = new FontStyleConverter();

        private static readonly FontStretchConverter fontStretchConverter = new FontStretchConverter();

        #region Public

        public static FontWeight GetFontWeightFromInvariantStringOrNormal(string value)
        {
            if (value == null) return FontWeights.Normal;

            try
            {
                return (FontWeight) fontWeightConverter.ConvertFromInvariantString(value);
            }
            catch
            {
                return FontWeights.Normal;
            }
        }

        public static FontStyle GetFontStyleFromInvariantStringOrNormal(string value)
        {
            if (value == null) return FontStyles.Normal;

            try
            {
                return (FontStyle) fontStyleConverter.ConvertFromInvariantString(value);
            }
            catch
            {
                return FontStyles.Normal;
            }
        }

        public static FontStretch GetFontStretchFromInvariantStringOrNormal(string value)
        {
            if (value == null) return FontStretches.Normal;
            try
            {
                return (FontStretch) fontStretchConverter.ConvertFromInvariantString(value);
            }
            catch
            {
                return FontStretches.Normal;
            }
        }

        public static FamilyTypeface ChooseRegularFamilyTypeface(this FontFamily family)
        {
            return family.FamilyTypefaces.OrderBy(o =>
            {
                return Math.Abs(o.Stretch.ToOpenTypeStretch() - FontStretches.Normal.ToOpenTypeStretch()) * 100 +
                       Math.Abs(o.Weight.ToOpenTypeWeight() - FontWeights.Normal.ToOpenTypeWeight()) +
                       (o.Style == FontStyles.Normal ? 0 : o.Style == FontStyles.Oblique ? 1 : 2) * 1000;
            }).FirstOrDefault() ?? family.FamilyTypefaces.FirstOrDefault();
        }

        public static FamilyTypeface ConvertFromInvariantStringsOrNormal(this FontFamily family, string style, string weight, string stretch)
        {
            var styleObj = GetFontStyleFromInvariantStringOrNormal(style);
            var weightObj = GetFontWeightFromInvariantStringOrNormal(weight);
            var stretchObj = GetFontStretchFromInvariantStringOrNormal(stretch);
            return family.FamilyTypefaces.FirstOrDefault(o => o.Style == styleObj && o.Weight == weightObj && o.Stretch == stretchObj)
                   ?? family.ChooseRegularFamilyTypeface();
        }

        #endregion
    }
}