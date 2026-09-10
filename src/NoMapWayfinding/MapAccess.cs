using UnityEngine;

namespace NoMapWayfinding
{
    /// <summary>
    /// Tracks whether the player has earned the right to see the full-screen map.
    /// Access is granted by using a cartography table and is revoked when the map is
    /// closed, when the player walks out of range of that table, or when they die.
    /// </summary>
    internal static class MapAccess
    {
        public static bool Granted { get; private set; }

        private static Vector3 _tablePosition;

        public static void Open(MapTable table)
        {
            Minimap map = Minimap.instance;
            if (map == null || table == null)
            {
                return;
            }

            _tablePosition = table.transform.position;

            // Granted must be set before SetMapMode, otherwise the prefix that gates the
            // large map will downgrade the request we are about to make.
            Granted = true;
            // SetMapMode(Large) zeroes m_mapOffset, which already centres the view on the
            // player, so there is nothing further to do here.
            map.SetMapMode(Minimap.MapMode.Large);
        }

        public static void Close()
        {
            if (!Granted)
            {
                return;
            }

            Granted = false;

            Minimap map = Minimap.instance;
            if (map != null && map.m_mode == Minimap.MapMode.Large)
            {
                map.SetMapMode(Minimap.MapMode.Small);
            }
        }

        /// <summary>Called once per frame from the Minimap.Update postfix.</summary>
        public static void Tick()
        {
            if (!Granted)
            {
                return;
            }

            Minimap map = Minimap.instance;
            if (map == null)
            {
                Granted = false;
                return;
            }

            // The player closed the map themselves; vanilla already put us back in Small.
            if (map.m_mode != Minimap.MapMode.Large)
            {
                Granted = false;
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                Close();
                return;
            }

            float range = Mathf.Max(1f, Settings.TableRange);
            if (Vector3.Distance(player.transform.position, _tablePosition) > range)
            {
                Close();
            }
        }
    }
}
