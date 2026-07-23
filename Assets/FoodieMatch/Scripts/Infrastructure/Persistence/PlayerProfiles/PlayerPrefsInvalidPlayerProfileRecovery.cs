using System;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Infrastructure.Persistence.Save;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles
{
    public sealed class PlayerPrefsInvalidPlayerProfileRecovery :
        IInvalidPlayerProfileRecovery
    {
        private readonly ISaveService _saveService;

        public PlayerPrefsInvalidPlayerProfileRecovery(ISaveService saveService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public bool TryBackupAndRemove(out string errorMessage)
        {
            try
            {
                if (!_saveService.HasKey(PlayerProfileSaveKeys.Profile))
                {
                    errorMessage = null;
                    return true;
                }

                string invalidProfile = _saveService.GetString(
                    PlayerProfileSaveKeys.Profile,
                    string.Empty);
                _saveService.SetString(
                    PlayerProfileSaveKeys.InvalidProfileBackup,
                    invalidProfile);
                _saveService.DeleteKey(PlayerProfileSaveKeys.Profile);
                _saveService.Save();
                errorMessage = null;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = $"Invalid player profile could not be backed up: " +
                               $"{exception.Message}";
                return false;
            }
        }
    }
}
