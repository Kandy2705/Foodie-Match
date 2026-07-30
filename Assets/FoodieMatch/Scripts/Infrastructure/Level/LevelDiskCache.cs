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
    }
}
