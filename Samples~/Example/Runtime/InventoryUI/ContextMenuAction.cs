using System;

namespace LOGIYGames
{
    /// <summary>
    /// Immutable action description used by the Inventory sample context menu.
    /// </summary>
    public readonly struct ContextMenuAction
    {
        /// <summary>Text displayed by the menu button.</summary>
        public readonly string Label;

        /// <summary>Callback invoked when the user activates the action.</summary>
        public readonly Action Callback;

        /// <summary>Whether the menu button is interactable.</summary>
        public readonly bool Interactable;

        /// <summary>Creates a context-menu action.</summary>
        public ContextMenuAction(string label, Action callback, bool interactable = true)
        {
            Label = label;
            Callback = callback;
            Interactable = interactable;
        }
    }
}
