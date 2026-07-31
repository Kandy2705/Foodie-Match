using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FoodieMatch.Infrastructure.Level
{
    public sealed class LevelDiskCache
    {
        private const string StagingDirectoryName = "staging";

        private static readonly Encoding FileEncoding = new UTF8Encoding(false);

        private readonly string _rootDirectory;
        private readonly string _rootDirectoryPrefix;
        private readonly string _stagingDirectory;

        public LevelDiskCache(string rootDirectory)
        {
            _rootDirectory = Path.GetFullPath(rootDirectory);
            _rootDirectoryPrefix =
                _rootDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            _stagingDirectory = Path.Combine(
                _rootDirectory,
                StagingDirectoryName);
        }

        public bool TryReadFile(
            string relativePath,
            out string content)
        {
            string filePath = GetCachePath(relativePath);

            if (!File.Exists(filePath))
            {
                content = null;
                return false;
            }

            content = File.ReadAllText(filePath, FileEncoding);
            return true;
        }

        public bool TryReadBytes(
            string relativePath,
            out byte[] content)
        {
            string filePath = GetCachePath(relativePath);

            if (!File.Exists(filePath))
            {
                content = null;
                return false;
            }

            content = File.ReadAllBytes(filePath);
            return true;
        }

        public async Task<bool> WriteFileAtomicallyAsync(
            string relativePath,
            string content,
            Func<string, bool> isContentValid)
        {
            string targetPath = GetCachePath(relativePath);
            string stagedPath = CreateStagedFilePath();

            Directory.CreateDirectory(_stagingDirectory);

            try
            {
                await WriteStagedFileAsync(stagedPath, content);
                string stagedContent =
                    File.ReadAllText(stagedPath, FileEncoding);

                if (!isContentValid(stagedContent))
                {
                    return false;
                }

                ActivateStagedFile(stagedPath, targetPath);
                return true;
            }
            finally
            {
                if (File.Exists(stagedPath))
                {
                    File.Delete(stagedPath);
                }
            }
        }

        public async Task<bool> WriteDirectoryAtomicallyAsync(
            string relativePath,
            Func<string, Task> writeStagedDirectory,
            Func<string, bool> isDirectoryValid)
        {
            string targetPath = GetCachePath(relativePath);
            string stagedPath = CreateStagedDirectoryPath();

            Directory.CreateDirectory(stagedPath);

            try
            {
                await writeStagedDirectory(stagedPath);

                if (!isDirectoryValid(stagedPath))
                {
                    return false;
                }

                ActivateStagedDirectory(stagedPath, targetPath);
                return true;
            }
            finally
            {
                if (Directory.Exists(stagedPath))
                {
                    Directory.Delete(stagedPath, recursive: true);
                }
            }
        }

        public void DeleteSubdirectoriesExcept(
            string relativePath,
            string keptDirectoryName)
        {
            string parentPath = GetCachePath(relativePath);

            if (!Directory.Exists(parentPath))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(keptDirectoryName) ||
                keptDirectoryName.Contains(
                    Path.DirectorySeparatorChar.ToString()) ||
                keptDirectoryName.Contains(
                    Path.AltDirectorySeparatorChar.ToString()))
            {
                throw new ArgumentException(
                    "Kept directory name must be a single directory name.",
                    nameof(keptDirectoryName));
            }

            string[] directories = Directory.GetDirectories(parentPath);

            for (int i = 0; i < directories.Length; i++)
            {
                if (!string.Equals(
                        Path.GetFileName(directories[i]),
                        keptDirectoryName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Delete(directories[i], recursive: true);
                }
            }
        }

        public void ClearStaging()
        {
            if (Directory.Exists(_stagingDirectory))
            {
                Directory.Delete(_stagingDirectory, recursive: true);
            }
        }

        private string GetCachePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException(
                    "Cache path must be relative.",
                    nameof(relativePath));
            }

            string fullPath = Path.GetFullPath(
                Path.Combine(_rootDirectory, relativePath));

            if (!fullPath.StartsWith(
                    _rootDirectoryPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Cache path must stay inside the cache directory.",
                    nameof(relativePath));
            }

            return fullPath;
        }

        private string CreateStagedFilePath()
        {
            return Path.Combine(
                _stagingDirectory,
                $"{Guid.NewGuid():N}.tmp");
        }

        private string CreateStagedDirectoryPath()
        {
            return Path.Combine(
                _stagingDirectory,
                Guid.NewGuid().ToString("N"));
        }

        private static async Task WriteStagedFileAsync(
            string stagedPath,
            string content)
        {
            byte[] bytes = FileEncoding.GetBytes(content);

            using FileStream stream = new(
                stagedPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }

        private static void ActivateStagedFile(
            string stagedPath,
            string targetPath)
        {
            string targetDirectory = Path.GetDirectoryName(targetPath);
            Directory.CreateDirectory(targetDirectory);

            if (File.Exists(targetPath))
            {
                File.Replace(stagedPath, targetPath, null);
                return;
            }

            File.Move(stagedPath, targetPath);
        }

        private static void ActivateStagedDirectory(
            string stagedPath,
            string targetPath)
        {
            string targetParent = Path.GetDirectoryName(targetPath);
            Directory.CreateDirectory(targetParent);

            if (!Directory.Exists(targetPath))
            {
                Directory.Move(stagedPath, targetPath);
                return;
            }

            string backupPath = $"{targetPath}.{Guid.NewGuid():N}.backup";
            Directory.Move(targetPath, backupPath);

            try
            {
                Directory.Move(stagedPath, targetPath);
                Directory.Delete(backupPath, recursive: true);
            }
            catch
            {
                if (!Directory.Exists(targetPath))
                {
                    Directory.Move(backupPath, targetPath);
                }

                throw;
            }
        }
    }
}
