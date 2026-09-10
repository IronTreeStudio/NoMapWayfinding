using UnityEngine;

namespace NoMapWayfinding
{
    /// <summary>
    /// Stops a cartography table being used as a portable position finder: carry the materials,
    /// build a table wherever you are, read your position off it, dismantle it and walk on.
    ///
    /// A table has to have stood for a while before it can be read. One you built and left
    /// standing behaves exactly as before; one you plant to check where you are does not. Someone
    /// can still camp beside a fresh table and wait it out, but that is a real cost rather than a
    /// drive-by.
    ///
    /// The stamp lives in the table's own ZDO, so it persists and replicates to everyone.
    /// A table with no stamp counts as settled, which grandfathers every table that already
    /// existed before this was installed rather than blacking them all out on first login.
    /// </summary>
    internal static class TableSettling
    {
        private static readonly int PlacedAtKey = "nmw_tablePlacedAt".GetStableHashCode();

        /// <summary>Called from the Piece.SetCreator postfix, which vanilla runs once at placement.</summary>
        public static void StampPlacement(MapTable table)
        {
            if (table == null)
            {
                return;
            }

            ZNetView view = table.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner())
            {
                return;
            }

            if (ZNet.instance == null)
            {
                return;
            }

            view.GetZDO().Set(PlacedAtKey, (long)ZNet.instance.GetTimeSeconds());
        }

        public static bool IsSettled(MapTable table)
        {
            return RemainingSeconds(table) <= 0d;
        }

        /// <summary>Seconds still to wait, or zero if the table is ready.</summary>
        public static double RemainingSeconds(MapTable table)
        {
            float required = Settings.TableSettlingSeconds;
            if (required <= 0f || table == null || ZNet.instance == null)
            {
                return 0d;
            }

            ZNetView view = table.GetComponent<ZNetView>();
            if (view == null || !view.IsValid())
            {
                return 0d;
            }

            long placedAt = view.GetZDO().GetLong(PlacedAtKey, 0L);
            if (placedAt == 0L)
            {
                // Placed before this mod existed. Leave it alone.
                return 0d;
            }

            double elapsed = ZNet.instance.GetTimeSeconds() - placedAt;

            // A negative age means the clock moved under us rather than that the table is new.
            // Fail open: locking someone out of their own base table is worse than a missed check.
            if (elapsed < 0d)
            {
                return 0d;
            }

            double remaining = required - elapsed;
            return remaining > 0d ? remaining : 0d;
        }

        public static string DescribeRemaining(double seconds)
        {
            int whole = Mathf.CeilToInt((float)seconds);
            if (whole < 60)
            {
                return whole + "s";
            }

            int minutes = whole / 60;
            int rest = whole % 60;
            return rest == 0 ? minutes + "m" : minutes + "m " + rest + "s";
        }
    }
}
