using HarmonyLib;
using StardewModdingAPI;

namespace StaticInventory
{
    /// <summary>
    /// The mod entry point.
    /// </summary>
    public class ModEntry : Mod
    {
        public static IMonitor MonitorObject { get; private set; }

        /*********
        ** Public methods
        *********/

        /// <summary>
        /// The mod entry point, called after the mod is first loaded.
        /// </summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            MonitorObject = Monitor;
            new Harmony(ModManifest.UniqueID).PatchAll();
        }
    }
}
