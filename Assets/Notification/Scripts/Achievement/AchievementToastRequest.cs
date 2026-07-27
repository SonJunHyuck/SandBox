namespace Sandbox.Notification
{
    public sealed class AchievementToastRequest : NotificationRequest
    {
        public readonly string AchievementId;
        public readonly string Header;
        public readonly string Title;
        public readonly string Description;

        public AchievementToastRequest(string achievementId, string header, string title, string description)
        {
            AchievementId = achievementId;
            Header = header;
            Title = title;
            Description = description;
        }
    }
}
