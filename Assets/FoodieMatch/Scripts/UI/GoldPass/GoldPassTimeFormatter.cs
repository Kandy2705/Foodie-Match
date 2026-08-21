namespace FoodieMatch.UI.GoldPass
{
    internal static class GoldPassTimeFormatter
    {
        public static string Format(int totalMinutes)
        {
            int days = totalMinutes / (24 * 60);
            int hours = totalMinutes / 60 % 24;
            int minutes = totalMinutes % 60;

            if (days > 0)
            {
                return $"{days}d {hours}h";
            }

            return hours > 0
                ? $"{hours}h {minutes}m"
                : $"{minutes}m";
        }
    }
}
