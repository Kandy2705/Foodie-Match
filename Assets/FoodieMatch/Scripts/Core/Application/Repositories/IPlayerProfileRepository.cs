using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Domain.Player;

namespace FoodieMatch.Core.Application.Repositories
{
    public interface IPlayerProfileRepository
    {
        Task<PlayerProfileLoadResult> LoadAsync(
            CancellationToken cancellationToken = default);

        Task<PlayerProfileSaveResult> SaveAsync(
            PlayerProfile profile,
            long expectedRevision,
            CancellationToken cancellationToken = default);
    }
}
