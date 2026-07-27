using TMPro;
using UnityEngine;
using Sandbox.Notification.UI;

namespace Sandbox.Notification
{
    public sealed class ItemToastView : NotificationView
    {
        [SerializeField] private TMP_Text itemText;

        public void Bind(ItemToastRequest request)
        {
            itemText.text = $"{request.DisplayName}   +{request.AddedAmount}  ({request.TotalAmount})";
        }
    }
}
