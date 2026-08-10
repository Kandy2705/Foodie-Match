using System.Threading.Tasks;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Motion;
using FoodieMatch.Features.RequiredPackage;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class GameplayIntroCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayAudioPresenter _audioPresenter;
        private readonly RequiredPackageGroupView _packageGroupView;
        private readonly BoardLayoutView _boardLayoutView;

        private GameplaySession _session;

        public GameplayIntroCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayAudioPresenter audioPresenter,
            RequiredPackageGroupView packageGroupView,
            BoardLayoutView boardLayoutView)
        {
            _sessionGuard = sessionGuard;
            _audioPresenter = audioPresenter;
            _packageGroupView = packageGroupView;
            _boardLayoutView = boardLayoutView;
        }

        public void BeginSession(GameplaySession session)
        {
            _session = session;
        }

        public void EndSession()
        {
            _packageGroupView.StopInitialEnterMotion();
            _session = null;
        }

        public async Task<MotionResult> PlayAsync(
            GameplaySession session)
        {
            if (!CanContinue(session))
            {
                return MotionResult.Cancelled;
            }

            _audioPresenter.PlayPackageEntering();
            MotionResult result =
                await _packageGroupView.PlayInitialEnterAsync();

            if (CanContinue(session))
            {
                _boardLayoutView.StartGrillMovement();
            }

            return result;
        }

        private bool CanContinue(GameplaySession session)
        {
            return session != null &&
                   session == _session &&
                   session.CanContinueGameplay &&
                   _sessionGuard.IsCurrentSession(session.SessionId);
        }
    }
}
