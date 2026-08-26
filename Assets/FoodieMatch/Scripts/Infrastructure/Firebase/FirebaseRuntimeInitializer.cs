using System.Threading.Tasks;
using Firebase;

namespace FoodieMatch.Infrastructure.Firebase
{
    public sealed class FirebaseRuntimeInitializer
    {
        private Task<DependencyStatus> _initializationTask;

        public Task<DependencyStatus> InitializeAsync()
        {
            return _initializationTask ??= InitializeFirebaseAsync();
        }

        private static async Task<DependencyStatus> InitializeFirebaseAsync()
        {
            FirebaseApp.LogLevel = LogLevel.Error;
            return await FirebaseApp.CheckAndFixDependenciesAsync();
        }
    }
}
