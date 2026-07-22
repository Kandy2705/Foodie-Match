namespace FoodieMatch.Core.Application.Repositories
{
    public interface IInvalidPlayerProfileRecovery
    {
        bool TryBackupAndRemove(out string errorMessage);
    }
}
