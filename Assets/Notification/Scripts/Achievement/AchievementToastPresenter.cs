using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sandbox.Notification
{
    /// <summary>Deduplicates achievements, then shows them with one retained queued view.</summary>
    public sealed class AchievementToastPresenter : INotificationHandler
    {
        private readonly MonoBehaviour host;
        private readonly AchievementToastView view;
        private readonly HashSet<string> shown = new HashSet<string>();
        private readonly Queue<AchievementToastRequest> queue = new Queue<AchievementToastRequest>();
        private bool showing;

        public Type RequestType => typeof(AchievementToastRequest);

        public AchievementToastPresenter(MonoBehaviour host, RectTransform area, AchievementToastView prefab)
        {
            this.host = host;
            view = UnityEngine.Object.Instantiate(prefab, area);
            view.HideImmediately();
        }

        public void Handle(NotificationRequest request)
        {
            var achievement = (AchievementToastRequest)request;
            if (!shown.Add(achievement.AchievementId))
                return;

            queue.Enqueue(achievement);
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
                yield return new WaitForSecondsRealtime(4f);

                var hidden = false;
                view.Hide(() => hidden = true);
                yield return new WaitUntil(() => hidden);
            }
            showing = false;
        }
    }
}
