namespace Wox.Infrastructure.Storage
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Serialization.Formatters.Binary;
    using Logger;
    using NLog;
    using UserSettings;

    /// <summary>
    /// Stroage object using binary data
    /// Normally, it has better performance, but not readable
    /// </summary>
    public class BinaryStorage<T>
    {
        public string FilePath { get; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public BinaryStorage(string filename)
        {
            const string directoryName = "Cache";
            var directoryPath = Path.Combine(DataLocation.DataDirectory(), directoryName);
            Helper.ValidateDirectory(directoryPath);

            const string fileSuffix = ".cache";
            FilePath = Path.Combine(directoryPath, $"{filename}{fileSuffix}");
        }

        #region Public

        public T TryLoad(T defaultData)
        {
            if (File.Exists(FilePath))
            {
                if (new FileInfo(FilePath).Length == 0)
                {
                    Logger.WoxError($"Zero length cache file <{FilePath}>");
                    Save(defaultData);
                    return defaultData;
                }

                using (var stream = new FileStream(FilePath, FileMode.Open))
                {
                    var d = Deserialize(stream, defaultData);
                    return d;
                }
            }

            Logger.WoxInfo("Cache file not exist, load default data");
            Save(defaultData);
            return defaultData;
        }

        public void Save(T data)
        {
            using (var stream = new FileStream(FilePath, FileMode.Create))
            {
                var binaryFormatter = new BinaryFormatter
                {
                    AssemblyFormat = FormatterAssemblyStyle.Simple
                };

                try
                {
                    binaryFormatter.Serialize(stream, data);
                }
                catch (SerializationException e)
                {
                    Logger.WoxError($"serialize error for file <{FilePath}>", e);
                }
            }
        }

        #endregion

        #region Private

        private T Deserialize(FileStream stream, T defaultData)
        {
            //http://stackoverflow.com/questions/2120055/binaryformatter-deserialize-gives-serializationexception
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            var binaryFormatter = new BinaryFormatter
            {
                AssemblyFormat = FormatterAssemblyStyle.Simple
            };

            try
            {
                var t = ((T) binaryFormatter.Deserialize(stream)).NonNull();
                return t;
            }
            catch (Exception e)
            {
                Logger.WoxError($"Deserialize error for file <{FilePath}>", e);
                return defaultData;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly ayResult = null;
            var sShortAssemblyName = args.Name.Split(',')[0];
            var ayAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var ayAssembly in ayAssemblies)
                if (sShortAssemblyName == ayAssembly.FullName.Split(',')[0])
                {
                    ayResult = ayAssembly;
                    break;
                }

            return ayResult;
        }

        #endregion
    }
}