using System.Collections.Generic;
using UnityEngine;

namespace NoMapWayfinding
{
    /// <summary>
    /// Remembers where people shouted from, so the compass can point at it for a while.
    ///
    /// Valheim already turns shouts into map pins, but it derives them from the floating text
    /// above the shouter's head and drops them the moment that text fades - seconds, not minutes.
    /// A marker meant to be navigated towards has to outlive the message, so this keeps its own
    /// list rather than reading the game's.
    ///
    /// Positions are fixed at the moment of the shout. The marker says "someone called for help
    /// from over there", not "here is where they are now".
    /// </summary>
    internal static class ShoutMarkers
    {
        /// <summary>Enough for a busy raid without turning the ribbon into a wall of icons.</summary>
        private const int MaxTracked = 8;

        /// <summary>Seconds of fade at the end, so a marker visibly runs out rather than blinking away.</summary>
        public const float FadeSeconds = 10f;

        internal class Marker
        {
            public Vector3 Position;
            public string Name;
            public float ExpiresAt;
        }

        private static readonly List<Marker> Markers = new List<Marker>();

        public static void Record(Vector3 position, string name)
        {
            float lifetime = Settings.CompassShoutSeconds;
            if (lifetime <= 0f)
            {
                return;
            }

            Markers.Add(new Marker
            {
                Position = position,
                Name = string.IsNullOrEmpty(name) ? "" : name,
                ExpiresAt = Time.time + lifetime
            });

            // Oldest first, so trimming from the front drops the stalest marker.
            while (Markers.Count > MaxTracked)
            {
                Markers.RemoveAt(0);
            }
        }

        public static List<Marker> Active()
        {
            float now = Time.time;
            for (int i = Markers.Count - 1; i >= 0; i--)
            {
                if (Markers[i].ExpiresAt <= now)
                {
                    Markers.RemoveAt(i);
                }
            }

            return Markers;
        }

        /// <summary>1 while the marker is fresh, easing to 0 over its last seconds.</summary>
        public static float Opacity(Marker marker)
        {
            float remaining = marker.ExpiresAt - Time.time;
            if (remaining >= FadeSeconds)
            {
                return 1f;
            }

            return Mathf.Clamp01(remaining / FadeSeconds);
        }

        public static void Clear()
        {
            Markers.Clear();
        }
    }
}
