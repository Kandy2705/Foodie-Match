namespace FoodieMatch.Core.Application.Player
{
    public enum PlayerProfileLoadStatus
    {
        Success = 0,
        NotFound = 1,
        InvalidData = 2,
        UnsupportedVersion = 3,
        Failed = 4
    }
}
