namespace Wox.Infrastructure.Storage
{
    using System.IO;
    using UserSettings;

    public class PluginJsonStorage<T> : JsonStorage<T> where T : new()
    {
        public PluginJsonStorage()
        {
            // C# releated, add python releated below
            var dataType = typeof(T);
            var assemblyName = typeof(T).Assembly.GetName().Name;
            DirectoryPath = Path.Combine(DataLocation.DataDirectory(), DirectoryName, Constant.Plugins, assemblyName);
            Helper.ValidateDirectory(DirectoryPath);

            FilePath = Path.Combine(DirectoryPath, $"{dataType.Name}{FileSuffix}");
        }
    }
}