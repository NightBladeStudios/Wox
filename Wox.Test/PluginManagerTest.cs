namespace Wox.Test
{
    using System.Linq;
    using Core.Configuration;
    using Core.Plugin;
    using Image;
    using Infrastructure;
    using Infrastructure.UserSettings;
    using NUnit.Framework;
    using ViewModel;

    [TestFixture]
    internal class PluginManagerTest
    {
        [OneTimeSetUp]
        public void setUp()
        {
            // todo remove i18n from application / ui, so it can be tested in a modular way
            new App();
            Settings.Initialize();
            ImageLoader.Initialize();

            var portable = new Portable();
            var settingsVm = new SettingWindowViewModel(portable);

            var stringMatcher = new StringMatcher();
            StringMatcher.Instance = stringMatcher;
            stringMatcher.UserSettingSearchPrecision = Settings.Instance.QuerySearchPrecision;

            PluginManager.LoadPlugins(Settings.Instance.PluginSettings);
            var mainVm = new MainViewModel(false);
            var api = new PublicAPIInstance(settingsVm, mainVm);
            PluginManager.InitializePlugins(api);
        }

        [TestCase("setting", "Settings")]
        [TestCase("netwo", "Network and Sharing Center")]
        public void BuiltinQueryTest(string QueryText, string ResultTitle)
        {
            var query = QueryBuilder.Build(QueryText.Trim(), PluginManager.NonGlobalPlugins);
            var plugins = PluginManager.AllPlugins;
            var result = plugins.SelectMany(
                    p => PluginManager.QueryForPlugin(p, query)
                )
                .OrderByDescending(r => r.Score)
                .First();

            Assert.IsTrue(result.Title.StartsWith(ResultTitle));
        }
    }
}