using BepInEx.Configuration;
using UnityEngine;

namespace NoMapWayfinding
{
    /// <summary>
    /// All settings live in BepInEx\config\com.rynwind.wayfinding.cfg and are read live,
    /// so they can be toggled with a config manager without restarting the game.
    /// </summary>
    internal static class PluginConfig
    {
        /// <summary>Kept so the settings panel can batch a drag into one file write.</summary>
        public static ConfigFile File;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> EnforceOnClients;
        public static ConfigEntry<bool> RequireModdedClients;
        public static ConfigEntry<float> ClientGraceSeconds;

        public static ConfigEntry<bool> HideMinimap;
        public static ConfigEntry<bool> RequireTableForMap;

        public static ConfigEntry<bool> AutoSyncAtTable;
        public static ConfigEntry<float> TableRange;
        public static ConfigEntry<float> TableSettlingSeconds;
        public static ConfigEntry<bool> TableNoRefund;

        public static ConfigEntry<bool> CompassEnabled;
        public static ConfigEntry<float> CompassWidth;
        public static ConfigEntry<float> CompassTopOffset;
        public static ConfigEntry<float> CompassPixelsPerDegree;
        public static ConfigEntry<float> CompassOpacity;
        public static ConfigEntry<float> CompassScale;
        public static ConfigEntry<bool> CompassShowBiome;
        public static ConfigEntry<string> CompassColor;
        public static ConfigEntry<string> CompassMarkerColor;
        public static ConfigEntry<float> CompassShoutSeconds;
        public static ConfigEntry<KeyCode> SettingsKey;

        public static void Bind(ConfigFile cfg)
        {
            File = cfg;

            Enabled = cfg.Bind("General", "Enabled", true,
                "Master switch. When false the mod does nothing and Valheim behaves normally.");

            EnforceOnClients = cfg.Bind("Server", "EnforceOnClients", true,
                "Server side only. When this instance is a server, send its Map and Table settings to " +
                "connecting clients that have the mod, overriding their local ones for as long as they " +
                "are connected. Compass appearance is never sent - that stays each player's own choice. " +
                "This cannot affect players who do not have the mod installed: the minimap is drawn on " +
                "their machine, not the server's.");

            RequireModdedClients = cfg.Bind("Server", "RequireModdedClients", false,
                "Off by default; opt in only if you run a server and want it. Disconnects players who " +
                "do not have this mod. Be aware that hosting a game from your own client makes you a " +
                "server, so switching this on there turns away friends who have not installed it. " +
                "The vanilla NoMap world key is usually the better tool: it costs unmodded clients " +
                "the map through the game's own code without excluding anyone. Treat this as keeping " +
                "honest players honest rather than as a guarantee: a mod cannot enforce anything on " +
                "a machine it does not control.");

            ClientGraceSeconds = cfg.Bind("Server", "ClientGraceSeconds", 20f,
                new ConfigDescription(
                    "Server side only. How long a joining player has to identify themselves as having " +
                    "the mod before being turned away. The clock starts once Valheim's own handshake " +
                    "finishes, so this measures the mod, not the connection.",
                    new AcceptableValueRange<float>(5f, 120f)));

            HideMinimap = cfg.Bind("Map", "HideMinimap", true,
                "Hide the corner minimap. Exploration still records normally, it is just never drawn.");

            RequireTableForMap = cfg.Bind("Map", "RequireTableForMap", true,
                "Block the full-screen map key. The world map can then only be opened by using a cartography table.");

            AutoSyncAtTable = cfg.Bind("Table", "AutoSyncOnUse", true,
                "Using a cartography table merges in both directions at once: the table's exploration is added to " +
                "yours and yours is added to the table. One interaction instead of separate read and write.");

            TableRange = cfg.Bind("Table", "Range", 8f,
                new ConfigDescription(
                    "How far you can walk from the cartography table before the map closes, in metres.",
                    new AcceptableValueRange<float>(2f, 64f)));

            TableSettlingSeconds = cfg.Bind("Table", "SettlingSeconds", 600f,
                new ConfigDescription(
                    "How long a newly built cartography table must stand before it can be read, in " +
                    "seconds. This stops a table being used as a portable position finder - carry the " +
                    "materials, build one to see where you are, dismantle it and walk on. A table you " +
                    "built and left standing is unaffected. Set to 0 to allow reading immediately. " +
                    "Tables that already existed when this was installed are never affected.",
                    new AcceptableValueRange<float>(0f, 7200f)));

            TableNoRefund = cfg.Bind("Table", "NoRefundOnRemove", false,
                "Give nothing back when a cartography table is removed, so using one as a disposable " +
                "position finder costs a full table every time. This also applies when a table is " +
                "destroyed rather than dismantled, since the game routes both through the same code.");

            CompassEnabled = cfg.Bind("Compass", "Enabled", true,
                "Draw a compass strip at the top of the screen in place of the minimap.");

            CompassWidth = cfg.Bind("Compass", "Width", 460f,
                new ConfigDescription("Width of the compass strip in pixels.",
                    new AcceptableValueRange<float>(160f, 1600f)));

            CompassTopOffset = cfg.Bind("Compass", "TopOffset", 16f,
                new ConfigDescription("Distance from the top of the screen in pixels.",
                    new AcceptableValueRange<float>(0f, 400f)));

            CompassPixelsPerDegree = cfg.Bind("Compass", "PixelsPerDegree", 3.2f,
                new ConfigDescription("Horizontal scale. Higher spreads the headings further apart.",
                    new AcceptableValueRange<float>(1f, 12f)));

            CompassOpacity = cfg.Bind("Compass", "Opacity", 0.85f,
                new ConfigDescription("Opacity of the compass at its centre.",
                    new AcceptableValueRange<float>(0.1f, 1f)));

            CompassScale = cfg.Bind("Compass", "Scale", 2f,
                new ConfigDescription("Overall size of the compass. Lower it if the ribbon feels too big.",
                    new AcceptableValueRange<float>(0.5f, 3f)));

            CompassShowBiome = cfg.Bind("Compass", "ShowBiomeName", true,
                "Show the current biome under the compass. The vanilla biome label sits inside the minimap " +
                "panel, so hiding the minimap hides it too; this puts that information back.");

            CompassColor = cfg.Bind("Compass", "Color", "#FFF7E0",
                "Colour of the headings, the tick marks and the biome name. Either a hex value such as " +
                "#FFF7E0 or #RRGGBB, or a colour name such as cyan. Transparency comes from Opacity " +
                "below rather than from this value.");

            CompassMarkerColor = cfg.Bind("Compass", "MarkerColor", "#FFF0C7",
                "Colour of the fixed marker in the centre of the compass, the one that shows the way " +
                "you are actually facing. Give it something distinct from Color to make it easier to " +
                "pick out at a glance.");

            SettingsKey = cfg.Bind("Compass", "SettingsKey", KeyCode.F7,
                "Opens a small panel for changing the compass colours in game, with the compass " +
                "recolouring live behind it. Set to None to disable the panel entirely.");

            CompassShoutSeconds = cfg.Bind("Compass", "ShoutMarkerSeconds", 120f,
                new ConfigDescription(
                    "When another player shouts, mark the direction it came from on the compass for " +
                    "this many seconds, using the game's own shout icon. The marker stays where the " +
                    "shout was, so it points at where they called from rather than following them. " +
                    "Set to 0 to turn it off.",
                    new AcceptableValueRange<float>(0f, 600f)));
        }
    }
}
