using GenericModConfigMenu;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;

namespace ConfirmTotem
{
    public class ModEntry : Mod
    {
        public const string ConfirmationLevel_None = "None";
        public const string ConfirmationLevel_Dialog = "Dialog";
        public const string ConfirmationLevel_Warning = "Warning";

        public const string TotemType_Warp = "Warp";
        public const string TotemType_Rain = "Rain";
        public const string TotemType_Treasure = "Treasure";
        public const string TotemType_Unknown = "Unknown";

        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();

            Helper.Events.GameLoop.GameLaunched += (e, a) => OnGameLaunched(e, a);

            ObjectPatches.ModInstance = this;
            ObjectPatches.Config = this.Config; // private, so must be provided directly

            var harmony = new Harmony(this.ModManifest.UniqueID);
            // detect when totem is used
            // can't patch totemWarp() or rainTotem() or treasureTotem() because they're private, so must patch function that calls those
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performUseAction)),
               prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.Object_performUseAction_Prefix))
            );
        }

        /// <summary>Add to Generic Mod Config Menu</summary>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add config options
            configMenu.AddTextOption(
                mod: this.ModManifest,
                getValue: () => this.Config.ConfirmationLevelWarpTotem,
                setValue: value => this.Config.ConfirmationLevelWarpTotem = value,
                name: () => Helper.Translation.Get("Options_WarpTotem"),
                allowedValues: new string[] {
                    ConfirmationLevel_None,
                    ConfirmationLevel_Dialog,
                    ConfirmationLevel_Warning
                },
                formatAllowedValue: value => Helper.Translation.Get($"Options_{value}")
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                getValue: () => this.Config.ConfirmationLevelRainTotem,
                setValue: value => this.Config.ConfirmationLevelRainTotem = value,
                name: () => Helper.Translation.Get("Options_RainTotem"),
                allowedValues: new string[] {
                    ConfirmationLevel_None,
                    ConfirmationLevel_Dialog,
                    ConfirmationLevel_Warning
                },
                formatAllowedValue: value => Helper.Translation.Get($"Options_{value}")
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                getValue: () => this.Config.ConfirmationLevelTreasureTotem,
                setValue: value => this.Config.ConfirmationLevelTreasureTotem = value,
                name: () => Helper.Translation.Get("Options_TreasureTotem"),
                allowedValues: new string[] {
                    ConfirmationLevel_None,
                    ConfirmationLevel_Dialog,
                    ConfirmationLevel_Warning
                },
                formatAllowedValue: value => Helper.Translation.Get($"Options_{value}")
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                getValue: () => this.Config.ConfirmationLevelUnknownTotem,
                setValue: value => this.Config.ConfirmationLevelUnknownTotem = value,
                name: () => Helper.Translation.Get("Options_UnknownTotem"),
                allowedValues: new string[] {
                    ConfirmationLevel_None,
                    ConfirmationLevel_Dialog,
                    ConfirmationLevel_Warning
                },
                formatAllowedValue: value => Helper.Translation.Get($"Options_{value}")
            );
        }
    }
}
