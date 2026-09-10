using UnityEngine;

namespace NoMapWayfinding
{
    /// <summary>
    /// A sliding compass ribbon drawn where the minimap used to be.
    ///
    /// This is deliberately IMGUI rather than uGUI. Parenting a Canvas into Valheim's HUD
    /// hierarchy means depending on the exact shape of that hierarchy, which Iron Gate
    /// reworks between updates; an OnGUI overlay only depends on Screen and the game font,
    /// so it survives game patches that would otherwise leave a dangling widget behind.
    /// </summary>
    internal class CompassHud : MonoBehaviour
    {
        private const float BaseHeight = 30f;
        private const int MinorStepDegrees = 15;

        private static readonly string[] Headings = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        private Texture2D _pixel;
        private readonly CachedColor _color = new CachedColor(new Color(1f, 0.97f, 0.88f));
        private readonly CachedColor _markerColor = new CachedColor(new Color(1f, 0.94f, 0.78f));
        private string _biomeName = "";
        private float _biomeChangedAt = -99f;
        private GUIStyle _labelStyle;
        private Font _font;
        private bool _fontSearched;

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !ShouldDraw())
            {
                return;
            }

            EnsureResources();
            Draw(CurrentHeading());

            if (Settings.CompassShowBiome)
            {
                DrawBiome();
            }
        }

        private void OnDestroy()
        {
            if (_pixel != null)
            {
                Destroy(_pixel);
                _pixel = null;
            }
        }

        /// <summary>
        /// Colours come from the config as text - a hex value or a colour name - rather than as a
        /// typed Color, because BepInEx 5 has no built-in converter for UnityEngine.Color and a
        /// string is the one form every config manager can edit. Parsed results are cached: the
        /// config is read live on every frame and TryParseHtmlString is not free.
        /// </summary>
        private sealed class CachedColor
        {
            private readonly Color _fallback;
            private string _text;
            private Color _value;
            private bool _warned;

            public CachedColor(Color fallback)
            {
                _fallback = fallback;
                _value = fallback;
            }

            public Color Get(string text)
            {
                if (text == _text)
                {
                    return _value;
                }

                _text = text;

                if (ColorUtility.TryParseHtmlString(text, out Color parsed))
                {
                    _value = parsed;
                    _warned = false;
                    return _value;
                }

                _value = _fallback;

                if (!_warned)
                {
                    _warned = true;
                    WayfindingPlugin.Log.LogWarning(
                        "Could not read the compass colour \"" + text + "\". Expected something like "
                        + "#FFF7E0 or a colour name such as cyan. Using the default for now.");
                }

                return _value;
            }
        }

        /// <summary>Alpha is owned by the Opacity setting, so only the RGB of the configured colour is used.</summary>
        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static bool ShouldDraw()
        {
            if (!Settings.Enabled || !Settings.CompassEnabled)
            {
                return false;
            }

            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                return false;
            }

            Minimap map = Minimap.instance;
            if (map == null || map.m_mode == Minimap.MapMode.Large)
            {
                return false;
            }

            Hud hud = Hud.instance;
            if (hud == null || hud.m_userHidden || !hud.IsVisible())
            {
                return false;
            }

            return GameCamera.instance != null;
        }

        private static float CurrentHeading()
        {
            // Camera yaw rather than body yaw: it is what the player is actually looking at,
            // and it matches how the vanilla minimap orients its player marker.
            GameCamera camera = GameCamera.instance;
            if (camera != null)
            {
                return camera.transform.eulerAngles.y;
            }

            Player player = Player.m_localPlayer;
            return player != null ? player.transform.eulerAngles.y : 0f;
        }

        private void EnsureResources()
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(1, 1, TextureFormat.ARGB32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _pixel.SetPixel(0, 0, Color.white);
                _pixel.Apply();
            }

            if (!_fontSearched)
            {
                _fontSearched = true;
                _font = FindGameFont();
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = false
                };

                if (_font != null)
                {
                    _labelStyle.font = _font;
                }
            }
        }

        /// <summary>Borrow Valheim's own typeface so the strip does not read as bolted on.</summary>
        private static Font FindGameFont()
        {
            Font fallback = null;

            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font == null || string.IsNullOrEmpty(font.name))
                {
                    continue;
                }

                string name = font.name.ToLowerInvariant();
                if (name.Contains("norsebold"))
                {
                    return font;
                }

                if (name.Contains("norse") || name.Contains("averia"))
                {
                    fallback = font;
                }
            }

            return fallback;
        }

        private void Draw(float heading)
        {
            float scale = Settings.CompassScale;
            float width = Settings.CompassWidth * scale;
            float height = BaseHeight * scale;
            float centre = width * 0.5f;
            float pixelsPerDegree = Settings.CompassPixelsPerDegree * scale;
            float opacity = Settings.CompassOpacity;

            var strip = new Rect(
                Mathf.Round((Screen.width - width) * 0.5f),
                Mathf.Round(Settings.CompassTopOffset * scale),
                width,
                height);

            Color previousColor = GUI.color;
            _labelStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(15f * scale));

            GUI.BeginGroup(strip);

            for (int degrees = 0; degrees < 360; degrees += MinorStepDegrees)
            {
                float offset = Mathf.DeltaAngle(heading, degrees) * pixelsPerDegree;
                float x = centre + offset;

                if (x < -30f * scale || x > width + 30f * scale)
                {
                    continue;
                }

                // Fade toward the ends so the ribbon dissolves instead of being cut off.
                float distanceFromCentre = Mathf.Abs(offset) / centre;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - distanceFromCentre) * 2.2f));
                if (fade <= 0.01f)
                {
                    continue;
                }

                bool isCardinal = degrees % 45 == 0;
                float alpha = opacity * fade;

                if (isCardinal)
                {
                    DrawLabel(Headings[degrees / 45], x, height, scale, alpha);
                }
                else
                {
                    DrawTick(x, height, scale, alpha * 0.55f);
                }
            }

            GUI.EndGroup();

            // Fixed centre marker, drawn outside the group so it is never faded.
            GUI.color = WithAlpha(_markerColor.Get(Settings.CompassMarkerColor), opacity);
            GUI.DrawTexture(
                new Rect(strip.x + centre - 1f, strip.y + strip.height - 7f * scale, 2f, 7f * scale),
                _pixel);

            GUI.color = previousColor;
        }

        private void DrawTick(float x, float height, float scale, float alpha)
        {
            GUI.color = WithAlpha(_color.Get(Settings.CompassColor), alpha);
            GUI.DrawTexture(new Rect(x - 0.5f, height - 5f * scale, 1f, 5f * scale), _pixel);
        }

        /// <summary>
        /// Valheim's biome label lives inside the minimap panel, so hiding that panel hides the
        /// label with it. Redraw it under the ribbon, including the brief pulse the vanilla
        /// label plays when the biome changes.
        /// </summary>
        private void DrawBiome()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            string name = player.GetCurrentBiomeData().GetName();
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (name != _biomeName)
            {
                _biomeName = name;
                _biomeChangedAt = Time.realtimeSinceStartup;
            }

            float scale = Settings.CompassScale;
            float sinceChange = Time.realtimeSinceStartup - _biomeChangedAt;
            float pulse = 1f + 0.35f * Mathf.Exp(-6f * sinceChange);

            _labelStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(14f * scale * pulse));

            var rect = new Rect(
                0f,
                Mathf.Round(Settings.CompassTopOffset * scale + BaseHeight * scale + 2f * scale),
                Screen.width,
                20f * scale);

            Color previousColor = GUI.color;
            float alpha = Settings.CompassOpacity;

            GUI.color = new Color(0f, 0f, 0f, alpha * 0.7f);
            _labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), _biomeName, _labelStyle);

            GUI.color = WithAlpha(_color.Get(Settings.CompassColor), alpha);
            GUI.Label(rect, _biomeName, _labelStyle);

            GUI.color = previousColor;
        }

        private void DrawLabel(string text, float x, float height, float scale, float alpha)
        {
            float halfWidth = 24f * scale;
            var rect = new Rect(x - halfWidth, 0f, halfWidth * 2f, height - 6f * scale);

            // Drop shadow first; Valheim's HUD text is legible over snow and sky the same way.
            GUI.color = new Color(0f, 0f, 0f, alpha * 0.7f);
            _labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _labelStyle);

            GUI.color = WithAlpha(_color.Get(Settings.CompassColor), alpha);
            GUI.Label(rect, text, _labelStyle);
        }
    }
}
