using TMPro;
using UnityEngine;
using Sandbox.Notification.UI;

namespace Sandbox.Notification
{
    public sealed class AchievementToastView : NotificationView
    {
        [SerializeField] private TMP_Text achievementText;

        public void Bind(AchievementToastRequest request)
        {
            achievementText.text = $"{request.Header}\n<size=27>{request.Title}</size>\n<size=17>{request.Description}</size>";
        }
    }
}
