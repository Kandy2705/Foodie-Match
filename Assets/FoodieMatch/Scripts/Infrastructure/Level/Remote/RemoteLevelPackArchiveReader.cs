using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelPackArchiveReader
    {
        private const string ManifestFileName = "pack_manifest.json";

        public bool TryRead(
            byte[] archiveContent,
            out byte[] manifestContent,
            out IReadOnlyDictionary<string, byte[]> levelContents)
        {
            manifestContent = null;
            levelContents = null;

            try
            {
                using MemoryStream archiveStream = new(archiveContent);
                using ZipArchive archive = new(
                    archiveStream,
                    ZipArchiveMode.Read);
                Dictionary<string, byte[]> levels = new(
                    StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = archive.Entries[i];

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    string path = entry.FullName;

                    if (string.Equals(
                            path,
                            ManifestFileName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (manifestContent != null)
                        {
                            return false;
                        }

                        manifestContent = ReadEntry(entry);
                        continue;
                    }

                    if (!RemoteLevelPathValidator.IsSafeJsonPath(path) ||
                        !levels.TryAdd(path, ReadEntry(entry)))
                    {
                        return false;
                    }
                }

                levelContents = levels;
                return manifestContent != null;
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static byte[] ReadEntry(ZipArchiveEntry entry)
        {
            using Stream entryStream = entry.Open();
            using MemoryStream contentStream = new();
            entryStream.CopyTo(contentStream);
            return contentStream.ToArray();
        }
    }
}
