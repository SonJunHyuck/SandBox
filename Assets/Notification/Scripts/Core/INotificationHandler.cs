using System;

namespace Sandbox.Notification
{
    public interface INotificationHandler
    {
        Type RequestType { get; }
        void Handle(NotificationRequest request);
    }
}
