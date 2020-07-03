namespace Wox.Infrastructure.UI
{
    using System;
    using System.Windows.Markup;

    public class EnumBindingSourceExtension : MarkupExtension
    {
        public Type EnumType
        {
            get => _enumType;
            set
            {
                if (value != _enumType)
                {
                    if (value != null)
                    {
                        var enumType = Nullable.GetUnderlyingType(value) ?? value;
                        if (!enumType.IsEnum) throw new ArgumentException("Type must represent an enum.");
                    }

                    _enumType = value;
                }
            }
        }

        private Type _enumType;

        public EnumBindingSourceExtension()
        {
        }

        public EnumBindingSourceExtension(Type enumType)
        {
            EnumType = enumType;
        }

        #region Public

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_enumType == null) throw new InvalidOperationException("The EnumType must be specified.");

            var actualEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
            var enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == _enumType) return enumValues;

            var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }

        #endregion
    }
}