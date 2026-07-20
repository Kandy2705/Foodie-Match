using FoodieMatch.Core.Application.Events;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class ComboCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayEvents _gameplayEvents;
        private readonly GameplayAudioPresenter _audioPresenter;

        private GameplaySession _session;

        public ComboCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayEvents gameplayEvents,
            GameplayAudioPresenter audioPresenter)
        {
            _sessionGuard = sessionGuard;
            _gameplayEvents = gameplayEvents;
            _audioPresenter = audioPresenter;
        }

        public void BeginSession(GameplaySession session)
        {
            _session = session;
            session.Combo.Reset();
            PublishComboChanged(session);
        }

        public void EndSession()
        {
            _session = null;
        }

        public void RegisterPackageCompleted(GameplaySession session)
        {
            if (!CanUpdateCombo(session))
            {
                return;
            }

            session.Combo.RegisterPackageCompleted();
            _audioPresenter.PlayPackageCompleted(session.Combo.ComboCount);
            PublishComboChanged(session);
        }

        public void AdvanceTime(float elapsedSeconds)
        {
            GameplaySession session = _session;

            if (!CanUpdateCombo(session) || !session.Combo.IsActive)
            {
                return;
            }

            session.Combo.AdvanceTime(elapsedSeconds);

            if (!session.Combo.IsActive)
            {
                PublishComboChanged(session);
            }
        }

        private bool CanUpdateCombo(GameplaySession session)
        {
            return session != null &&
                   _session == session &&
                   _sessionGuard.IsCurrentSession(session.SessionId) &&
                   session.CanContinueGameplay;
        }

        private void PublishComboChanged(GameplaySession session)
        {
            _gameplayEvents.OnComboChanged(
                new ComboChangedEvent(session.Combo.ComboCount, session.Combo.RemainingSeconds));
        }
    }
}
