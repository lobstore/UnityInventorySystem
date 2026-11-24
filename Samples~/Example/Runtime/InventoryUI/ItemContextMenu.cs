using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames
{
    /// <summary>Reusable context menu that pools buttons for a list of inventory actions.</summary>
    public class ItemContextMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform buttonsRoot;
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private Button backdropButton;

        private readonly List<Button> pool = new List<Button>();

        private void Awake()
        {
            ConfigureBackdrop();
            if (panel != null) panel.gameObject.SetActive(false);
        }

        /// <summary>Assigns references for a context menu generated at runtime.</summary>
        public void Setup(RectTransform menuPanel, RectTransform menuButtonsRoot, Button menuButtonPrefab, Button menuBackdropButton)
        {
            if (backdropButton != null)
                backdropButton.onClick.RemoveListener(Hide);

            panel = menuPanel;
            buttonsRoot = menuButtonsRoot;
            buttonPrefab = menuButtonPrefab;
            backdropButton = menuBackdropButton;

            ConfigureBackdrop();
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void ConfigureBackdrop()
        {
            if (backdropButton == null) return;
            backdropButton.onClick.RemoveListener(Hide);
            backdropButton.onClick.AddListener(Hide);
            backdropButton.gameObject.SetActive(false);
        }

        /// <summary>Displays the supplied actions near a screen-space pointer position.</summary>
        public void Show(Vector2 screenPosition, IReadOnlyList<ContextMenuAction> actions)
        {
            if (panel == null || buttonsRoot == null || buttonPrefab == null || actions == null || actions.Count == 0)
                return;

            for (var i = actions.Count; i < pool.Count; i++)
                if (pool[i] != null) pool[i].gameObject.SetActive(false);

            for (var i = 0; i < actions.Count; i++)
            {
                var button = GetOrCreate(i);
                if (button == null) continue;

                var action = actions[i];
                button.gameObject.SetActive(true);
                button.interactable = action.Interactable;

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = action.Label;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    Hide();
                    action.Callback?.Invoke();
                });
            }

            if (backdropButton != null) backdropButton.gameObject.SetActive(true);
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            if (backdropButton != null) backdropButton.transform.SetAsFirstSibling();

            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            panel.pivot = new Vector2(0f, 1f);

            var size = panel.rect.size;
            var x = Mathf.Clamp(screenPosition.x, 0f, Mathf.Max(0f, Screen.width - size.x));
            var y = Mathf.Clamp(screenPosition.y, size.y, Screen.height);
            panel.position = new Vector3(x, y, 0f);
        }

        /// <summary>Hides the menu and its backdrop.</summary>
        public void Hide()
        {
            if (panel != null) panel.gameObject.SetActive(false);
            if (backdropButton != null) backdropButton.gameObject.SetActive(false);
        }

        private Button GetOrCreate(int index)
        {
            while (pool.Count <= index)
            {
                var clone = Instantiate(buttonPrefab, buttonsRoot);
                if (clone == null) return null;
                clone.gameObject.SetActive(false);
                pool.Add(clone);
            }

            return pool[index];
        }

        private void OnDestroy()
        {
            if (backdropButton != null)
                backdropButton.onClick.RemoveListener(Hide);
        }
    }
}
