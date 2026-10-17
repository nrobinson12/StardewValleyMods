using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace StaticInventory.Patches
{
    /// <summary>
    /// Replaces vanilla toolbar shifting, which rotates the whole inventory, with a row offset.
    /// Tab, Ctrl+Tab, LB/RB and the debug B/N keys all go through Farmer.shiftToolbar.
    /// </summary>
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.shiftToolbar))]
    internal static class ShiftToolbarPatch
    {
        private static bool Prefix(Farmer __instance, bool right)
        {
            if (__instance != Game1.player) return true;

            // Same guard as vanilla Farmer.shiftToolbar.
            if (__instance.Items == null || __instance.Items.Count < Farmer.hotbarSize || __instance.UsingTool || Game1.dialogueUp || !__instance.CanMove || !__instance.Items.HasAny() || Game1.eventUp || Game1.farmEvent != null)
            {
                return false;
            }

            Game1.playSound("shwip");
            ToolbarRow.Shift(right);
            __instance.netItemStowed.Set(newValue: false);
            __instance.CurrentItem?.actionWhenBeingHeld(__instance);

            foreach (IClickableMenu menu in Game1.onScreenMenus)
            {
                if (menu is Toolbar toolbar)
                {
                    toolbar.shifted(right);
                    break;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// New items go into the first empty slot from index 0, which is the first row, not the toolbar.
    /// After vanilla places the item, move it to the first empty toolbar slot if there is one.
    /// All pickups (debris, gifts, crafting, etc.) go through this overload.
    /// A postfix is enough because vanilla puts the item itself into the empty slot, so we can find it.
    /// </summary>
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.addItemToInventory), new[] { typeof(Item), typeof(List<Item>) })]
    internal static class AddItemToInventoryPatch
    {
        private static void Prefix(Farmer __instance, Item item, out bool __state)
        {
            // Skip items that are already in the inventory, so we only move newly placed ones.
            __state = item != null && __instance.Items.Contains(item);
        }

        private static void Postfix(Farmer __instance, Item item, bool __state)
        {
            if (__instance != Game1.player || item == null || __state || ToolbarRow.Current == 0) return;

            // In menus (chests, shops, etc.) the whole inventory is visible, so keep vanilla's first free slot.
            if (Game1.activeClickableMenu != null) return;

            // Not found means it was merged into existing stacks or didn't fit.
            int placedAt = __instance.Items.IndexOf(item);
            if (placedAt < 0) return;

            if (ToolbarRow.IsOnToolbar(placedAt)) return;

            // Toolbar full: keep vanilla's choice, the first empty slot in the inventory.
            int toolbarSlot = ToolbarRow.FirstEmptySlot();
            if (toolbarSlot < 0) return;

            __instance.Items[placedAt] = null;
            __instance.Items[toolbarSlot] = item;
        }
    }

    /// <summary>
    /// Vanilla treats slots 0-11 as the toolbar everywhere it selects a slot: number keys,
    /// the scroll wheel and triggers, toolbar clicks and inventory page hotkeys.
    /// Mapping those onto the toolbar row here covers all of them at once.
    /// </summary>
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.CurrentToolIndex), MethodType.Setter)]
    internal static class CurrentToolIndexPatch
    {
        private static void Prefix(Farmer __instance, ref int value)
        {
            if (__instance != Game1.player || value < 0 || value >= Farmer.hotbarSize) return;

            value = ToolbarRow.ToInventoryIndex(value);
        }
    }
}
