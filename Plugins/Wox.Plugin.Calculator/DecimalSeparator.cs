namespace Wox.Plugin.Calculator
{
    using System.ComponentModel;
    using Core;

    [TypeConverter(typeof(LocalizationConverter))]
    public enum DecimalSeparator
    {
        [LocalizedDescription("wox_plugin_calculator_decimal_seperator_use_system_locale")]
        UseSystemLocale,

        [LocalizedDescription("wox_plugin_calculator_decimal_seperator_dot")]
        Dot,

        [LocalizedDescription("wox_plugin_calculator_decimal_seperator_comma")]
        Comma
    }
}