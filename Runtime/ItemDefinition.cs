using UnityEngine;

namespace LOGIYGames.Inventory
{
    /// <summary>
    /// Authoring asset that defines an item type and its inventory rules.
    /// Inventory identity is based on the asset reference at runtime; <see cref="Id"/> is intended
    /// for save-game and external-data integration and should be unique and stable.
    /// </summary>
    [CreateAssetMenu(menuName = "Inventory/Item/Data")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique identifier used by save systems. Assign and keep this value stable across releases.")]
        public string Id;

        /// <summary>Display name shown to the player.</summary>
        public string DisplayName;

        /// <summary>Optional item description.</summary>
        [TextArea(2, 6)]
        public string Description;

        /// <summary>Optional inventory icon.</summary>
        public Sprite Icon;

        /// <summary>Optional world prefab used by game-specific drop/spawn logic.</summary>
        public GameObject Prefab;

        [Header("Stacking")]
        /// <summary>Maximum number of units allowed in one slot.</summary>
        [Min(1)]
        public int MaxStackSize = 1;

        [Header("Economy & Rules")]
        /// <summary>Whether game-specific selling logic should allow this item.</summary>
        public bool Sellable = true;

        /// <summary>Price per unit. The inventory package does not implement currency handling.</summary>
        [Min(0)]
        public int SellPrice = 0;

        /// <summary>Whether game-specific drop logic should allow this item to be dropped.</summary>
        public bool Droppable = true;

#if UNITY_EDITOR
        private void OnValidate()
        {
            MaxStackSize = Mathf.Max(1, MaxStackSize);
            SellPrice = Mathf.Max(0, SellPrice);
        }
#endif
    }
}
