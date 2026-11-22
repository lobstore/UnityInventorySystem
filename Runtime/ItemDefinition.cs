using System.Collections.Generic;
using UnityEngine;

namespace LOGIYGames.Inventory  
{
    [CreateAssetMenu(menuName = "Inventory/Item/Data")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string Id;                       // уникальный строковый id для сейвов
        public string DisplayName;
        [TextArea(2, 6)] public string Description;
        public Sprite Icon;
        public GameObject Prefab;
        [Header("Stacking")]
        [Min(1)] public int MaxStackSize = 1;

        [Header("Economy & Rules")]
        public bool Sellable = true;
        public int SellPrice = 0;
        public bool Droppable = true;
    }
}
