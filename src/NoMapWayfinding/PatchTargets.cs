using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace NoMapWayfinding
{
    /// <summary>
    /// Several of the methods this mod patches are private, so they are bound by name rather
    /// than by <c>nameof</c> and the compiler cannot catch a rename. Valheim updates do rename
    /// things. Checking the targets up front turns "the mod silently stopped working" into a
    /// single explicit line in the BepInEx log naming exactly what moved.
    /// </summary>
    internal static class PatchTargets
    {
        public static bool Verify()
        {
            var missing = new List<string>();

            Require(missing, "Minimap.SetMapMode",
                AccessTools.Method(typeof(Minimap), "SetMapMode", new[] { typeof(Minimap.MapMode) }));
            Require(missing, "Minimap.Update", AccessTools.Method(typeof(Minimap), "Update"));
            Require(missing, "Minimap.m_smallRoot", (MemberInfo)AccessTools.Field(typeof(Minimap), "m_smallRoot"));
            Require(missing, "Minimap.m_mode", (MemberInfo)AccessTools.Field(typeof(Minimap), "m_mode"));

            var switchCallbackArgs = new[] { typeof(Switch), typeof(Humanoid), typeof(ItemDrop.ItemData) };
            Require(missing, "MapTable.OnRead", AccessTools.Method(typeof(MapTable), "OnRead", switchCallbackArgs));
            Require(missing, "MapTable.OnWrite", AccessTools.Method(typeof(MapTable), "OnWrite", switchCallbackArgs));

            // Used to stop the settings panel from clicking through into the game.
            Require(missing, "Player.TakeInput", AccessTools.Method(typeof(Player), "TakeInput"));
            Require(missing, "GameCamera.UpdateMouseCapture",
                AccessTools.Method(typeof(GameCamera), "UpdateMouseCapture"));

            // Used by the cartography table settling delay and the no-refund option.
            Require(missing, "Piece.SetCreator", AccessTools.Method(typeof(Piece), "SetCreator"));
            Require(missing, "Piece.DropResources", AccessTools.Method(typeof(Piece), "DropResources"));

            // Used to put a marker on the compass when someone shouts.
            Require(missing, "Chat.OnNewChatMessage", AccessTools.Method(typeof(Chat), "OnNewChatMessage"));

            if (missing.Count == 0)
            {
                return true;
            }

            WayfindingPlugin.Log.LogError(
                "NoMap Wayfinding could not find these members in this version of Valheim: "
                + string.Join(", ", missing.ToArray())
                + ". The game has probably been updated; the mod needs a rebuild against the new assemblies.");

            return false;
        }

        private static void Require(List<string> missing, string name, MemberInfo member)
        {
            if (member == null)
            {
                missing.Add(name);
            }
        }
    }
}
