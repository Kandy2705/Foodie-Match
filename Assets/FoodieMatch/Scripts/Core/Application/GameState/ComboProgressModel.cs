using System;

namespace FoodieMatch.Core.Application.GameState
{
    public sealed class ComboProgressModel
    {
        private readonly float _comboDurationSeconds;
        private float _remainingSeconds;

        public ComboProgressModel(float comboDurationSeconds)
        {
            if (!IsValidDuration(comboDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(comboDurationSeconds));
            }

            _comboDurationSeconds = comboDurationSeconds;
        }

        public int ComboCount { get; private set; }

        public float RemainingSeconds => _remainingSeconds;

        public bool IsActive => ComboCount > 0 && _remainingSeconds > 0f;

        public void RegisterPackageCompleted()
        {
            ComboCount = IsActive ? ComboCount + 1 : 1;
            _remainingSeconds = _comboDurationSeconds;
        }

        public void AdvanceTime(float elapsedSeconds)
        {
            if (!IsValidElapsedTime(elapsedSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            }

            if (!IsActive || elapsedSeconds == 0f)
            {
                return;
            }

            _remainingSeconds = Math.Max(0f, _remainingSeconds - elapsedSeconds);

            if (_remainingSeconds == 0f)
            {
                Reset();
            }
        }

        public void Reset()
        {
            ComboCount = 0;
            _remainingSeconds = 0f;
        }

        private static bool IsValidDuration(float durationSeconds)
        {
            return durationSeconds > 0f && !float.IsNaN(durationSeconds) && !float.IsInfinity(durationSeconds);
        }

        private static bool IsValidElapsedTime(float elapsedSeconds)
        {
            return elapsedSeconds >= 0f && !float.IsNaN(elapsedSeconds) && !float.IsInfinity(elapsedSeconds);
        }
    }
}
