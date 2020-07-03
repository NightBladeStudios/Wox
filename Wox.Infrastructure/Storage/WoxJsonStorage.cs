namespace Wox.Infrastructure.Storage
{
    using System.IO;
    using UserSettings;

    public class WoxJsonStorage<T> : JsonStorage<T> where T : new()
    {
        public WoxJsonStorage()
        {
            var directoryPath = Path.Combine(DataLocation.DataDirectory(), DirectoryName);
            Helper.ValidateDirectory(directoryPath);

            var filename = typeof(T).Name;
            FilePath = Path.Combine(directoryPath, $"{filename}{FileSuffix}");
        }
    }
}