namespace NoMapWayfinding.Sync
{
    /// <summary>
    /// Reports whether the world itself forbids the map, and says so once in the log.
    ///
    /// A server can set the vanilla NoMap global key, which costs every client their minimap and
    /// map key through Iron Gate's own code - unmodded clients included. That is real enforcement
    /// rather than a rule a client agrees to follow, and it changes what joining without the mod
    /// feels like, so it is worth stating plainly at startup instead of leaving an admin to infer
    /// it from player complaints.
    /// </summary>
    internal static class WorldRules
    {
        private static bool _announced;

        public static bool NoMap =>
            ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoMap);

        public static void AnnounceOnce()
        {
            if (_announced || ZoneSystem.instance == null)
            {
                return;
            }

            _announced = true;
            bool noMap = NoMap;
            bool isServer = ZNet.instance != null && ZNet.instance.IsServer();

            if (isServer)
            {
                WayfindingPlugin.Log.LogInfo(noMap
                    ? "This world has the NoMap global key set. Every client loses the minimap and "
                      + "the map key through the game's own code, so Server.RequireModdedClients is "
                      + "optional here: players without the mod can join, they just cannot see a map."
                    : "This world does not have the NoMap global key set, so players without the mod "
                      + "keep their minimap. Setting it (world modifiers, or setglobalkey NoMap) is the "
                      + "only way to stop that without turning them away.");
                return;
            }

            if (noMap)
            {
                WayfindingPlugin.Log.LogInfo(
                    "This world forbids the map. Wayfinding will still open it at a cartography table.");
            }
        }

        public static void Reset()
        {
            _announced = false;
        }
    }
}
