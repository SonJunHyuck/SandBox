using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sandbox.Notification
{
    /// <summary>Uses a fixed number of reusable item-toast slots and queues overflow pickups.</summary>
    public sealed class ItemToastPresenter : INotificationHandler
    {
        private readonly MonoBehaviour host;
        private readonly Transform container;
        private readonly float duration;
        private readonly ItemToastView prefab;
        private readonly Queue<ItemToastView> available = new Queue<ItemToastView>();
        private readonly HashSet<ItemToastView> active = new HashSet<ItemToastView>();
        private readonly Queue<ItemToastRequest> pending = new Queue<ItemToastRequest>();

        public Type RequestType => typeof(ItemToastRequest);

        public ItemToastPresenter(MonoBehaviour host, Transform container, float duration,
            ItemToastView prefab, int maximumVisible)
        {
            this.host = host;
            this.container = container;
            this.duration = duration;
            this.prefab = prefab;

            for (var i = 0; i < Mathf.Max(1, maximumVisible); i++)
            {
                var view = UnityEngine.Object.Instantiate(prefab, container);
                view.HideImmediately();
                available.Enqueue(view);
            }
        }

        public void Handle(NotificationRequest request) => Show((ItemToastRequest)request);

        public void Show(ItemToastRequest request)
        {
            if (available.Count == 0)
            {
                pending.Enqueue(request);
                return;
            }

            ShowNext(available.Dequeue(), request);
        }

        private void ShowNext(ItemToastView view, ItemToastRequest request)
        {
            active.Add(view);
            view.Bind(request);
            view.Show();
            host.StartCoroutine(Expire(view));
        }

        private IEnumerator Expire(ItemToastView view)
        {
            yield return new WaitForSecondsRealtime(duration);
            var hidden = false;
            view.Hide(() => hidden = true);
            yield return new WaitUntil(() => hidden);

            active.Remove(view);
            available.Enqueue(view);
            if (pending.Count > 0)
                Show(pending.Dequeue());
        }
    }
}
