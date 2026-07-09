using System;

namespace FoodieMatch.UI.Setting
{
    public sealed class SettingPopupViewActions
    {
        public SettingPopupViewActions(
            Action closeClicked,
            Action<bool> soundChanged,
            Action<bool> musicChanged)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
            SoundChanged = soundChanged ?? throw new ArgumentNullException(nameof(soundChanged));
            MusicChanged = musicChanged ?? throw new ArgumentNullException(nameof(musicChanged));
        }

        public Action CloseClicked { get; }

        public Action<bool> SoundChanged { get; }

        public Action<bool> MusicChanged { get; }
    }
}
