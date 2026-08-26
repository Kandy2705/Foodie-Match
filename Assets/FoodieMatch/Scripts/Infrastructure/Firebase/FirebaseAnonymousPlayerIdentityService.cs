using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using FoodieMatch.Core.Application.Authentication;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Firebase
{
    public sealed class FirebaseAnonymousPlayerIdentityService :
        IPlayerIdentityService
    {
        private readonly FirebaseRuntimeInitializer _runtimeInitializer;

        private FirebaseAuth _auth;

        public FirebaseAnonymousPlayerIdentityService(
            FirebaseRuntimeInitializer runtimeInitializer)
        {
            _runtimeInitializer = runtimeInitializer;
        }

        public string PlayerId => _auth?.CurrentUser?.UserId;

        public bool IsAuthenticated => _auth?.CurrentUser != null;

        public async Task<bool> AuthenticateAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                DependencyStatus dependencyStatus =
                    await _runtimeInitializer.InitializeAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (dependencyStatus != DependencyStatus.Available)
                {
                    return false;
                }

                _auth = FirebaseAuth.DefaultInstance;

                if (_auth.CurrentUser != null)
                {
                    return true;
                }

                await _auth.SignInAnonymouslyAsync();
                cancellationToken.ThrowIfCancellationRequested();
                return _auth.CurrentUser != null;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Firebase Anonymous Auth failed: {exception.Message}");
                return false;
            }
        }
    }
}
