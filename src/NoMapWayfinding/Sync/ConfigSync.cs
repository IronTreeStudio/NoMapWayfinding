using HarmonyLib;
using UnityEngine;

namespace NoMapWayfinding.Sync
{
    /// <summary>
    /// Rule distribution between a server and its modded clients.
    ///
    /// The client asks and the server answers, rather than the server pushing on connect. That
    /// avoids hooking the peer handshake - the most version-fragile part of Valheim's netcode -
    /// and it degrades correctly: against a server with no mod the request simply goes
    /// unanswered and the player keeps their own settings.
    /// </summary>
    internal static class ConfigSync
    {
        public const string RequestRpc = "Wayfinding_RequestRules";
        public const string ApplyRpc = "Wayfinding_Rules";

        public static void Register()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null)
            {
                return;
            }

            rpc.Register<ZPackage>(RequestRpc, OnRulesRequested);
            rpc.Register<ZPackage>(ApplyRpc, OnRulesReceived);
        }

        /// <summary>Server side: a modded client is asking what the rules are here.</summary>
        private static void OnRulesRequested(long sender, ZPackage pkg)
        {
            ZNet znet = ZNet.instance;
            if (znet == null || !znet.IsServer())
            {
                return;
            }

            // Asking for the rules is what proves a client has the mod. Record that first: it
            // must not depend on whether this server happens to be distributing rules.
            ModdedClientGate.MarkVerified(sender);

            if (!PluginConfig.EnforceOnClients.Value)
            {
                // Deliberately silent. The client is already treating no reply as "keep your
                // own settings", which is exactly what not enforcing means.
                return;
            }

            ServerRules rules = ServerRules.FromLocalConfig();
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ApplyRpc, rules.Serialize());
            WayfindingPlugin.Log.LogInfo("Sent Wayfinding rules to peer " + sender + ": " + rules);
        }

        /// <summary>Client side: the server has told us how it wants the mod to behave.</summary>
        private static void OnRulesReceived(long sender, ZPackage pkg)
        {
            ZNet znet = ZNet.instance;
            if (znet == null || znet.IsServer())
            {
                return;
            }

            // Routed RPCs can be addressed to any peer, so only take rules from the server.
            ZNetPeer server = znet.GetServerPeer();
            if (server == null || sender != server.m_uid)
            {
                WayfindingPlugin.Log.LogWarning(
                    "Ignoring Wayfinding rules from peer " + sender + ", which is not the server.");
                return;
            }

            ServerRules rules = ServerRules.Deserialize(pkg);
            if (rules == null)
            {
                return;
            }

            ServerRules.Apply(rules);
            WayfindingPlugin.Log.LogInfo("This server sets the Wayfinding rules: " + rules);
        }
    }

    /// <summary>
    /// Registers the RPCs the moment ZRoutedRpc exists. Its constructor is the earliest and
    /// most stable point available on both a client and a dedicated server.
    /// </summary>
    [HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, new[] { typeof(bool) })]
    internal static class ZRoutedRpcConstructorPatch
    {
        private static void Postfix()
        {
            ConfigSync.Register();
        }
    }

    /// <summary>
    /// Client side driver: asks the server for its rules once connected, and drops them again
    /// on disconnect so the player's own settings come back.
    /// </summary>
    internal class ConfigSyncClient : MonoBehaviour
    {
        private const float RetryInterval = 2f;
        private const int MaxAttempts = 5;

        private float _nextAttemptAt;
        private int _attempts;

        private void Update()
        {
            ZNet znet = ZNet.instance;

            if (znet == null)
            {
                // Back to the main menu or single player.
                ServerRules.Clear();
                _attempts = 0;
                return;
            }

            // Hosting our own game: the local config already is the rule.
            if (znet.IsServer())
            {
                _attempts = 0;
                return;
            }

            if (ServerRules.Active || _attempts >= MaxAttempts)
            {
                return;
            }

            if (Time.time < _nextAttemptAt || znet.GetServerPeer() == null || ZRoutedRpc.instance == null)
            {
                return;
            }

            _nextAttemptAt = Time.time + RetryInterval;
            _attempts++;

            ZRoutedRpc.instance.InvokeRoutedRPC(ConfigSync.RequestRpc, new ZPackage());

            if (_attempts == MaxAttempts)
            {
                // Not an error: an unmodded or non-enforcing server never answers.
                WayfindingPlugin.Log.LogInfo(
                    "Server did not send Wayfinding rules; using local settings.");
            }
        }
    }
}
