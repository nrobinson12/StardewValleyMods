using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace AutoSort
{
    public class ModEntry : Mod
    {
        private IClickableMenu? prevMenu;
        private int prevChestCount = -1;

        public override void Entry(IModHelper helper)
        {
            prevMenu = null;
            helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;
            helper.Events.World.ChestInventoryChanged += OnChestInventoryChanged;
        }

        private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Game1.activeClickableMenu is ItemGrabMenu menu && menu.context is Chest chest)
            {
                SortChestIfPossible(chest, false);

                if (menu.organizeButton != null)
                {
                    Debug("Removing organize button");
                    menu.organizeButton = null;
                }
            }

            prevMenu = Game1.activeClickableMenu;
        }

        private void OnChestInventoryChanged(object? sender, ChestInventoryChangedEventArgs e)
        {
            Debug("Chest inventory changed");

            if (Game1.activeClickableMenu is ItemGrabMenu menu && menu.context is Chest chest)
            {
                if (chest.Items.Count == prevChestCount)
                {
                    SortChestIfPossible(chest, true);
                }
            }
        }

        private void SortChestIfPossible(Chest chest, bool sortSameCount)
        {
            // Opening a new chest
            bool newChest = IsChest(prevMenu) is null;

            // Same chest, different # of items
            bool newItemCount = chest.Items.Count != prevChestCount;

            if (chest.Items.Count != prevChestCount)
            {
                Debug("Previous chest count: " + prevChestCount);
                Debug("Current chest count: " + chest.Items.Count);
            }

            if (newChest || newItemCount || sortSameCount)
            {
                if (newChest) Debug("Opening a new chest");
                if (newItemCount) Debug("Chest items count changed");
                OrganizeChestIfNecessary(chest.Items);
            }

            prevChestCount = chest.Items.Count;
        }

        private static Chest? IsChest(IClickableMenu? clickableMenu)
        {
            if (clickableMenu is not ItemGrabMenu menu) return null;
            if (menu.context is not Chest chest) return null;
            return chest;
        }

        private void OrganizeChestIfNecessary(Inventory items)
        {
            if (!AreItemsSorted(items))
            {
                Debug("Sorting items");
                ItemGrabMenu.organizeItemsInList(items);
            }
            else Debug("Items are already sorted");
        }

        private bool AreItemsSorted(Inventory items)
        {

            // If there are no items or only one item, it is sorted
            if (items.Count <= 1)
            {
                Debug("Items.Count <= 0");
                return true;
            }

            List<Item> copy = new List<Item>(items);

            // Make sure all tools are at the start
            // Make sure there are no nulls before items
            bool seenNonTool = false;
            bool seenNullItem = false;
            for (int i = 0; i < copy.Count; i++)
            {
                Item item = copy[i];

                if (item is Tool)
                {
                    if (seenNonTool)
                    {
                        // If we've seen a non-tool before,
                        // and there is another tool after it, it's not sorted
                        Debug("Seen non-tool before seeing another tool");
                        return false;
                    }

                    copy.RemoveAt(i--);
                }

                // If there is a gap in the chest
                // This will happen when clicking on an item you can't add to your inventory
                if (item is not null && seenNullItem)
                {
                    Debug("Seen null item before seeing another item");
                    return false;
                }

                seenNullItem = item is null;
                seenNonTool = item is not Tool;
            }

            // Remove trailing null items
            for (int i = copy.Count - 1; i >= 0; i--)
            {
                if (copy[i] is null)
                {
                    copy.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }

            // Make sure everything is stacked
            for (int i = 0; i < copy.Count; i++)
            {
                Item current_item = copy[i];
                if (current_item.getRemainingStackSpace() <= 0)
                {
                    continue;
                }

                for (int j = i + 1; j < copy.Count; j++)
                {
                    Item other_item = copy[j];

                    // If an item can stack with another item, it's not sorted
                    if (current_item.canStackWith(other_item))
                    {
                        Debug("2 items can stack");
                        return false;
                    }
                }
            }
            
            // Make sure it is sorted otherwise
            Comparer<Item> comparer = Comparer<Item>.Default;
            for (int i = 1; i < copy.Count; i++)
            {
                if (comparer.Compare(copy[i - 1], copy[i]) > 0)
                {
                    Debug("items are not sorted");
                    return false;
                }
            }

            // If you made it this far, it is sorted
            return true;
        }

        private void Debug(string message)
        {
            this.Monitor.Log(message, LogLevel.Debug);
        }

    }
}
