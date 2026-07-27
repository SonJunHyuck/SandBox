using System;
using System.Collections;
using UnityEngine;

namespace Sandbox.Notification
{
    /// <summary>One retained view; each request replaces the previous warning immediately.</summary>
    public sealed class WarningPresenter : INotificationHandler
    {
        private readonly MonoBehaviour host;
        private readonly WarningView view;
        private Coroutine timer;

        public Type RequestType => typeof(WarningRequest);

        public WarningPresenter(MonoBehaviour host, RectTransform area, WarningView prefab)
        {
            this.host = host;
            view = UnityEngine.Object.Instantiate(prefab, area);
            view.HideImmediately();
        }

        public void Handle(NotificationRequest request)
        {
            var warning = (WarningRequest)request;
            if (timer != null)
                host.StopCoroutine(timer);

            view.Bind(warning);
            view.Show();
            timer = host.StartCoroutine(HideAfter(warning.Duration));
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            view.Hide(() => timer = null);
        }
    }
}
