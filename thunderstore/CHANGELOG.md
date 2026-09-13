# Changelog

## 1.1.1

- Fixed your own shouts appearing on the compass. The check compared the routed-RPC peer id
  against a character id - two different numbers, so it never matched.
- Fixed the shout marker never being named. The label was positioned above the icon band, which
  is already clamped to the top of the screen, so at the default offset it was drawn off-screen.
  It now sits on its own line under the compass.

## 1.1.0

- When another player shouts, the compass marks the direction it came from for two minutes,
  using the game's own shout icon. Turning towards a marker names whoever called. Configurable
  with `Compass.ShoutMarkerSeconds`, or `0` to turn it off.

## 1.0.0

First release.

- The minimap is hidden, and a compass ribbon with the current biome takes its place.
- The world map opens only at a cartography table, and closes when you walk away from it.
- Using a table merges exploration in both directions, so a group shares one map.
- Map pins are shared through the table along with the explored ground.
- A newly built table must stand for ten minutes before it can be read, so one cannot be
  carried around as a portable position finder. Configurable, and tables that already existed
  are never affected.
- Optionally, removing a cartography table returns nothing.
- An in-game panel on F7 recolours the compass live.
- Servers running the mod hand their Map and Table settings to modded clients, and can
  optionally turn away players who do not have it.
