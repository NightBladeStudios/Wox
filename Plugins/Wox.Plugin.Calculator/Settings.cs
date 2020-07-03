namespace Wox.Plugin.Calculator
{
    public class Settings
    {
        public DecimalSeparator DecimalSeparator { get; set; } = DecimalSeparator.UseSystemLocale;
        public int MaxDecimalPlaces { get; set; } = 10;
    }
}