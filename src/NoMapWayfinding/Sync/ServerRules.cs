using UnityEngine;

namespace NoMapWayfinding.Sync
{
    /// <summary>
    /// The gameplay rules a server can impose on modded clients, and the overlay that holds
    /// them while connected.
    ///
    /// Only rules are synced. Compass width, scale, opacity and the rest stay local for good:
    /// they are display preferences on someone else's monitor, and a server has no business
    /// choosing them.
    ///
    /// The overlay deliberately never writes to the client's config file. Leaving a server has
    /// to restore that player's own settings exactly, and a server should not be able to
    /// permanently edit files on a machine it does not own.
    /// </summary>
    internal class ServerRules
    {
        private const int PayloadVersion = 2;

        public bool Enabled = true;
        public bool HideMinimap = true;
        public bool RequireTableForMap = true;
        public bool AutoSyncAtTable = true;
        public float TableRange = 8f;
        public float TableSettlingSeconds;
        public bool TableNoRefund;

        /// <summary>True while a server's rules are overriding the local config.</summary>
        public static bool Active { get; private set; }

        public static ServerRules Current { get; private set; }

        public static ServerRules FromLocalConfig()
        {
            return new ServerRules
            {
                Enabled = PluginConfig.Enabled.Value,
                HideMinimap = PluginConfig.HideMinimap.Value,
                RequireTableForMap = PluginConfig.RequireTableForMap.Value,
                AutoSyncAtTable = PluginConfig.AutoSyncAtTable.Value,
                TableRange = PluginConfig.TableRange.Value,
                TableSettlingSeconds = PluginConfig.TableSettlingSeconds.Value,
                TableNoRefund = PluginConfig.TableNoRefund.Value
            };
        }

        public static void Apply(ServerRules rules)
        {
            Current = rules;
            Active = true;
        }

        public static void Clear()
        {
            if (!Active)
            {
                return;
            }

            Active = false;
            Current = null;
            WayfindingPlugin.Log.LogInfo("Left the server; local Wayfinding settings are in force again.");
        }

        public ZPackage Serialize()
        {
            var pkg = new ZPackage();
            pkg.Write(PayloadVersion);
            pkg.Write(Enabled);
            pkg.Write(HideMinimap);
            pkg.Write(RequireTableForMap);
            pkg.Write(AutoSyncAtTable);
            pkg.Write(TableRange);
            pkg.Write(TableSettlingSeconds);
            pkg.Write(TableNoRefund);
            return pkg;
        }

        /// <summary>Returns null if the payload is unreadable or from an incompatible version.</summary>
        public static ServerRules Deserialize(ZPackage pkg)
        {
            if (pkg == null)
            {
                return null;
            }

            try
            {
                int version = pkg.ReadInt();
                if (version != PayloadVersion)
                {
                    WayfindingPlugin.Log.LogWarning(
                        "Server sent Wayfinding rules version " + version + ", expected " + PayloadVersion
                        + ". Ignoring them and keeping local settings; the server and this client are "
                        + "running different versions of the mod.");
                    return null;
                }

                return new ServerRules
                {
                    Enabled = pkg.ReadBool(),
                    HideMinimap = pkg.ReadBool(),
                    RequireTableForMap = pkg.ReadBool(),
                    AutoSyncAtTable = pkg.ReadBool(),
                    TableRange = Mathf.Clamp(pkg.ReadSingle(), 2f, 64f),
                    TableSettlingSeconds = Mathf.Clamp(pkg.ReadSingle(), 0f, 7200f),
                    TableNoRefund = pkg.ReadBool()
                };
            }
            catch (System.Exception e)
            {
                WayfindingPlugin.Log.LogWarning("Could not read Wayfinding rules from the server: " + e.Message);
                return null;
            }
        }

        public override string ToString()
        {
            return "enabled=" + Enabled
                + " hideMinimap=" + HideMinimap
                + " requireTable=" + RequireTableForMap
                + " autoSync=" + AutoSyncAtTable
                + " range=" + TableRange
                + " settling=" + TableSettlingSeconds
                + " noRefund=" + TableNoRefund;
        }
    }
}
