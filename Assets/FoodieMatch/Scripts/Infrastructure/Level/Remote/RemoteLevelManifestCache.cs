using System.Threading.Tasks;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelManifestCache
    {
        private const string ManifestCachePath =
            "manifest/manifest.json";

        private readonly LevelDiskCache _diskCache;
        private readonly RemoteLevelManifestParser _parser = new();
        private readonly RemoteLevelManifestValidator _validator = new();

        public RemoteLevelManifestCache(LevelDiskCache diskCache)
        {
            _diskCache = diskCache;
        }

        public bool TryLoad(out RemoteLevelManifestDto manifest)
        {
            if (!_diskCache.TryReadFile(
                    ManifestCachePath,
                    out string content))
            {
                manifest = null;
                return false;
            }

            return TryParseAndValidate(
                content,
                expectedManifestVersion: null,
                out manifest);
        }

        public Task<bool> WriteAtomicallyAsync(
            string content,
            int? expectedManifestVersion)
        {
            return _diskCache.WriteFileAtomicallyAsync(
                ManifestCachePath,
                content,
                stagedContent => TryParseAndValidate(
                    stagedContent,
                    expectedManifestVersion,
                    out _));
        }

        private bool TryParseAndValidate(
            string content,
            int? expectedManifestVersion,
            out RemoteLevelManifestDto manifest)
        {
            return _parser.TryParse(content, out manifest) &&
                   _validator.IsValid(
                       manifest,
                       expectedManifestVersion);
        }
    }
}
