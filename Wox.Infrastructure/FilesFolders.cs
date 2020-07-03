namespace Wox.Infrastructure
{
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using Logger;
    using NLog;

    public static class FilesFolders
    {
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        #region Public

        public static void Copy(this string sourcePath, string targetPath)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourcePath);

            if (!dir.Exists)
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourcePath);

            try
            {
                var dirs = dir.GetDirectories();
                // If the destination directory doesn't exist, create it.
                if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

                // Get the files in the directory and copy them to the new location.
                var files = dir.GetFiles();
                foreach (var file in files)
                {
                    var path = Path.Combine(targetPath, file.Name);
                    file.CopyTo(path, false);
                }

                // Recursively copy subdirectories by calling itself on each subdirectory until there are no more to copy
                foreach (var subDirInfo in dirs)
                {
                    var path = Path.Combine(targetPath, subDirInfo.Name);
                    Copy(subDirInfo.FullName, path);
                }
            }
            catch (System.Exception e)
            {
                var message = $"Copying path {targetPath} has failed, it will now be deleted for consistency";
                Logger.WoxError(message, e);
                MessageBox.Show(message);
                RemoveFolderIfExists(targetPath);
            }
        }

        public static bool VerifyBothFolderFilesEqual(this string fromPath, string toPath)
        {
            try
            {
                var fromDir = new DirectoryInfo(fromPath);
                var toDir = new DirectoryInfo(toPath);

                if (fromDir.GetFiles("*", SearchOption.AllDirectories).Length != toDir.GetFiles("*", SearchOption.AllDirectories).Length)
                    return false;

                if (fromDir.GetDirectories("*", SearchOption.AllDirectories).Length != toDir.GetDirectories("*", SearchOption.AllDirectories).Length)
                    return false;

                return true;
            }
            catch (System.Exception e)
            {
                var message = $"Unable to verify folders and files between {fromPath} and {toPath}";
                Logger.WoxError(message, e);
                MessageBox.Show(message);
                return false;
            }
        }

        public static void RemoveFolderIfExists(this string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (System.Exception e)
            {
                var message = $"Not able to delete folder {(object) path}, please go to the location and manually delete it";
                Logger.WoxError(message, e);
                MessageBox.Show(message);
            }
        }

        public static bool LocationExists(this string path)
        {
            return Directory.Exists(path);
        }

        public static bool FileExits(this string filePath)
        {
            return File.Exists(filePath);
        }

        public static void OpenLocationInExporter(string location)
        {
            try
            {
                if (LocationExists(location))
                    Process.Start(location);
            }
            catch (System.Exception e)
            {
                var message = $"Unable to open location {(object) location}, please check if it exists";
                Logger.WoxError(message, e);
                MessageBox.Show(message);
            }
        }

        #endregion
    }
}