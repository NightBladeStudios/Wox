namespace Wox.Infrastructure.Storage
{
    using System;
    using System.Globalization;
    using System.IO;
    using Logger;
    using Newtonsoft.Json;
    using NLog;

    /// <summary>
    /// Serialize object using json format.
    /// </summary>
    public class JsonStorage<T>
    {
        // need a new directory name
        public const string DirectoryName = "Settings";
        public const string FileSuffix = ".json";
        public string FilePath { get; set; }
        public string DirectoryPath { get; set; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly JsonSerializerSettings _serializerSettings;
        private T _data;


        internal JsonStorage()
        {
            // use property initialization instead of DefaultValueAttribute
            // easier and flexible for default value of object
            _serializerSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        #region Public

        public T Load()
        {
            if (File.Exists(FilePath))
            {
                var serialized = File.ReadAllText(FilePath);
                if (!string.IsNullOrWhiteSpace(serialized))
                    Deserialize(serialized);
                else
                    LoadDefault();
            }
            else
            {
                LoadDefault();
            }

            return _data.NonNull();
        }

        public void Save()
        {
            var serialized = JsonConvert.SerializeObject(_data, Formatting.Indented);
            File.WriteAllText(FilePath, serialized);
        }

        #endregion

        #region Private

        private void Deserialize(string serialized)
        {
            try
            {
                _data = JsonConvert.DeserializeObject<T>(serialized, _serializerSettings);
            }
            catch (JsonException e)
            {
                LoadDefault();
                Logger.WoxError($"Deserialize error for json <{FilePath}>", e);
            }

            if (_data == null) LoadDefault();
        }

        private void LoadDefault()
        {
            if (File.Exists(FilePath)) BackupOriginFile();

            _data = JsonConvert.DeserializeObject<T>("{}", _serializerSettings);
            Save();
        }

        private void BackupOriginFile()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffffff", CultureInfo.CurrentUICulture);
            var directory = Path.GetDirectoryName(FilePath).NonNull();
            var originName = Path.GetFileNameWithoutExtension(FilePath);
            var backupName = $"{originName}-{timestamp}{FileSuffix}";
            var backupPath = Path.Combine(directory, backupName);
            File.Copy(FilePath, backupPath, true);
            // todo give user notification for the backup process
        }

        #endregion
    }
}