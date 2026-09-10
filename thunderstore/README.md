# NoMap Wayfinding

Takes the minimap away and puts the world map where it belongs: on a cartography table,
showing everything your group has found between them.

![Compass](https://raw.githubusercontent.com/IronTreeStudio/NoMapWayfinding/main/media/VHCompassDefaultColor-NoMapWayfinderMod.png)

## What it does

- **No minimap.** You navigate by looking at the world.
- **A compass instead**, with the biome you are standing in named underneath it.
- **The map key does nothing.** Walk up to a cartography table and the map opens; walk away
  and it closes.
- **One touch shares everything.** Using a table merges exploration in both directions, so
  anyone who visits leaves with everything the group has found.
- **Tables are not a portable GPS.** A newly built table has to stand a while before it can
  be read, so one cannot be carried around, planted to check your position, and dismantled
  again.

Exploration is still recorded the whole time you are out there. You just cannot look at it
until you reach a table.

## The map, at a table

![The world map at a cartography table](https://raw.githubusercontent.com/IronTreeStudio/NoMapWayfinding/main/media/VHCartographyTable-NoMapWayfinderMod.png)

Everything the group has explored, merged. Pins are shared too, so a marker one player left
on a hidden crypt is waiting for the rest of them at the table.

## Tuning the compass without leaving the game

Press **F7** for a panel with sliders. The compass recolours live behind it, so you can see
what you are choosing rather than guessing at hex values in a text file.

![Compass recoloured](https://raw.githubusercontent.com/IronTreeStudio/NoMapWayfinding/main/media/VHCompassTeal-NoMapWayfinderMod.png)

## The settling delay

![The settling delay message](https://raw.githubusercontent.com/IronTreeStudio/NoMapWayfinding/main/media/VHSettlingDelay-NoMapWayfinderMod.png)

A table you built and left standing is unaffected, and so is every table that already existed
before you installed the mod. Set `Table.SettlingSeconds` to `0` if you would rather not have
it at all.

## Install

Use a mod manager, or copy `NoMapWayfinding.dll` into `<Valheim>\BepInEx\plugins\`.

Settings appear at `<Valheim>\BepInEx\config\nomapwayfinding.cfg` after the first launch and
are read live, so a config manager can change them without a restart.

## Multiplayer

Client-side. It changes no world state and no network protocol, so it works on vanilla servers
and alongside players who are not running it.

Install it on a dedicated server as well and the server hands its `Map` and `Table` settings to
connecting players who have the mod, for as long as they are connected. Compass appearance is
never sent - that stays each player's own.

**A server cannot hide the minimap of a player who does not have the mod.** The minimap is drawn
on their machine. If you want the rule to hold for everyone, set the vanilla **NoMap** world key:
the game itself then removes the minimap and the map key from every client, unmodded ones
included, and this mod adds back the only thing it cannot do - reading the shared map at a table.

## Settings

| Setting | Default | Effect |
| --- | --- | --- |
| `General.Enabled` | `true` | Master switch. Off means vanilla behaviour. |
| `Map.HideMinimap` | `true` | Never draw the corner minimap. |
| `Map.RequireTableForMap` | `true` | The map key stops working; only a table opens the map. |
| `Table.AutoSyncOnUse` | `true` | Merge exploration both ways in one interaction. |
| `Table.Range` | `8` | Metres you can walk from the table before the map closes. |
| `Table.SettlingSeconds` | `600` | How long a new table must stand before it can be read. |
| `Table.NoRefundOnRemove` | `false` | Removing a table returns nothing. |
| `Compass.Enabled` | `true` | Draw the compass ribbon. |
| `Compass.ShowBiomeName` | `true` | Name the current biome under the compass. |
| `Compass.Scale` | `2.0` | Overall size of the compass. |
| `Compass.Width` | `460` | Ribbon width, before scale. |
| `Compass.TopOffset` | `16` | Distance from the top, before scale. |
| `Compass.PixelsPerDegree` | `3.2` | How far apart the headings sit. |
| `Compass.Opacity` | `0.85` | Opacity at the centre. |
| `Compass.Color` | `#FFF7E0` | Headings, ticks and biome name. |
| `Compass.MarkerColor` | `#FFF0C7` | The centre marker showing the way you face. |
| `Compass.SettingsKey` | `F7` | Opens the colour panel. `None` disables it. |
| `Server.EnforceOnClients` | `true` | Server side. Send this server's rules to modded clients. |
| `Server.RequireModdedClients` | `false` | Server side. Turn away players without the mod. |
| `Server.ClientGraceSeconds` | `20` | Server side. How long a joiner has to identify itself. |

Everything under `Map`, `Table` and `Server` is a rule and can be set by a server. Everything
under `Compass` is presentation and always stays yours.

## Source

[github.com/IronTreeStudio/NoMapWayfinding](https://github.com/IronTreeStudio/NoMapWayfinding) - MIT.
