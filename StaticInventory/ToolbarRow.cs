using StardewModdingAPI;
using StardewValley;

namespace StaticInventory
{
    /// <summary>
    /// Tracks which inventory row the local player's toolbar shows.
    /// The items never move; the toolbar just points at a different row.
    /// </summary>
    internal static class ToolbarRow
    {
        private const string ModDataKey = "Dinotopea.StaticInventory/ToolbarRow";

        /// <summary>
        /// The row the toolbar shows. Stored in the farmer's modData so it's saved per player
        /// and works for farmhands and split-screen.
        /// </summary>
        public static int Current
        {
            get
            {
                Farmer player = Game1.player;
                if (player?.Items == null) return 0;

                // Not saved yet (new save, or never shifted).
                if (!player.modData.TryGetValue(ModDataKey, out string savedRow)) return 0;

                // Saved value isn't a number.
                if (!int.TryParse(savedRow, out int row)) return 0;

                // Ignore a row that no longer fits (e.g. another mod shrank the inventory).
                return row >= 0 && row < Count ? row : 0;
            }
            set
            {
                Game1.player.modData[ModDataKey] = value.ToString();
            }
        }

        /// <summary>How many rows the inventory has.</summary>
        public static int Count => Game1.player.Items.Count / Farmer.hotbarSize;

        /// <summary>The inventory index of the first toolbar slot.</summary>
        public static int FirstSlot => Current * Farmer.hotbarSize;

        /// <summary>Convert a toolbar slot (0-11) to its inventory index.</summary>
        public static int ToInventoryIndex(int toolbarSlot)
        {
            return FirstSlot + toolbarSlot;
        }

        /// <summary>Whether an inventory index is on the toolbar's row.</summary>
        public static bool IsOnToolbar(int inventoryIndex)
        {
            return inventoryIndex >= FirstSlot && inventoryIndex < FirstSlot + Farmer.hotbarSize;
        }

        /// <summary>
        /// The selected item's toolbar slot (0-11), or outside that range if it isn't on the toolbar's row.
        /// Takes a farmer because the Toolbar.draw transpiler calls it in place of farmer.CurrentToolIndex.
        /// </summary>
        public static int SelectedToolbarSlot(Farmer farmer)
        {
            return farmer.CurrentToolIndex - FirstSlot;
        }

        /// <summary>The inventory index of the first empty toolbar slot, or -1 if the toolbar is full.</summary>
        public static int FirstEmptySlot()
        {
            for (int i = 0; i < Farmer.hotbarSize; i++)
            {
                int index = ToInventoryIndex(i);
                if (Game1.player.Items[index] == null) return index;
            }

            return -1;
        }

        /// <summary>Move the toolbar one row, keeping the same slot selected.</summary>
        public static void Shift(bool right)
        {
            Farmer player = Game1.player;
            int selected = player.CurrentToolIndex;
            bool selectedInInventory = selected >= 0 && selected < player.Items.Count;

            Current = Mod(Current + (right ? 1 : -1), Count);

            // The CurrentToolIndex setter patch maps the slot onto the new row.
            if (selectedInInventory) player.CurrentToolIndex = selected % Farmer.hotbarSize;

            ModEntry.MonitorObject.Log($"Toolbar now shows row {Current + 1} of {Count}.", LogLevel.Trace);
        }

        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
