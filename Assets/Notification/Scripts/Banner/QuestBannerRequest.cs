namespace Sandbox.Notification
{
    public sealed class QuestBannerRequest : NotificationRequest
    {
        public readonly string QuestId;
        public readonly string Title;
        public readonly string Description;

        public QuestBannerRequest(string questId, string title, string description)
        {
            QuestId = questId;
            Title = title;
            Description = description;
        }
    }
}
