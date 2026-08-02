using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Repositories
{
    public interface ILevelRepository
    {
        Task<LevelDefinition> LoadLevelAsync(int levelNumber);
    }
}
