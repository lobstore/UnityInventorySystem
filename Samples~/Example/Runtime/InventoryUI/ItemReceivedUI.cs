using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    /// <summary>Transient notification that fades an acquired item stack out after a short delay.</summary>
    public class ItemReceivedUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float displayDuration = 1f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

        private Coroutine disappearCoroutine;

        private void Awake()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        /// <summary>Displays the supplied non-empty stack and restarts the hide timer.</summary>
        public void Show(ItemStack item)
        {
            if (item.IsEmpty) return;

            if (icon != null) icon.sprite = item.Data.Icon;
            if (itemName != null) itemName.text = item.Data.DisplayName;
            if (amountText != null) amountText.text = $"x{item.Amount}";
            if (canvasGroup == null) return;

            if (disappearCoroutine != null)
                StopCoroutine(disappearCoroutine);

            canvasGroup.alpha = 1f;
            disappearCoroutine = StartCoroutine(DisappearCoroutine());
        }

        private IEnumerator DisappearCoroutine()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, displayDuration));

            var duration = Mathf.Max(0f, fadeDuration);
            if (duration <= 0f)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                disappearCoroutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            disappearCoroutine = null;
        }

        private void OnDisable()
        {
            if (disappearCoroutine != null)
            {
                StopCoroutine(disappearCoroutine);
                disappearCoroutine = null;
            }
        }
    }
}
