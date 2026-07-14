namespace FoodieMatch.Features.LevelSystem
{
    public sealed class GameplaySessionGuard
    {
        private int _currentSessionId;
        private bool _hasActiveSession;

        public int CurrentSessionId => _currentSessionId;

        public int BeginSession()
        {
            _currentSessionId++;
            _hasActiveSession = true;

            return _currentSessionId;
        }

        public void EndSession()
        {
            _hasActiveSession = false;
        }

        public bool IsCurrentSession(int sessionId)
        {
            return _hasActiveSession &&
                   sessionId == _currentSessionId;
        }
    }
}
