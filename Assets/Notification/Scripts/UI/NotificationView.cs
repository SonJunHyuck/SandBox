using System;
using UnityEngine;

namespace Sandbox.Notification.UI
{
    public abstract class NotificationView : MonoBehaviour
    {
        [SerializeField] private NotificationShowHideEffect showHideEffect;

        protected virtual void Awake()
        {
            if (showHideEffect == null)
                showHideEffect = GetComponent<NotificationShowHideEffect>();
        }

        public void Show() => showHideEffect.PlayShow();
        public void Hide(Action completed) => showHideEffect.PlayHide(completed);
        public void HideImmediately() => showHideEffect.HideImmediately();
    }
}
