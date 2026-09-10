namespace NoMapWayfinding
{
    using NoMapWayfinding.Sync;

    /// <summary>
    /// The single place the rest of the mod reads settings from. Gameplay rules come from the
    /// server when one is imposing them and from the local config otherwise; presentation is
    /// always local.
    ///
    /// Everything reads through here rather than touching ConfigEntry.Value directly, so there
    /// is no path that silently bypasses a server's rules.
    /// </summary>
    internal static class Settings
    {
        public static bool Enabled =>
            ServerRules.Active ? ServerRules.Current.Enabled : PluginConfig.Enabled.Value;

        public static bool HideMinimap =>
            ServerRules.Active ? ServerRules.Current.HideMinimap : PluginConfig.HideMinimap.Value;

        public static bool RequireTableForMap =>
            ServerRules.Active ? ServerRules.Current.RequireTableForMap : PluginConfig.RequireTableForMap.Value;

        public static bool AutoSyncAtTable =>
            ServerRules.Active ? ServerRules.Current.AutoSyncAtTable : PluginConfig.AutoSyncAtTable.Value;

        public static float TableRange =>
            ServerRules.Active ? ServerRules.Current.TableRange : PluginConfig.TableRange.Value;

        public static float TableSettlingSeconds =>
            ServerRules.Active ? ServerRules.Current.TableSettlingSeconds : PluginConfig.TableSettlingSeconds.Value;

        public static bool TableNoRefund =>
            ServerRules.Active ? ServerRules.Current.TableNoRefund : PluginConfig.TableNoRefund.Value;

        // Presentation. Never synced - see ServerRules.
        public static bool CompassEnabled => PluginConfig.CompassEnabled.Value;
        public static bool CompassShowBiome => PluginConfig.CompassShowBiome.Value;
        public static string CompassColor => PluginConfig.CompassColor.Value;
        public static string CompassMarkerColor => PluginConfig.CompassMarkerColor.Value;
        public static UnityEngine.KeyCode SettingsKey => PluginConfig.SettingsKey.Value;
        public static float CompassWidth => PluginConfig.CompassWidth.Value;
        public static float CompassTopOffset => PluginConfig.CompassTopOffset.Value;
        public static float CompassPixelsPerDegree => PluginConfig.CompassPixelsPerDegree.Value;
        public static float CompassOpacity => PluginConfig.CompassOpacity.Value;
        public static float CompassScale => PluginConfig.CompassScale.Value;
    }
}
