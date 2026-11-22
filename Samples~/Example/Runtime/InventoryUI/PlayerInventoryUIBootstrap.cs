using LOGIYGames.Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    public class PlayerInventoryUIBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject inventorySlotPrefab;
        [SerializeField] private RectTransform inventoryContainer;
        [SerializeField] private CanvasGroup group;

        [Header("Drag ghost (optional — auto-created if null)")]
        [SerializeField] private Image dragGhost;
        [SerializeField] private TMP_Text dragGhostAmount;

        [Header("Context menu (optional — auto-created if null)")]
        [SerializeField] private ItemContextMenu contextMenu;

        [SerializeField] private ItemReceivedUI itemReceivedUI;

        private GameObject runtimeUIRoot;

        private InventoryUIFactory Factory;
        private InventoryController currentInventory;

        public bool IsInventoryOpened { get; private set; }

        private void Awake()
        {
            EnsureRuntimeUI();

            if (PlayerManager.Instance != null)
                PlayerManager.Instance.OnCharacterChanged.AddListener(UpdateInventoryUI);
        }

        /// <summary>Подставляет дефолтные объекты для всех неназначенных ссылок.</summary>
        private void EnsureRuntimeUI()
        {
            bool needGhost = dragGhost == null;
            bool needMenu = contextMenu == null;
            if (!needGhost && !needMenu) return;

            // Ищем root canvas, чтобы созданные объекты были поверх UI.
            var canvas = inventoryContainer != null
                ? inventoryContainer.GetComponentInParent<Canvas>()
                : GetComponentInParent<Canvas>();
            Transform uiParent = canvas != null ? canvas.rootCanvas.transform : transform;

            runtimeUIRoot = new GameObject("InventoryRuntimeUI", typeof(RectTransform));
            runtimeUIRoot.transform.SetParent(uiParent, false);
            var rt = runtimeUIRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (needGhost)
                dragGhost = RuntimeInventoryUIBuilder.CreateDragGhost(
                    runtimeUIRoot.transform, out dragGhostAmount);

            if (needMenu)
                contextMenu = RuntimeInventoryUIBuilder.CreateContextMenu(runtimeUIRoot.transform);

            // Ghost должен быть ниже меню, если оба в runtimeUIRoot.
            if (needGhost && !needMenu) dragGhost.transform.SetAsFirstSibling();
            if (!needGhost && needMenu) contextMenu.transform.SetAsLastSibling();
        }

        private void UpdateInventoryUI(Character.Actor newChar)
        {
            if (currentInventory != null)
                currentInventory.OnItemAdded.RemoveListener(ShowReceivedItem);

            Factory?.Dispose();
            Factory = null;

            currentInventory = newChar != null ? newChar.GetComponent<InventoryController>() : null;
            if (currentInventory == null) return;

            currentInventory.OnItemAdded.AddListener(ShowReceivedItem);

            Factory = new InventoryUIFactory(
                currentInventory,
                inventoryContainer,
                inventorySlotPrefab,
                dragGhost,
                dragGhostAmount,
                contextMenu);

            Factory.Create();
        }

        private void ShowReceivedItem(ItemStack item) => itemReceivedUI.Show(item);

        private void Update()
        {
            if (GameManager.Instance.UIInput.InventoryPressed)
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

            Factory?.Dispose();

            if (runtimeUIRoot != null) Destroy(runtimeUIRoot);
        }
    }
}