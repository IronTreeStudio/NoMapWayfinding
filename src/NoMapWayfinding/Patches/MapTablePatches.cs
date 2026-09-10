using System;
using System.Reflection;
using HarmonyLib;

namespace NoMapWayfinding.Patches
{
    /// <summary>
    /// The cartography table is the only place the world map can be read, and touching it
    /// performs a full two-way merge so everyone who visits leaves with everyone's exploration.
    ///
    /// This reuses Valheim's own OnRead/OnWrite rather than reimplementing the merge, so the
    /// shared-map packet format and its "MapData" RPC stay entirely vanilla. Unmodded players
    /// using the same table are unaffected, and the mod is safe on a vanilla server.
    ///
    /// Both methods are private and OnRead is overloaded, so everything here is bound by name
    /// and explicit argument types rather than by <c>nameof</c>.
    /// </summary>
    [HarmonyPatch]
    internal static class MapTablePatches
    {
        private static readonly Type[] SwitchCallbackArgs =
        {
            typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData)
        };

        private static readonly MethodInfo OnWriteMethod =
            AccessTools.Method(typeof(MapTable), "OnWrite", SwitchCallbackArgs);

        private static bool _syncing;

        /// <summary>Refuse a table that has not stood long enough yet, and say how long is left.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapTable), "OnRead", new[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) })]
        private static bool OnRead_Prefix(MapTable __instance, Humanoid user, ItemDrop.ItemData item)
        {
            return !Refused(__instance, user, item);
        }

        /// <summary>
        /// Take ownership of the table before any write.
        ///
        /// OnWrite builds its payload from the locally replicated copy of the table ZDO and then
        /// sends the merged result to the ZDO owner, who overwrites it wholesale. If we are not
        /// the owner that copy can be stale, so two players writing within a replication
        /// round-trip of each other lose one of the two contributions. Owning the ZDO first makes
        /// the read authoritative and runs RPC_MapData locally, which closes that window.
        ///
        /// Vanilla has the same race, but only on the write switch. Auto-sync writes on every use
        /// of the table, so it would hit this far more often.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapTable), "OnWrite", new[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) })]
        private static bool OnWrite_Prefix(MapTable __instance, Humanoid user, ItemDrop.ItemData item)
        {
            if (Refused(__instance, user, item))
            {
                return false;
            }

            if (ShouldHandle(user, item))
            {
                ZNetView view = __instance.GetComponent<ZNetView>();
                if (view != null && view.IsValid())
                {
                    view.ClaimOwnership();
                }
            }

            return true;
        }

        /// <summary>
        /// The "read map" switch. Vanilla only pulls the table's data down into the player, so
        /// push the player's data back up as well.
        /// </summary>
        /// <remarks>
        /// Deliberately does not inspect the return value: vanilla OnRead returns false on every
        /// path, because Switch uses the result to mean "consumed the item", not "succeeded".
        /// </remarks>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapTable), "OnRead", new[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) })]
        private static void OnRead_Postfix(MapTable __instance, Switch caller, Humanoid user, ItemDrop.ItemData item)
        {
            // Harmony still runs postfixes when a prefix skipped the original, so this has to
            // check the gate again rather than assume the read happened.
            if (_syncing || !ShouldHandle(user, item) || !TableSettling.IsSettled(__instance))
            {
                return;
            }

            if (Settings.AutoSyncAtTable)
            {
                PushLocalMapToTable(__instance, caller, user, item);
            }

            OfferMap(__instance);
        }

        /// <summary>
        /// The "write map" switch. Vanilla OnWrite already calls the four-argument OnRead
        /// overload first, so it is a complete two-way merge on its own and needs no help.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapTable), "OnWrite", new[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) })]
        private static void OnWrite_Postfix(MapTable __instance, Humanoid user, ItemDrop.ItemData item)
        {
            // Reentered from our own OnRead postfix; that call opens the map once it returns.
            if (_syncing || !ShouldHandle(user, item) || !TableSettling.IsSettled(__instance))
            {
                return;
            }

            OfferMap(__instance);
        }

        /// <summary>
        /// True when the settling delay should stop this interaction. Tells the player how long
        /// is left rather than leaving them poking a table that silently does nothing.
        /// </summary>
        private static bool Refused(MapTable table, Humanoid user, ItemDrop.ItemData item)
        {
            if (_syncing || !ShouldHandle(user, item))
            {
                return false;
            }

            double remaining = TableSettling.RemainingSeconds(table);
            if (remaining <= 0d)
            {
                return false;
            }

            user.Message(
                MessageHud.MessageType.Center,
                "The charts are still being drawn (" + TableSettling.DescribeRemaining(remaining) + ")");

            return true;
        }

        private static void PushLocalMapToTable(MapTable table, Switch caller, Humanoid user, ItemDrop.ItemData item)
        {
            if (OnWriteMethod == null)
            {
                return;
            }

            // OnWrite internally calls OnRead(Switch, Humanoid, ItemData, bool) - a different
            // overload from the one patched above - so this cannot recurse through Harmony.
            // The flag only stops OnWrite_Postfix from opening the map a second time.
            _syncing = true;
            try
            {
                OnWriteMethod.Invoke(table, new object[] { caller, user, item });
            }
            catch (Exception e)
            {
                WayfindingPlugin.Log.LogWarning("Cartography table sync failed: " + e);
            }
            finally
            {
                _syncing = false;
            }
        }

        private static bool ShouldHandle(Humanoid user, ItemDrop.ItemData item)
        {
            if (!Settings.Enabled)
            {
                return false;
            }

            // Vanilla treats an item interaction as "not for me" and bails immediately.
            if (item != null)
            {
                return false;
            }

            // Only react for ourselves; every other client runs its own copy of this.
            Player player = user as Player;
            return player != null && player == Player.m_localPlayer;
        }

        private static void OfferMap(MapTable table)
        {
            if (Settings.RequireTableForMap)
            {
                MapAccess.Open(table);
            }
        }
    }
}
