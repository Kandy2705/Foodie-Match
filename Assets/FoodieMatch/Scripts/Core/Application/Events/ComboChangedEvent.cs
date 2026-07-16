namespace FoodieMatch.Core.Application.Events
{
    public readonly struct ComboChangedEvent
    {
        public int ComboCount { get; }

        public float FillNormalized { get; }

        public bool IsActive { get; }

        public ComboChangedEvent(
            int comboCount,
            float fillNormalized,
            bool isActive)
        {
            ComboCount = comboCount;
            FillNormalized = fillNormalized;
            IsActive = isActive;
        }
    }
}
