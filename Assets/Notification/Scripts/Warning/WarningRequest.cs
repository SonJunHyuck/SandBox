namespace Sandbox.Notification
{
    public sealed class WarningRequest : NotificationRequest
    {
        public readonly string Message;
        public readonly float Duration;

        public WarningRequest(string message, float duration)
        {
            Message = message;
            Duration = duration;
        }
    }
}
