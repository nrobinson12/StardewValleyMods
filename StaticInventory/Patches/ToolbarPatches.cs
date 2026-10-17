using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StaticInventory.Patches
{
    /// <summary>
    /// Makes the toolbar draw its current row instead of slots 0-11.
    /// A transpiler is used because a prefix would have to copy all of Toolbar.draw;
    /// this only rewrites the item reads and the selected-slot checks.
    /// </summary>
    [HarmonyPatch(typeof(Toolbar), nameof(Toolbar.draw))]
    internal static class ToolbarDrawPatch
    {
        // Toolbar.draw reads Items[i] twice (null check, then drawInMenu) and CurrentToolIndex twice (slot highlight, item scale).
        private const int ExpectedItemReads = 2;
        private const int ExpectedIndexReads = 2;

        private static readonly MethodInfo GetItem = AccessTools.Method(typeof(Inventory), "get_Item", new[] { typeof(int) });
        private static readonly MethodInfo GetToolIndex = AccessTools.PropertyGetter(typeof(Farmer), nameof(Farmer.CurrentToolIndex));
        private static readonly MethodInfo ToInventoryIndex = AccessTools.Method(typeof(ToolbarRow), nameof(ToolbarRow.ToInventoryIndex));
        private static readonly MethodInfo SelectedToolbarSlot = AccessTools.Method(typeof(ToolbarRow), nameof(ToolbarRow.SelectedToolbarSlot));

        /// <summary>
        /// Point the toolbar buttons at the toolbar's row, so vanilla clicks, hover tooltips
        /// and Shift+right-click drops use the right slot. They read the slot from the button name.
        /// </summary>
        private static void Prefix(Toolbar __instance)
        {
            // Skip if already renamed for this row. Parsing doesn't allocate, unlike building new strings.
            if (int.TryParse(__instance.buttons[0].name, out int firstButtonSlot) && firstButtonSlot == ToolbarRow.FirstSlot) return;

            for (int i = 0; i < __instance.buttons.Count; i++)
            {
                __instance.buttons[i].name = ToolbarRow.ToInventoryIndex(i).ToString();
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> original = new(instructions);
            List<CodeInstruction> code = new(original);
            int itemReads = 0;
            int indexReads = 0;

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].Calls(GetToolIndex))
                {
                    // Replace in place so any labels on this instruction are kept.
                    code[i] = new CodeInstruction(code[i]) { opcode = OpCodes.Call, operand = SelectedToolbarSlot };
                    indexReads++;
                }
                else if (code[i].Calls(GetItem))
                {
                    // The toolbar slot is on the stack; convert it to the toolbar row's inventory index.
                    code.Insert(i, new CodeInstruction(OpCodes.Call, ToInventoryIndex));
                    i++;
                    itemReads++;
                }
            }

            if (itemReads != ExpectedItemReads || indexReads != ExpectedIndexReads)
            {
                ModEntry.MonitorObject.Log($"Toolbar.draw has changed (found {itemReads} item reads and {indexReads} selected-slot reads). The toolbar will show the first row only.", LogLevel.Warn);
                return original;
            }

            ModEntry.MonitorObject.Log("Patched Toolbar.draw to show the selected row.", LogLevel.Trace);
            return code;
        }
    }
}
