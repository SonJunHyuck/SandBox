using TMPro;
using UnityEngine;
using Sandbox.Notification.UI;

namespace Sandbox.Notification
{
    public sealed class WarningView : NotificationView
    {
        [SerializeField] private TMP_Text messageText;

        public void Bind(WarningRequest request)
        {
            messageText.text = $"⚠  {request.Message}";
        }
    }
}
