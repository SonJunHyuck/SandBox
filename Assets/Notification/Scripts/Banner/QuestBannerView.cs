using TMPro;
using UnityEngine;
using Sandbox.Notification.UI;

namespace Sandbox.Notification
{
    public sealed class QuestBannerView : NotificationView
    {
        [SerializeField] private TMP_Text bannerText;

        public void Bind(QuestBannerRequest request)
        {
            bannerText.text = $"QUEST UPDATED\n<size=24>{request.Title}</size>\n<size=17>{request.Description}</size>";
        }
    }
}
