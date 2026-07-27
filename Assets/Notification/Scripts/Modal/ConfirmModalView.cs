using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sandbox.Notification.UI;

namespace Sandbox.Notification
{
    public sealed class ConfirmModalView : NotificationView
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action confirmed;
        private Action cancelled;

        protected override void Awake()
        {
            base.Awake();
            confirmButton.onClick.AddListener(() => confirmed?.Invoke());
            cancelButton.onClick.AddListener(() => cancelled?.Invoke());
        }

        public void Bind(ConfirmModalRequest request, Action onConfirmed, Action onCancelled)
        {
            titleText.text = request.Title;
            descriptionText.text = request.Description;
            confirmed = onConfirmed;
            cancelled = onCancelled;
        }
    }
}
