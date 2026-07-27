using System;
using System.Collections;
using UnityEngine;

namespace Sandbox.Notification.UI
{
    /// <summary>A tiny unscaled fade-and-scale effect, intentionally dependency-free.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class NotificationShowHideEffect : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float showDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float hideDuration = 0.12f;
        [SerializeField, UnityEngine.Range(0.5f, 1f)] private float hiddenScale = 0.92f;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 shownScale;
        private Coroutine routine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            shownScale = rectTransform.localScale;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void PlayShow()
        {
            gameObject.SetActive(true);
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Animate(0f, 1f, hiddenScale, 1f, showDuration, null));
        }

        public void PlayHide(Action completed)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Animate(canvasGroup.alpha, 0f,
                rectTransform.localScale.x / shownScale.x, hiddenScale, hideDuration, completed));
        }

        public void HideImmediately()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
            canvasGroup.alpha = 0f;
            rectTransform.localScale = shownScale * hiddenScale;
            gameObject.SetActive(false);
        }

        private IEnumerator Animate(float fromAlpha, float toAlpha, float fromScale, float toScale,
            float duration, Action completed)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                rectTransform.localScale = shownScale * Mathf.Lerp(fromScale, toScale, t);
                yield return null;
            }

            canvasGroup.alpha = toAlpha;
            rectTransform.localScale = shownScale * toScale;
            routine = null;
            if (toAlpha <= 0f)
                gameObject.SetActive(false);
            completed?.Invoke();
        }
    }
}
