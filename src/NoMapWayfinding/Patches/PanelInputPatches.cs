using HarmonyLib;
using UnityEngine;

namespace NoMapWayfinding.Patches
{
    /// <summary>
    /// While the settings panel is open the player must not also be swinging an axe at whatever
    /// is behind it, and the cursor has to be free to reach the sliders.
    ///
    /// Both are done the way Valheim does them for its own windows rather than by fighting the
    /// input system: Player.TakeInput is the single gate the game already consults for "is a UI
    /// eating input right now", and GameCamera.UpdateMouseCapture is where cursor lock is
    /// decided. Vanilla lists Minimap.IsOpen, InventoryGui, Menu and the rest in exactly these
    /// two places; this adds one more condition to each.
    /// </summary>
    [HarmonyPatch]
    internal static class PanelInputPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "TakeInput")]
        private static void TakeInput_Postfix(ref bool __result)
        {
            if (CompassSettingsPanel.IsOpen)
            {
                __result = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
        private static bool UpdateMouseCapture_Prefix()
        {
            if (!CompassSettingsPanel.IsOpen)
            {
                return true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return false;
        }
    }
}
