using HarmonyLib;


namespace NoMapWayfinding.Patches
{
    [HarmonyPatch(typeof(Minimap))]
    internal static class MinimapPatches
    {
        /// <summary>
        /// Gate the full-screen map, and let a cartography table override a world-level no-map rule.
        ///
        /// Two things happen here.
        ///
        /// A blocked request is downgraded to <see cref="Minimap.MapMode.Small"/> rather than to
        /// None. Minimap.UpdateMap - which drives pin upkeep, biome discovery and the shared-map
        /// fade - only runs while the mode is Small or Large, so forcing None would quietly stop
        /// more than the drawing.
        ///
        /// And when the table has granted access, Game.m_noMap is cleared for the duration of this
        /// one call. SetMapMode is the single place in the whole assembly that reads that flag, so
        /// clearing it here is enough to open the map and affects nothing else. It is restored in
        /// the postfix whatever happens. This is what lets a server run with the NoMap global key -
        /// which costs unmodded clients their minimap through Iron Gate's own code - while modded
        /// clients can still read the shared map at a table.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("SetMapMode")]
        private static void SetMapMode_Prefix(ref Minimap.MapMode mode, out bool __state)
        {
            __state = Game.m_noMap;

            if (!Settings.Enabled)
            {
                return;
            }

            if (mode == Minimap.MapMode.Large && MapAccess.Granted)
            {
                Game.m_noMap = false;
                return;
            }

            if (mode == Minimap.MapMode.Large && Settings.RequireTableForMap)
            {
                mode = Minimap.MapMode.Small;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetMapMode")]
        private static void SetMapMode_Postfix(Minimap __instance, bool __state)
        {
            Game.m_noMap = __state;
            ApplyMinimapVisibility(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void Update_Postfix(Minimap __instance)
        {
            // Re-assert every frame: the vanilla HUD toggle and several UI paths reactivate
            // the small root without going through SetMapMode.
            ApplyMinimapVisibility(__instance);
            MapAccess.Tick();
        }

        /// <summary>
        /// Hiding the minimap means hiding its pins too.
        ///
        /// Minimap.UpdatePins is called straight from Minimap.Update rather than from UpdateMap,
        /// and it is not gated on the map mode, so it keeps instantiating pin icons into
        /// m_pinRootSmall the whole time the minimap is hidden. Deactivating only m_smallRoot
        /// left those icons drawing over the HUD.
        ///
        /// This matters more with the mod than in vanilla: AddSharedMapData imports every other
        /// player's saved pins, and auto-sync runs it on every use of a table rather than only on
        /// a deliberate read, so a group accumulates a lot of pins quickly.
        /// </summary>
        private static void ApplyMinimapVisibility(Minimap map)
        {
            if (map == null || map.m_smallRoot == null)
            {
                return;
            }

            // Only Small owns the corner minimap. In Large and None the game has already
            // deactivated these and we must not fight it - which also means that under a NoMap
            // world, where the mode never leaves None, there is nothing for us to do. Scoping to
            // Small also keeps us off the large map's own pins when a table opens it.
            if (map.m_mode != Minimap.MapMode.Small)
            {
                return;
            }

            bool shouldBeVisible = !(Settings.Enabled && Settings.HideMinimap);

            SetActive(map.m_smallRoot, shouldBeVisible);

            if (map.m_pinRootSmall != null)
            {
                SetActive(map.m_pinRootSmall.gameObject, shouldBeVisible);
            }

            if (map.m_pinNameRootSmall != null)
            {
                SetActive(map.m_pinNameRootSmall.gameObject, shouldBeVisible);
            }

            // UpdatePins turns this on whenever shared pins exist. Only ever force it off: when
            // the minimap is visible again it is vanilla's to control, not ours.
            if (!shouldBeVisible && map.m_sharedMapHint != null)
            {
                SetActive(map.m_sharedMapHint, false);
            }
        }

        private static void SetActive(UnityEngine.GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
