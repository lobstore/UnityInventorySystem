using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    /// <summary>Displays an item icon and stack quantity. No inventory state is stored in this view.</summary>
    public class ItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stackSize;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite missingIconSprite;

        /// <summary>Image used to display the current item's icon.</summary>
        public Image Icon => icon;

        /// <summary>Updates the image and quantity label from a stack value.</summary>
        public void SetItem(ItemStack value)
        {
            if (icon == null || stackSize == null)
            {
                Debug.LogWarning("ItemView requires an Image and TextMeshProUGUI reference.", this);
                return;
            }

            icon.sprite = value.IsEmpty
                ? missingIconSprite
                : value.Data.Icon != null ? value.Data.Icon : missingIconSprite;

            stackSize.enabled = !value.IsEmpty && value.Amount > 1;
            stackSize.text = value.IsEmpty ? string.Empty : value.Amount.ToString();
        }
    }
}
