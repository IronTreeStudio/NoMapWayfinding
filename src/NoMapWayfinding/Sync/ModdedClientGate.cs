using System.Collections.Generic;
using UnityEngine;

namespace NoMapWayfinding.Sync
{
    /// <summary>
    /// Server side admission control: refuse players who do not have the mod.
    ///
    /// Without this, server rules only bind clients that chose to install the mod, which means
    /// they bind nobody who did not want to be bound. A player who simply never installs it
    /// keeps their minimap.
    ///
    /// The signal is one the mod already produces: a modded client asks the server for its rules
    /// as soon as it connects, and a vanilla client never does. So there is no extra handshake
    /// to design and nothing bolted onto Valheim's peer negotiation, which is the most
    /// version-fragile part of its netcode. Anyone who has not identified themselves within the
    /// grace period is turned away.
    ///
    /// This raises the bar; it is not a guarantee. Rules enforced on the client can always be
    /// defeated by someone willing to edit the assembly or answer the handshake themselves.
    /// Treat it as keeping honest players honest, not as a security boundary.
    /// </summary>
    internal static class ModdedClientGate
    {
        /// <summary>How long after sending the error to let it reach the client before hanging up.</summary>
        private const float DisconnectDelay = 1f;

        private static readonly HashSet<long> Verified = new HashSet<long>();
        private static readonly Dictionary<long, float> FirstSeen = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> Rejecting = new Dictionary<long, float>();

        /// <summary>
        /// Called the moment a client asks for the rules, which only the mod does.
        ///
        /// Deliberately recorded before any EnforceOnClients check: a server that distributes no
        /// rules but still requires the mod must not end up kicking everyone.
        /// </summary>
        public static void MarkVerified(long peerId)
        {
            Verified.Add(peerId);
        }

        public static void Tick()
        {
            ZNet znet = ZNet.instance;
            if (znet == null || !znet.IsServer())
            {
                Forget();
                return;
            }

            List<ZNetPeer> peers = znet.GetConnectedPeers();
            var present = new HashSet<long>();

            foreach (ZNetPeer peer in peers)
            {
                // A peer has no uid until it has finished Valheim's own handshake. Starting the
                // clock before then would penalise a slow connection rather than a missing mod.
                if (peer == null || peer.m_uid == 0L)
                {
                    continue;
                }

                present.Add(peer.m_uid);
                Consider(znet, peer);
            }

            Prune(present);
        }

        private static void Consider(ZNet znet, ZNetPeer peer)
        {
            long uid = peer.m_uid;

            if (Rejecting.TryGetValue(uid, out float rejectedAt))
            {
                if (Time.time - rejectedAt >= DisconnectDelay)
                {
                    znet.Disconnect(peer);
                    Rejecting.Remove(uid);
                }

                return;
            }

            if (Verified.Contains(uid))
            {
                return;
            }

            if (!FirstSeen.ContainsKey(uid))
            {
                FirstSeen[uid] = Time.time;
                return;
            }

            if (!PluginConfig.RequireModdedClients.Value)
            {
                return;
            }

            if (Time.time - FirstSeen[uid] < Mathf.Max(5f, PluginConfig.ClientGraceSeconds.Value))
            {
                return;
            }

            Reject(peer);
        }

        private static void Reject(ZNetPeer peer)
        {
            string who = string.IsNullOrEmpty(peer.m_playerName)
                ? "peer " + peer.m_uid
                : peer.m_playerName;

            WayfindingPlugin.Log.LogWarning(
                "Refusing " + who + ": this server requires the NoMap Wayfinding mod and that client did "
                + "not identify itself as having it. Set Server.RequireModdedClients to false to "
                + "allow unmodded players.");

            // ErrorVersion is handled by vanilla ZNet.RPC_Error, so even a client without the mod
            // gets a real error screen instead of an unexplained drop. It is the closest thing
            // Valheim has to "you are missing something you need to play here".
            try
            {
                if (peer.m_rpc != null && peer.m_rpc.IsConnected())
                {
                    peer.m_rpc.Invoke("Error", (int)ZNet.ConnectionStatus.ErrorVersion);
                }
            }
            catch (System.Exception e)
            {
                WayfindingPlugin.Log.LogWarning("Could not send the rejection notice: " + e.Message);
            }

            // Hang up on a later tick. Disconnect disposes the peer, which would discard the
            // message we just queued.
            Rejecting[peer.m_uid] = Time.time;
        }

        private static void Prune(HashSet<long> present)
        {
            Drop(FirstSeen, present);
            Drop(Rejecting, present);

            Verified.RemoveWhere(uid => !present.Contains(uid));
        }

        private static void Drop(Dictionary<long, float> map, HashSet<long> present)
        {
            if (map.Count == 0)
            {
                return;
            }

            var gone = new List<long>();
            foreach (KeyValuePair<long, float> entry in map)
            {
                if (!present.Contains(entry.Key))
                {
                    gone.Add(entry.Key);
                }
            }

            foreach (long uid in gone)
            {
                map.Remove(uid);
            }
        }

        private static void Forget()
        {
            if (Verified.Count == 0 && FirstSeen.Count == 0 && Rejecting.Count == 0)
            {
                return;
            }

            Verified.Clear();
            FirstSeen.Clear();
            Rejecting.Clear();
        }
    }

    /// <summary>
    /// Drives the gate. Runs on every instance because a player hosting their own game is a
    /// server too; it does nothing until ZNet says this instance is one.
    /// </summary>
    internal class ServerGate : MonoBehaviour
    {
        private const float Interval = 1f;

        private float _nextTickAt;

        private void Update()
        {
            if (Time.time < _nextTickAt)
            {
                return;
            }

            _nextTickAt = Time.time + Interval;

            if (ZNet.instance == null)
            {
                WorldRules.Reset();
                ModdedClientGate.Tick();
                return;
            }

            WorldRules.AnnounceOnce();
            ModdedClientGate.Tick();
        }
    }
}
