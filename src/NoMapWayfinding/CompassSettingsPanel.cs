using BepInEx.Configuration;
using UnityEngine;

namespace NoMapWayfinding
{
    /// <summary>
    /// A small in-game panel for tuning the compass colours, drawn with IMGUI so it needs no
    /// external config manager and no part of Valheim's UI hierarchy.
    ///
    /// The point of doing this in game rather than in a text file is the live preview: the
    /// compass reads its settings every frame, so dragging a slider recolours the real thing
    /// behind the panel rather than a swatch pretending to be it.
    /// </summary>
    internal class CompassSettingsPanel : MonoBehaviour
    {
        private const int WindowId = 0x4E4D57; // "NMW"

        public static bool IsOpen { get; private set; }

        private static Rect _window = new Rect(80f, 80f, 300f, 0f);

        private Texture2D _swatch;
        private GUIStyle _headerStyle;
        private bool _savedSaveOnSet;

        private void Update()
        {
            if (Player.m_localPlayer == null)
            {
                Close();
                return;
            }

            if (!Input.GetKeyDown(Settings.SettingsKey) || TypingSomewhere())
            {
                return;
            }

            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>Don't steal the key from chat, the console or a sign.</summary>
        private static bool TypingSomewhere()
        {
            return (Chat.instance != null && Chat.instance.HasFocus())
                || Console.IsVisible()
                || TextInput.IsVisible()
                || Minimap.InTextInput();
        }

        private void Open()
        {
            IsOpen = true;

            // Every slider drag writes to the ConfigEntry so the compass updates live. Without
            // this that would rewrite the config file on every frame of the drag, so batch it
            // into a single save when the panel closes.
            ConfigFile file = PluginConfig.File;
            if (file != null)
            {
                _savedSaveOnSet = file.SaveOnConfigSet;
                file.SaveOnConfigSet = false;
            }
        }

        private void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;

            ConfigFile file = PluginConfig.File;
            if (file != null)
            {
                file.SaveOnConfigSet = _savedSaveOnSet;
                file.Save();
            }
        }

        private void OnDestroy()
        {
            Close();

            if (_swatch != null)
            {
                Destroy(_swatch);
                _swatch = null;
            }
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            EnsureResources();

            // Unlike the compass this must see every event, not just Repaint, or the sliders
            // never receive their mouse input.
            _window = GUILayout.Window(WindowId, _window, DrawWindow, "NoMap Wayfinding");
        }

        private void EnsureResources()
        {
            if (_swatch == null)
            {
                _swatch = new Texture2D(1, 1, TextureFormat.ARGB32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _swatch.SetPixel(0, 0, Color.white);
                _swatch.Apply();
            }

            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(4f);

            Color compass = ColorRow("Compass", CurrentColor(Settings.CompassColor, new Color(1f, 0.97f, 0.88f)));
            Write(PluginConfig.CompassColor, compass);

            GUILayout.Space(6f);

            Color marker = ColorRow("Marker", CurrentColor(Settings.CompassMarkerColor, new Color(1f, 0.94f, 0.78f)));
            Write(PluginConfig.CompassMarkerColor, marker);

            GUILayout.Space(6f);
            GUILayout.Label("Opacity", _headerStyle);
            float opacity = Slider(PluginConfig.CompassOpacity.Value, 0.1f, 1f);
            if (!Mathf.Approximately(opacity, PluginConfig.CompassOpacity.Value))
            {
                PluginConfig.CompassOpacity.Value = opacity;
            }

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset"))
            {
                PluginConfig.CompassColor.Value = (string)PluginConfig.CompassColor.DefaultValue;
                PluginConfig.CompassMarkerColor.Value = (string)PluginConfig.CompassMarkerColor.DefaultValue;
                PluginConfig.CompassOpacity.Value = (float)PluginConfig.CompassOpacity.DefaultValue;
            }

            if (GUILayout.Button("Close"))
            {
                Close();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(
                Settings.SettingsKey + " to reopen. Everything else is in the config file.",
                GUI.skin.label);

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        private Color ColorRow(string label, Color value)
        {
            GUILayout.Label(label + "   " + Hex(value), _headerStyle);

            GUILayout.BeginHorizontal();

            Rect box = GUILayoutUtility.GetRect(44f, 44f, GUILayout.Width(44f), GUILayout.ExpandWidth(false));
            if (Event.current.type == EventType.Repaint)
            {
                Color previous = GUI.color;
                GUI.color = Color.black;
                GUI.DrawTexture(box, _swatch);
                GUI.color = value;
                GUI.DrawTexture(new Rect(box.x + 1f, box.y + 1f, box.width - 2f, box.height - 2f), _swatch);
                GUI.color = previous;
            }

            GUILayout.Space(6f);
            GUILayout.BeginVertical();
            float r = Channel("R", value.r);
            float g = Channel("G", value.g);
            float b = Channel("B", value.b);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            return new Color(r, g, b, 1f);
        }

        private float Channel(string name, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(12f));
            float result = Slider(value, 0f, 1f);
            GUILayout.Label(Mathf.RoundToInt(result * 255f).ToString(), GUILayout.Width(28f));
            GUILayout.EndHorizontal();
            return result;
        }

        private static float Slider(float value, float min, float max)
        {
            return GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(120f));
        }

        private static Color CurrentColor(string text, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(text, out Color parsed) ? parsed : fallback;
        }

        private static string Hex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        /// <summary>Only write when the value actually moved, so idle frames touch nothing.</summary>
        private static void Write(ConfigEntry<string> entry, Color color)
        {
            string hex = Hex(color);
            if (entry.Value != hex)
            {
                entry.Value = hex;
            }
        }
    }
}
