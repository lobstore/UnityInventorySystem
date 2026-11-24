# Changelog

All notable changes are documented here.

## [Unreleased]

### Added
- XML documentation for core inventory data and operation types.
- Structured API contract for AI agents.
- Full Russian manual and Quick Start PDF.
- Consistent inventory-level removal notifications for Clear/ClearSlot and transfer operations.

### Changed
- Normalize missing serialized slots and UnityEvent references before use.
- Clamp slot writes to the item's MaxStackSize and centralize empty-stack semantics.
- Harden add/remove, split, move, swap, clear, and cross-inventory transfer behavior.
- Make the sample UI factory own only the views it creates and explicitly unbind them before destruction.
- Improve sample UI null validation, listener cleanup, context-menu configuration, and transient notification lifecycle.
- Preserve serialized field names, runtime namespace, assembly name, and existing .meta files.

### Fixed
- Negative ItemStack amounts are normalized to zero.
- Null item definitions no longer participate in searches or stacking.
- ContainsItem rejects negative requested amounts.
- Clear operations now report removed stacks through OnItemRemoved.
- TransferTo reports source inventory removal after the target accepts items.
- Slot view subscriptions are removed on rebind/unbind/destroy.
- UI sample avoids stale listeners and destroys only owned slot views.

## [0.1.0] - 2026-11-22

### Added
- Initial release of LOGIYGames Inventory.
