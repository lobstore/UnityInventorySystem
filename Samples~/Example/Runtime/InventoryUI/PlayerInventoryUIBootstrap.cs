using LOGIYGames.Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    /// <summary>
    /// Connects the active character's InventoryController to the sample inventory UI.
    /// Character and GameManager references are project-level integration points.
    /// </summary>
    public class PlayerInventoryUIBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject inventorySlotPrefab;
        [SerializeField] private RectTransform inventoryContainer;
        [SerializeField] private CanvasGroup group;

        [Header("Drag ghost (optional - auto-created if null)")]
        [SerializeField] private Image dragGhost;
        [SerializeField] private TMP_Text dragGhostAmount;

        [Header("Context menu (optional - auto-created if null)")]
        [SerializeField] private ItemContextMenu contextMenu;

        [SerializeField] private ItemReceivedUI itemReceivedUI;

        private GameObject runtimeUIRoot;
        private InventoryUIFactory factory;
        private InventoryController currentInventory;

        /// <summary>Whether the inventory panel is currently visible and interactive.</summary>
        public bool IsInventoryOpened { get; private set; }

        private void Awake()
        {
            EnsureRuntimeUI();
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.OnCharacterChanged.AddListener(UpdateInventoryUI);
        }

        private void EnsureRuntimeUI()
        {
            var needGhost = dragGhost == null;
            var needMenu = contextMenu == null;
            if (!needGhost && !needMenu) return;

            var canvas = inventoryContainer != null
                ? inventoryContainer.GetComponentInParent<Canvas>()
                : GetComponentInParent<Canvas>();
            var uiParent = canvas != null ? canvas.rootCanvas.transform : transform;

            runtimeUIRoot = new GameObject("InventoryRuntimeUI", typeof(RectTransform));
            runtimeUIRoot.transform.SetParent(uiParent, false);
            var rect = runtimeUIRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (needGhost)
                dragGhost = RuntimeInventoryUIBuilder.CreateDragGhost(runtimeUIRoot.transform, out dragGhostAmount);
            if (needMenu)
                contextMenu = RuntimeInventoryUIBuilder.CreateContextMenu(runtimeUIRoot.transform);

            if (needGhost && !needMenu) dragGhost.transform.SetAsFirstSibling();
            if (!needGhost && needMenu) contextMenu.transform.SetAsLastSibling();
        }

        private void UpdateInventoryUI(Character.Actor newCharacter)
        {
            if (currentInventory != null)
                currentInventory.OnItemAdded.RemoveListener(ShowReceivedItem);

            factory?.Dispose();
            factory = null;

            currentInventory = newCharacter != null
                ? newCharacter.GetComponent<InventoryController>()
                : null;
            if (currentInventory == null) return;

            currentInventory.OnItemAdded.AddListener(ShowReceivedItem);

            if (inventoryContainer == null || inventorySlotPrefab == null)
            {
                Debug.LogWarning("Inventory UI requires an inventory container and slot prefab.", this);
                return;
            }

            factory = new InventoryUIFactory(currentInventory, inventoryContainer, inventorySlotPrefab,
                dragGhost, dragGhostAmount, contextMenu);
            factory.Create();
        }

        private void ShowReceivedItem(ItemStack item)
        {
            if (itemReceivedUI != null) itemReceivedUI.Show(item);
        }

        private void Update()
        {
            // UIInput is intentionally not null-compared: its project-defined type may be a struct.
            if (GameManager.Instance != null && GameManager.Instance.UIInput.InventoryPressed)
                IsInventoryOpened = !IsInventoryOpened;

            if (group != null)
            {
                group.alpha = IsInventoryOpened ? 1f : 0f;
                group.interactable = IsInventoryOpened;
                group.blocksRaycasts = IsInventoryOpened;
            }

            if (!IsInventoryOpened) contextMenu?.Hide();
        }

        private void OnDestroy()
        {
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.OnCharacterChanged.RemoveListener(UpdateInventoryUI);
            if (currentInventory != null)
                currentInventory.OnItemAdded.RemoveListener(ShowReceivedItem);

            factory?.Dispose();
            factory = null;
            if (runtimeUIRoot != null) Destroy(runtimeUIRoot);
        }
    }
}
