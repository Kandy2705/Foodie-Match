namespace FoodieMatch.Shared.Pooling
{
    public interface IPoolLifecycle
    {
        void Initialize();

        void Clear();
    }
}
