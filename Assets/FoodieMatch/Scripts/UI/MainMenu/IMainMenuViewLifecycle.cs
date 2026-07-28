namespace FoodieMatch.UI.MainMenu
{
    public interface IMainMenuViewLifecycle
    {
        void Clear();
    }

    public interface IMainMenuTabSelectionHandler
    {
        void OnTabSelected();
    }
}
