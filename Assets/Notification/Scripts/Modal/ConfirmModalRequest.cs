namespace Sandbox.Notification
{
    public sealed class ConfirmModalRequest
    {
        public readonly string Title;
        public readonly string Description;

        public ConfirmModalRequest(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }
}
