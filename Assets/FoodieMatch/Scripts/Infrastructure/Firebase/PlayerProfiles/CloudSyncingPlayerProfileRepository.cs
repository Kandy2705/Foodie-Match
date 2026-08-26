using System;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Player;

namespace FoodieMatch.Infrastructure.Firebase.PlayerProfiles
{
    public sealed class CloudSyncingPlayerProfileRepository :
        IPlayerProfileRepository
    {
        private readonly IPlayerProfileRepository _localRepository;
        private readonly PlayerProfileCloudSynchronizer _cloudSynchronizer;

        public CloudSyncingPlayerProfileRepository(
            IPlayerProfileRepository localRepository,
            PlayerProfileCloudSynchronizer cloudSynchronizer)
        {
            _localRepository = localRepository ??
                throw new ArgumentNullException(nameof(localRepository));
            _cloudSynchronizer = cloudSynchronizer ??
                throw new ArgumentNullException(nameof(cloudSynchronizer));
        }

        public Task<PlayerProfileLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            return _localRepository.LoadAsync(cancellationToken);
        }

        public async Task<PlayerProfileSaveResult> SaveAsync(
            PlayerProfile profile,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            PlayerProfileSaveResult result = await _localRepository.SaveAsync(
                profile,
                expectedRevision,
                cancellationToken);

            if (result.IsSuccess)
            {
                _cloudSynchronizer.QueueUpload(result.Record);
            }

            return result;
        }
    }
}
