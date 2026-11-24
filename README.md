# LOGIYGames Inventory

Inventory package for Unity 6000.6. Provides item definitions, serializable stacks and slots, and an inventory controller with stack-aware operations.

## Core concepts

- `ItemDefinition` — ScriptableObject describing item identity, icon, prefab, stack limit, and game-specific selling/drop flags.
- `ItemStack` — serializable value containing an ItemDefinition reference and quantity.
- `ItemSlot` — one inventory position with an `OnItemChanged` event.
- `InventoryController` — query, add, remove, move, split, swap, transfer, and auto-stack API.

Runtime item identity is based on the ItemDefinition asset reference. `Id` is a stable key intended for save-game integration; it is not used by the runtime stack matcher.

## Quick start

~~~csharp
using LOGIYGames.Inventory;

int remainder = inventory.AddItem(new ItemStack(potionDefinition, 27));
if (remainder > 0)
    UnityEngine.Debug.LogWarning($"Inventory could not fit {remainder} items.");
~~~

`AddItem` returns the quantity that did not fit (zero means all units were accepted). `RemoveItem` is all-or-nothing, while `RemoveFromSlot` and `TransferTo` may be partial.

## Documentation

- [API.json](Documentation/API.json) — structured API contract for AI agents and tooling.
- [Manual.md](Documentation/Manual.md) — complete Russian-language manual.
- [QuickStart.pdf](Documentation/QuickStart.pdf) — four-page usage and UI integration guide.

## Optional sample UI

Import the `UI Example` sample to use InventoryUIFactory, presenter/view components, context menu, drag ghost, and ItemReceivedUI. `PlayerInventoryUIBootstrap` is a project integration example and references project-level Character/PlayerManager/GameManager types; use InventoryUIFactory directly when those types are not part of your project.

## Compatibility and scope

- Package ID: `com.logiygames.inventory`
- Package version: `0.1.0`
- Unity version in package manifest: `6000.6`
- Runtime assembly: `LOGIYGames.Inventory`
- Namespace: `LOGIYGames.Inventory`

Persistence, currency, automatic world drops, equipment rules, item use, and network synchronization are intentionally left to the consuming game.
