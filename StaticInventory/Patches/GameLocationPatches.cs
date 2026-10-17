using HarmonyLib;
using StardewValley;
using System;

namespace StaticInventory.Patches
{
    /// <summary>
    /// Picking up furniture puts it in slots 0-11 and selects that slot. If those are full, vanilla
    /// kicks the item in slot 11 out to another slot. When the toolbar shows another row, do the same
    /// thing on the toolbar row instead, so nothing on the first row gets moved.
    /// </summary>
    [HarmonyPatch(typeof(GameLocation), "removeQueuedFurniture")]
    internal static class RemoveQueuedFurniturePatch
    {
        /// <summary>Mirrors vanilla GameLocation.removeQueuedFurniture, using the toolbar row.</summary>
        private static bool Prefix(GameLocation __instance, Guid guid)
        {
            if (ToolbarRow.Current == 0) return true;

            Farmer who = Game1.player;
            if (!__instance.furniture.TryGetValue(guid, out var furnitureItem) || !who.couldInventoryAcceptThisItem(furnitureItem))
            {
                return false;
            }

            furnitureItem.performRemoveAction();
            __instance.furniture.Remove(guid);

            int emptySlot = ToolbarRow.FirstEmptySlot();
            if (emptySlot >= 0)
            {
                who.Items[emptySlot] = furnitureItem;
                who.CurrentToolIndex = emptySlot % Farmer.hotbarSize;
            }
            else
            {
                // Toolbar row is full: like vanilla, swap into its last slot and move that item elsewhere.
                int lastSlot = ToolbarRow.ToInventoryIndex(Farmer.hotbarSize - 1);
                Item kickedOut = who.addItemToInventory(furnitureItem, lastSlot);
                who.addItemToInventory(kickedOut);
                who.CurrentToolIndex = Farmer.hotbarSize - 1;
            }

            __instance.localSound("coin");
            return false;
        }
    }
}
