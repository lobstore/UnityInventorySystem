using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames
{
    public class ItemContextMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform buttonsRoot;
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private Button backdropButton;

        private readonly List<Button> _pool = new();

        private void Awake()
        {
            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(Hide);
                backdropButton.gameObject.SetActive(false);
            }
            if (panel != null) panel.gameObject.SetActive(false);
        }

        /// <summary>Инициализация для рантайм-создаваемого меню.</summary>
        public void Setup(RectTransform panel, RectTransform buttonsRoot, Button buttonPrefab, Button backdropButton)
        {
            this.panel = panel;
            this.buttonsRoot = buttonsRoot;
            this.buttonPrefab = buttonPrefab;
            this.backdropButton = backdropButton;

            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(Hide);
                backdropButton.gameObject.SetActive(false);
            }
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public void Show(Vector2 screenPos, IReadOnlyList<ContextMenuAction> actions)
        {
            if (panel == null || actions == null || actions.Count == 0) return;

            for (int i = actions.Count; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);

            for (int i = 0; i < actions.Count; i++)
            {
                var btn = GetOrCreate(i);
                var action = actions[i];

                btn.gameObject.SetActive(true);
                btn.interactable = action.Interactable;

                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = action.Label;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
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
            float x = Mathf.Clamp(screenPos.x, 0f, Screen.width - size.x);
            float y = Mathf.Clamp(screenPos.y, size.y, Screen.height);
            panel.position = new Vector3(x, y, 0f);
        }

        public void Hide()
        {
            if (panel == null || !panel.gameObject.activeSelf) return;
            panel.gameObject.SetActive(false);
            if (backdropButton != null) backdropButton.gameObject.SetActive(false);
        }

        private Button GetOrCreate(int index)
        {
            while (_pool.Count <= index)
            {
                var clone = Instantiate(buttonPrefab, buttonsRoot);
                clone.gameObject.SetActive(false);
                _pool.Add(clone);
            }
            return _pool[index];
        }
    }
}
