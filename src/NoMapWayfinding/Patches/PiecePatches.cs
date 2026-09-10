using HarmonyLib;

namespace NoMapWayfinding.Patches
{
    [HarmonyPatch]
    internal static class PiecePatches
    {
        /// <summary>
        /// Stamp a cartography table with the moment it was placed.
        ///
        /// Piece.SetCreator is called from Player.PlacePiece on the freshly instantiated object
        /// and guards internally on GetCreator() == 0, so it runs exactly once, at placement, and
        /// never on load. That is what keeps tables built before this was installed unstamped and
        /// therefore usable.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Piece), "SetCreator")]
        private static void SetCreator_Postfix(Piece __instance)
        {
            if (!Settings.Enabled || Settings.TableSettlingSeconds <= 0f)
            {
                return;
            }

            MapTable table = __instance.GetComponentInChildren<MapTable>(includeInactive: true);
            if (table != null)
            {
                TableSettling.StampPlacement(table);
            }
        }

        /// <summary>
        /// Optionally give nothing back when a cartography table is removed, so using one as a
        /// disposable position finder costs a full table each time.
        ///
        /// Note this covers destruction as well as dismantling, since Valheim routes both through
        /// DropResources - a table smashed by a troll also leaves nothing behind.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Piece), "DropResources")]
        private static bool DropResources_Prefix(Piece __instance)
        {
            if (!Settings.Enabled || !Settings.TableNoRefund)
            {
                return true;
            }

            return __instance.GetComponentInChildren<MapTable>(includeInactive: true) == null;
        }
    }
}
