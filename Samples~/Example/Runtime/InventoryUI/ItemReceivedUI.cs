using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    public class ItemReceivedUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text amountText;

        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private float displayDuration = 1f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine disappearCoroutine;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
        }

        public void Show(ItemStack item)
        {

            icon.sprite = item.Data.Icon;
            itemName.text = item.Data.DisplayName;
            amountText.text = $"x{item.Amount}";

            if (disappearCoroutine != null)
                StopCoroutine(disappearCoroutine);

            canvasGroup.alpha = 1f;

            disappearCoroutine = StartCoroutine(DisappearCoroutine());
        }

        private IEnumerator DisappearCoroutine()
        {
            yield return new WaitForSeconds(displayDuration);

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;

                canvasGroup.alpha = Mathf.Lerp(
                    1f,
                    0f,
                    elapsed / fadeDuration
                );

                yield return null;
            }

            canvasGroup.alpha = 0f;

            disappearCoroutine = null;
        }
    }
}