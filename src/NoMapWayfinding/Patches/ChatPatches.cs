using HarmonyLib;
using UnityEngine;

namespace NoMapWayfinding.Patches
{
    [HarmonyPatch]
    internal static class ChatPatches
    {
        /// <summary>
        /// Note a shout so the compass can point back at it.
        ///
        /// Chat.OnNewChatMessage is the one place every message arrives with both a position and
        /// a Talker.Type, and it runs once per message, so there is nothing to deduplicate.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Chat), "OnNewChatMessage")]
        private static void OnNewChatMessage_Postfix(long senderID, Vector3 pos, Talker.Type type, UserInfo sender)
        {
            if (!Settings.Enabled || Settings.CompassShoutSeconds <= 0f || type != Talker.Type.Shout)
            {
                return;
            }

            // Your own shout would sit on your own heading and tell you nothing.
            Player player = Player.m_localPlayer;
            if (player != null && senderID == player.GetPlayerID())
            {
                return;
            }

            ShoutMarkers.Record(pos, sender != null ? sender.GetDisplayName() : "");
        }
    }
}
