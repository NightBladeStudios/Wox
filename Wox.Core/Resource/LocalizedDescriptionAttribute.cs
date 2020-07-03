namespace Wox.Core
{
    using System.ComponentModel;
    using Resource;

    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        public override string Description
        {
            get
            {
                var description = _translator.GetTranslation(_resourceKey);
                return string.IsNullOrWhiteSpace(description) ? string.Format("[[{0}]]", _resourceKey) : description;
            }
        }

        private readonly string _resourceKey;
        private readonly Internationalization _translator;

        public LocalizedDescriptionAttribute(string resourceKey)
        {
            _translator = InternationalizationManager.Instance;
            _resourceKey = resourceKey;
        }
    }
}