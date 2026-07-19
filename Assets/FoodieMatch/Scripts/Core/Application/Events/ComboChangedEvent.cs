namespace FoodieMatch.Core.Application.Events
{
    public readonly struct ComboChangedEvent
    {
        public int ComboCount { get; }

        public float RemainingSeconds { get; }

        public ComboChangedEvent(int comboCount, float remainingSeconds)
        {
            ComboCount = comboCount;
            RemainingSeconds = remainingSeconds;
        }
    }
}
