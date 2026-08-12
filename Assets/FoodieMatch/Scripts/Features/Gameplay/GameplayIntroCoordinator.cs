using System.Threading.Tasks;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Motion;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class GameplayIntroCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayAudioPresenter _audioPresenter;
        private readonly RequiredPackageGroupView _packageGroupView;
        private readonly WaitingRackView _waitingRackView;
        private readonly BoardLayoutView _boardLayoutView;

        private GameplaySession _session;

        public GameplayIntroCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayAudioPresenter audioPresenter,
            RequiredPackageGroupView packageGroupView,
            WaitingRackView waitingRackView,
            BoardLayoutView boardLayoutView)
        {
            _sessionGuard = sessionGuard;
            _audioPresenter = audioPresenter;
            _packageGroupView = packageGroupView;
            _waitingRackView = waitingRackView;
            _boardLayoutView = boardLayoutView;
        }

        public void BeginSession(GameplaySession session)
        {
            _session = session;
            _packageGroupView.PrepareInitialEnter();
            _waitingRackView.PrepareIntro();
            _boardLayoutView.PrepareGrillIntro();
        }

        public void EndSession()
        {
            _packageGroupView.StopInitialEnterMotion();
            _waitingRackView.StopIntroMotion();
            _boardLayoutView.StopGrillIntroMotion();
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
            Task<MotionResult> packageMotion =
                _packageGroupView.PlayInitialEnterAsync();
            Task<MotionResult> waitingRackMotion =
                _waitingRackView.PlayIntroAsync();
            Task<MotionResult> grillMotion =
                _boardLayoutView.PlayGrillIntroAsync();
            MotionResult[] results = await Task.WhenAll(
                packageMotion,
                waitingRackMotion,
                grillMotion);

            if (CanContinue(session))
            {
                _boardLayoutView.StartGrillMovement();
            }

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] != MotionResult.Completed)
                {
                    return results[i];
                }
            }

            return MotionResult.Completed;
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
