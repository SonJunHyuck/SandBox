using System;
using System.Collections.Generic;

namespace Sandbox.Notification
{
    /// <summary>Routes requests by type. It has no knowledge of presenter state or layer blocking.</summary>
    public sealed class NotificationService
    {
        private readonly Dictionary<Type, INotificationHandler> handlers = new Dictionary<Type, INotificationHandler>();

        public NotificationService(params INotificationHandler[] registrations)
        {
            foreach (var handler in registrations)
                handlers.Add(handler.RequestType, handler);
        }

        public void Publish(NotificationRequest request)
        {
            if (!handlers.TryGetValue(request.GetType(), out var handler))
                throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");

            handler.Handle(request);
        }
    }
}
