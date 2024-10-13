using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using static StardewValley.GameLocation;

namespace ConfirmTotem
{
    internal class ObjectPatches
    {
        // initialized by ModEntry.cs
        // initialized by ModEntry.cs
        public static ModEntry ModInstance;
        public static ModConfig Config;

        public static bool fromConfirmation = false;
        public static StardewValley.Object totemBeingConfirmed = null;
        public static GameLocation locationBeingConfirmed = null;

        public static bool Object_performUseAction_Prefix(GameLocation location, StardewValley.Object __instance)
        {
            // If we should ask for confirmation, then do so and return false (skips base game function)
            // Otherwise, return true (runs base game function normally)
            try
            {
                // Would the base game reach the code for "totem is being used"?

                if (!Game1.player.canMove)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (player can't move)", LogLevel.Trace);
                    return true;
                }
                if (__instance.isTemporarilyInvisible)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (object is temporarily invisible)", LogLevel.Trace);
                    return true;
                }
                if (__instance.name == null)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (object has no name)", LogLevel.Trace);
                    return true;
                }
                if (!__instance.name.Contains("Totem"))
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (object isn't a totem)", LogLevel.Trace);
                    return true;
                }

                // normal_gameplay
                if (Game1.eventUp)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (event up)", LogLevel.Trace);
                    return true;
                }
                if (Game1.isFestival())
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (festival)", LogLevel.Trace);
                    return true;
                }
                if (Game1.fadeToBlack)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (fade to black)", LogLevel.Trace);
                    return true;
                }
                if (Game1.player.swimming.Value)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (swimming)", LogLevel.Trace);
                    return true;
                }
                if (Game1.player.bathingClothes.Value)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (bathing clothes)", LogLevel.Trace);
                    return true;
                }
                if (Game1.player.onBridge.Value)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Ignoring object use (on bridge)", LogLevel.Trace);
                    return true;
                }

                // If farmer just confirmed, then reset mod state and let normal action occur
                if (fromConfirmation)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Got confirmation", LogLevel.Debug);
                    totemBeingConfirmed = null;
                    locationBeingConfirmed = null;
                    fromConfirmation = false;
                    return true;
                }

                // If someone else's confirmation is pending, then give up and let normal action occur
                // Future improvement: track pending confirmations on a per-farmer basis
                if (totemBeingConfirmed != null || locationBeingConfirmed != null)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Mod can't handle overlapping confirmations in multiplayer", LogLevel.Debug);
                    return true;
                }

                // What type of totem is it?
                // Future improvement: allow classifying additional items in config file
                var totemType = ModEntry.TotemType_Unknown;
                switch (__instance.QualifiedItemId)
                {
                    case "(O)261": // desert
                    case "(O)688": // farm
                    case "(O)689": // mountains
                    case "(O)690": // beach
                    case "(O)886": // island
                        totemType = ModEntry.TotemType_Warp;
                        break;

                    case "(O)681": // rain
                        totemType = ModEntry.TotemType_Rain;
                        break;

                    case "(O)TreasureTotem":
                        totemType = ModEntry.TotemType_Treasure;
                        break;

                    default:
                        if (__instance.name.Contains("Warp"))
                        {
                            totemType = ModEntry.TotemType_Warp;
                            break;
                        }
                        if (__instance.name.Contains("Rain"))
                        {
                            totemType = ModEntry.TotemType_Rain;
                            break;
                        }
                        if (__instance.name.Contains("Treasure"))
                        {
                            totemType = ModEntry.TotemType_Treasure;
                            break;
                        }
                        break;
                }

                ModInstance.Monitor.Log($"[Confirm Totem] Object {__instance.name} (ID {__instance.ParentSheetIndex}) has type {totemType}", LogLevel.Debug);

                string totemConfirmationLevel;
                switch (totemType)
                {
                    case ModEntry.TotemType_Warp:
                        totemConfirmationLevel = Config.ConfirmationLevelWarpTotem;
                        break;
                    case ModEntry.TotemType_Rain:
                        totemConfirmationLevel = Config.ConfirmationLevelRainTotem;
                        break;
                    case ModEntry.TotemType_Treasure:
                        totemConfirmationLevel = Config.ConfirmationLevelTreasureTotem;
                        break;
                    default:
                        totemConfirmationLevel = Config.ConfirmationLevelUnknownTotem;
                        break;
                }

                // Future improvement: allow whitelisting/blacklisting locations where game should ask for confirmation in config file

                // If current type shouldn't be confirmed, then reset mod state and let normal action occur
                if (totemConfirmationLevel == ModEntry.ConfirmationLevel_None)
                {
                    ModInstance.Monitor.Log("[Confirm Totem] Confirmation level is None", LogLevel.Debug);
                    totemBeingConfirmed = null;
                    locationBeingConfirmed = null;
                    fromConfirmation = false;
                    return true;
                }

                // Record mod state
                totemBeingConfirmed = __instance;
                locationBeingConfirmed = location;

                // Generate confirmation
                ModInstance.Monitor.Log("[Confirm Totem] Asking for confirmation", LogLevel.Debug);
                switch (totemConfirmationLevel)
                {
                    case ModEntry.ConfirmationLevel_Dialog:
                        location.createQuestionDialogue(
                            question: string.Format(ModInstance.Helper.Translation.Get("Question_UseTotem"), __instance.name),
                            answerChoices: location.createYesNoResponses(),
                            afterDialogueBehavior: new afterQuestionBehavior(totemResponse)
                        );
                        break;
                    case ModEntry.ConfirmationLevel_Warning:
                        Game1.activeClickableMenu = new ConfirmationDialog(
                            string.Format(ModInstance.Helper.Translation.Get("Question_UseTotem"), __instance.name),
                            totemConfirmed,
                            totemCanceled
                        );
                        break;
                }

                // Block normal action
                return false;
            }
            catch (Exception ex)
            {
                ModInstance.Monitor.Log($"[Confirm Totem] Object_performUseAction_Prefix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
                return true;
            }
        }

        private static void totemResponse(Farmer who, string responseKey)
        {
            who.canMove = true; // otherwise they get stuck
            if (responseKey.Contains("Yes"))
            {
                totemConfirmed(who);
            }
            else
            {
                totemCanceled(who);
            }
        }

        private static void totemConfirmed(Farmer who)
        {
            // Reset UI state
            Game1.exitActiveMenu();

            // If we lost track of mod state, then give up and exit

            if (totemBeingConfirmed == null)
            {
                ModInstance.Monitor.Log("[Confirm Totem] Ignoring confirmation (lost track of totem)", LogLevel.Error);
                return;
            }
            if (locationBeingConfirmed == null)
            {
                ModInstance.Monitor.Log("[Confirm Totem] Ignoring confirmation (lost track of location)", LogLevel.Error);
                return;
            }

            // Indicate that it was confirmed
            fromConfirmation = true;

            // Retain a reference to totemBeingConfirmed before .performUseAction() clears it
            var totemJustConfirmed = totemBeingConfirmed;

            // Trigger normal action
            ModInstance.Monitor.Log("[Confirm Totem] Processing confirmation", LogLevel.Trace);
            totemBeingConfirmed.performUseAction(locationBeingConfirmed);

            // Use up the totem if it was in inventory
            // Ignore other stuff equivalent to totems, e.g. Shrouded Figure at Night Market
            if (who.ActiveItem == totemJustConfirmed)
            {
                who.reduceActiveItemByOne();
            }
        }

        private static void totemCanceled(Farmer who)
        {
            // Reset UI state
            Game1.exitActiveMenu();

            // Reset mod state
            ModInstance.Monitor.Log("[Confirm Totem] Totem was canceled", LogLevel.Debug);
            fromConfirmation = false;
            totemBeingConfirmed = null;
            locationBeingConfirmed = null;
        }

    }
}
