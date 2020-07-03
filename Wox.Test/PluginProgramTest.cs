namespace Wox.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Configuration;
    using Core.Plugin;
    using Infrastructure;
    using NUnit.Framework;
    using Plugin;
    using Plugin.Program;
    using ViewModel;
    using Settings = Infrastructure.UserSettings.Settings;

    [TestFixture]
    internal class PluginProgramTest
    {
        private Main plugin;

        [OneTimeSetUp]
        public void Setup()
        {
            Settings.Initialize();
            var portable = new Portable();
            var settingsVm = new SettingWindowViewModel(portable);
            var stringMatcher = new StringMatcher();
            StringMatcher.Instance = stringMatcher;
            stringMatcher.UserSettingSearchPrecision = Settings.Instance.QuerySearchPrecision;
            PluginManager.LoadPlugins(Settings.Instance.PluginSettings);
            var mainVm = new MainViewModel(false);
            var api = new PublicAPIInstance(settingsVm, mainVm);

            plugin = new Main();
            plugin.InitSync(new PluginInitContext
            {
                API = api
            });
        }

        //[TestCase("powershell", "Windows PowerShell")] skip for appveyror
        [TestCase("note", "Notepad")]
        [TestCase("computer", "computer")]
        public void Win32Test(string QueryText, string ResultTitle)
        {
            var query = QueryBuilder.Build(QueryText.Trim(), new Dictionary<string, PluginPair>());
            var result = plugin.Query(query).OrderByDescending(r => r.Score).First();
            Assert.IsTrue(result.Title.StartsWith(ResultTitle));
        }
    }
}