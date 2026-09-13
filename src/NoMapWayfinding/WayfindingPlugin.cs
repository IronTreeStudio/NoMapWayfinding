using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using NoMapWayfinding.Patches;
using NoMapWayfinding.Sync;

namespace NoMapWayfinding
{
    /// <summary>
    /// Loads in both the game client and the dedicated server, but does different work in each.
    ///
    /// On a client it hides the minimap, gates the world map and draws the compass. On a server
    /// it does none of that - there is no HUD to change - and only answers modded clients that
    /// ask what the rules are here.
    ///
    /// Note what a server cannot do: the minimap is drawn client-side, so a player without this
    /// mod installed keeps their minimap no matter what the server says. Server rules bind
    /// modded clients only.
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public class WayfindingPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.rynwind.nomapwayfinding";
        public const string Name = "NoMap Wayfinding";
        public const string Version = "1.1.1";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        /// <summary>
        /// Valheim's dedicated server runs headless, so it has no graphics device. Checked here
        /// rather than through ZNet.IsDedicated because ZNet does not exist yet at plugin load.
        /// </summary>
        private static bool IsHeadless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private void Awake()
        {
            Log = Logger;
            PluginConfig.Bind(OpenConfigFile());

            _harmony = new Harmony(Guid);

            // Rule distribution and admission control run in both roles: a player hosting their
            // own game is a server too, and neither does anything until ZNet says so.
            _harmony.PatchAll(typeof(ZRoutedRpcConstructorPatch));
            gameObject.AddComponent<ServerGate>();

            if (IsHeadless)
            {
                Log.LogInfo(Name + " " + Version + " loaded in server mode.");
                Log.LogInfo(PluginConfig.EnforceOnClients.Value
                    ? "  Rules: modded clients will be given this server's Map and Table settings."
                    : "  Rules: not enforced; clients keep their own settings.");
                Log.LogInfo(PluginConfig.RequireModdedClients.Value
                    ? "  Access: players WITHOUT the mod will be refused after "
                      + PluginConfig.ClientGraceSeconds.Value + "s. Set Server.RequireModdedClients"
                      + " to false to allow them."
                    : "  Access: players without the mod are allowed, and will keep their minimap.");
                return;
            }

            if (!PatchTargets.Verify())
            {
                return;
            }

            _harmony.PatchAll(typeof(MinimapPatches));
            _harmony.PatchAll(typeof(MapTablePatches));
            _harmony.PatchAll(typeof(PanelInputPatches));
            _harmony.PatchAll(typeof(PiecePatches));
            _harmony.PatchAll(typeof(ChatPatches));

            gameObject.AddComponent<CompassHud>();
            gameObject.AddComponent<CompassSettingsPanel>();
            gameObject.AddComponent<ConfigSyncClient>();

            Log.LogInfo(Name + " " + Version + " loaded.");
        }

        /// <summary>
        /// BepInEx names a plugin's config file after its GUID, which would give
        /// com.rynwind.nomapwayfinding.cfg. Build the ConfigFile by hand instead so it lands
        /// under a name someone would actually go looking for.
        /// </summary>
        private ConfigFile OpenConfigFile()
        {
            string path = Path.Combine(Paths.ConfigPath, "nomapwayfinding.cfg");
            string legacy = Path.Combine(Paths.ConfigPath, Guid + ".cfg");

            // Carry over whatever the old GUID-named file held, so renaming it does not quietly
            // reset someone's tuning. Copied rather than moved: deleting a file we did not have
            // to delete is the worse failure, and a stale copy costs a few kilobytes.
            try
            {
                if (!File.Exists(path) && File.Exists(legacy))
                {
                    File.Copy(legacy, path);
                    Log.LogInfo("Carried settings over from " + Path.GetFileName(legacy)
                        + " into nomapwayfinding.cfg. The old file is no longer read and can be deleted.");
                }
            }
            catch (Exception e)
            {
                Log.LogWarning("Could not carry settings over from the old config file: " + e.Message);
            }

            return new ConfigFile(path, saveOnInit: true, Info.Metadata);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
