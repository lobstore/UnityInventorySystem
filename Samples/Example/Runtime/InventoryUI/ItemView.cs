using Alchemy.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    public class ItemView : MonoBehaviour
    {
        [SerializeField, Required] TextMeshProUGUI stackSize;
        [SerializeField, Required] Image icon;
        [SerializeField, Required] Sprite missingIconSprite;

        ItemStack item;

        public Image Icon => icon;

        public void SetItem(ItemStack value)
        {
            item = value;
            if (item.Data == null)
            {
                icon.sprite = missingIconSprite;
                stackSize.enabled = false;
                stackSize.text = string.Empty;
                return;
            }
            icon.sprite = item.Data.Icon ?? missingIconSprite;
            stackSize.enabled = item.Amount > 1;
            stackSize.text = item.Amount.ToString();
        }
    }
}
