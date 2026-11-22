using System;

namespace LOGIYGames
{
    public readonly struct ContextMenuAction
    {
        public readonly string Label;
        public readonly Action Callback;
        public readonly bool Interactable;

        public ContextMenuAction(string label, Action callback, bool interactable = true)
        {
            Label = label;
            Callback = callback;
            Interactable = interactable;
        }
    }
}
