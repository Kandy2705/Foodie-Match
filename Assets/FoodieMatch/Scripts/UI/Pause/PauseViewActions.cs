using System;

namespace FoodieMatch.UI.Pause
{
    public sealed class PauseViewActions
    {
        public PauseViewActions(
            Action resumeClicked,
            Action restartClicked,
            Action homeClicked,
            Action closeClicked)
        {
            ResumeClicked = resumeClicked ?? throw new ArgumentNullException(nameof(resumeClicked));
            RestartClicked = restartClicked ?? throw new ArgumentNullException(nameof(restartClicked));
            HomeClicked = homeClicked ?? throw new ArgumentNullException(nameof(homeClicked));
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
        }

        public Action ResumeClicked { get; }

        public Action RestartClicked { get; }

        public Action HomeClicked { get; }

        public Action CloseClicked { get; }
    }
}
