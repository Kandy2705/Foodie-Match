using System.Threading;
using System.Threading.Tasks;

namespace FoodieMatch.Core.Application.Authentication
{
    public interface IPlayerIdentityService
    {
        string PlayerId { get; }

        bool IsAuthenticated { get; }

        Task<bool> AuthenticateAsync(
            CancellationToken cancellationToken);
    }
}
