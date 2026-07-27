using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sandbox.Notification
{
    /// <summary>One retained banner view, displaying queued requests sequentially.</summary>
    public sealed class QuestBannerPresenter : INotificationHandler
    {
        private readonly MonoBehaviour host;
        private readonly QuestBannerView view;
        private readonly Queue<QuestBannerRequest> queue = new Queue<QuestBannerRequest>();
        private bool showing;

        public Type RequestType => typeof(QuestBannerRequest);

        public QuestBannerPresenter(MonoBehaviour host, RectTransform area, QuestBannerView prefab)
        {
            this.host = host;
            view = UnityEngine.Object.Instantiate(prefab, area);
            view.HideImmediately();
        }

        public void Handle(NotificationRequest request)
        {
            queue.Enqueue((QuestBannerRequest)request);
            if (!showing)
                host.StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            showing = true;
            while (queue.Count > 0)
            {
                view.Bind(queue.Dequeue());
                view.Show();
                yield return new WaitForSecondsRealtime(3f);

                var hidden = false;
                view.Hide(() => hidden = true);
                yield return new WaitUntil(() => hidden);
            }
            showing = false;
        }
    }
}
