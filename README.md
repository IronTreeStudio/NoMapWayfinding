# NoMap Wayfinding

A Valheim mod that takes away the minimap and puts the world map back where it belongs: on a
cartography table, showing everyone's exploration.

- **No minimap.** The corner map is never drawn. You navigate by looking at the world.
- **A compass instead.** A sliding heading ribbon sits where the minimap was, with the current
  biome under it.
- **The map lives on the table.** The map key does nothing. Use a cartography table and the
  full-screen map opens; walk away from the table and it closes.
- **One touch shares everything.** Using a table merges in both directions at once - the
  table's exploration is added to yours and yours to the table's - so any player who visits
  leaves with the group's combined map.

## Requirements

- Valheim (Windows)
- [BepInEx for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)

## Install

Copy `NoMapWayfinding.dll` into `<Valheim>\BepInEx\plugins\`.

The mod never changes world state or the network protocol, so it works on vanilla servers and
alongside players who are not running it.

## Servers

The mod also loads on a dedicated server, where it does something quite different: it does not
touch any HUD, and instead answers modded clients that ask what the rules are here. A client
that gets an answer uses the server's `Map` and `Table` settings for as long as it is connected,
then goes back to its own. Set `Server.EnforceOnClients` to `false` to leave clients alone.

Compass settings are never sent. Those are display preferences on someone else's monitor.

### Two ways to deploy this

**With the vanilla NoMap world key (recommended).** Set `NoMap` on the world - through world
modifiers at creation, or `setglobalkey NoMap` - and every client loses the minimap and the map
key through Iron Gate's own code, unmodded ones included. The mod then only has to do the part
vanilla cannot: open the map at a cartography table.

This is real enforcement rather than a rule clients agree to follow, and it inverts the default.
Installing nothing now means having no map, instead of keeping one. It costs nothing elsewhere,
either: every player keeps recording exploration whether or not they can see it, and unmodded
players can still use cartography tables normally. They contribute their exploration to a table
and pull the group's back down, they simply cannot see the result.

The cost: an unmodded player loses the map completely, pins and death markers included, not just
the minimap. And `NoMap` is a property of the world, so it applies in single player on that world
too.

**Without it.** Leave `NoMap` alone and the mod hides the minimap itself, on clients that have
it. Players without the mod keep theirs, so `Server.RequireModdedClients` below is the only thing
standing between them and a minimap.

### Requiring the mod

Rules only bind players who installed the mod, which is nobody who did not want to be bound. So
`Server.RequireModdedClients` turns away players who do not have it. It is off by default - opt in
only if you run a server and want it, and note that hosting a game from your own client makes you
a server, so switching it on there turns away friends who have not installed it.

A player who has not identified themselves within `Server.ClientGraceSeconds` is disconnected and
shown a real error screen rather than being dropped without explanation, whether or not they have
the mod installed.

The NoMap world key above is usually the better tool. It costs unmodded clients the map through
the game's own code without excluding anyone from the server.

**Neither of these is a security boundary.** Anything a mod checks on a player's own machine can
be worked around by someone determined enough, and the minimap is drawn on their machine. Treat
these as keeping honest players honest. Where you need a rule to hold regardless of goodwill, the
NoMap world key is the closest thing available, because the game itself is what enforces it.

## Configuration

Settings are written to `<Valheim>\BepInEx\config\nomapwayfinding.cfg` on first run and
are read live, so a config manager can change them without restarting.

| Setting | Default | Effect |
| --- | --- | --- |
| `General.Enabled` | `true` | Master switch. Off means vanilla behaviour. |
| `Server.EnforceOnClients` | `true` | Server side only. Send this server's rules to modded clients. |
| `Server.RequireModdedClients` | `false` | Opt in. Turn away players who do not have the mod. |
| `Server.ClientGraceSeconds` | `20` | Server side only. How long a joiner has to identify itself. |
| `Map.HideMinimap` | `true` | Never draw the corner minimap. |
| `Map.RequireTableForMap` | `true` | The map key stops working; only a cartography table opens the map. |
| `Table.AutoSyncOnUse` | `true` | Using a table merges exploration both ways in one interaction. |
| `Table.Range` | `8` | Metres you can walk from the table before the map closes. |
| `Table.SettlingSeconds` | `600` | How long a new table must stand before it can be read. `0` disables. |
| `Table.NoRefundOnRemove` | `false` | Removing a cartography table returns nothing. |
| `Compass.Enabled` | `true` | Draw the compass ribbon. |
| `Compass.ShowBiomeName` | `true` | Show the current biome under the compass. |
| `Compass.Color` | `#FFF7E0` | Headings, ticks and biome name. Hex, or a colour name like `cyan`. |
| `Compass.MarkerColor` | `#FFF0C7` | The fixed centre marker showing the way you face. |
| `Compass.ShoutMarkerSeconds` | `120` | Mark where another player shouted from, for this long. `0` disables. |
| `Compass.SettingsKey` | `F7` | Opens an in-game panel for tuning the colours live. `None` disables it. |
| `Compass.Width` | `460` | Ribbon width in pixels. |
| `Compass.TopOffset` | `16` | Distance from the top of the screen. |
| `Compass.PixelsPerDegree` | `3.2` | Horizontal scale of the ribbon. |
| `Compass.Opacity` | `0.85` | Opacity at the centre. |
| `Compass.Scale` | `2.0` | Overall size of the compass. Width and TopOffset are multiplied by it. |

Everything under `Map` and `Table` is a rule and can be overridden by a server. Everything under
`Compass` is presentation and always stays local. The whole of `Settings.cs` exists so that no
code path reads a rule without going through that check.

The feature groups are independent. Turning `HideMinimap` off while leaving `RequireTableForMap`
on keeps the minimap but still gates the full-screen map, and vice versa.

## How it works

**Hiding the minimap.** `Minimap.Update` forces the mode back to `Small` every frame whenever it
is `None`, so the mode cannot simply be switched off. Instead the mode is left alone and
`m_smallRoot` is deactivated. The game goes on believing the minimap is up, which matters more
than it sounds: exploration, pin upkeep and biome discovery all run out of the same update path.

**Gating the world map.** A prefix on `Minimap.SetMapMode` downgrades an ungranted `Large`
request to `Small`. Access is granted only by using a cartography table and is revoked when the
map is closed, when you walk out of range of that table, or when you die.

**The settling delay.** A cartography table is otherwise a portable position finder: carry the
materials, build one wherever you are, read your position off it, dismantle it and walk on.
`Table.SettlingSeconds` makes a new table wait before it can be read, which costs nothing to a
table you built and left standing.

The stamp goes in the table's own ZDO, so it persists and replicates. It is written from a
postfix on `Piece.SetCreator`, which `Player.PlacePiece` calls on the freshly instantiated object
and which guards internally on `GetCreator() == 0` - so it runs exactly once, at placement, and
never on load. Tables with no stamp count as settled, which grandfathers everything built before
the mod was installed rather than blacking them all out on first login. A negative age fails open
for the same reason: locking someone out of their own base table is worse than a missed check.

Both this and `Table.NoRefundOnRemove` are rules, so a server sets them. Under the `NoMap`
deployment that is unusually close to real enforcement, because an unmodded client cannot open
the map at all.

**Sharing.** Vanilla `MapTable.OnWrite` already calls `OnRead` internally, so writing is a
complete two-way merge; only the read switch is one-directional. The mod calls Valheim's own
`OnWrite` after a read, which means the shared-map packet format and the `MapData` RPC stay
completely vanilla.

**Table ownership.** `OnWrite` builds its payload from the locally replicated copy of the table's
ZDO and sends the result to the ZDO owner, who overwrites it wholesale. When you are not the
owner that copy can be stale, so two players writing within a replication round-trip of each
other lose one of the two contributions. A prefix claims ownership first, which makes the read
authoritative and runs `RPC_MapData` locally. Vanilla has the same race, but only on the write
switch; auto-sync writes on every use of a table and would hit it far more often.

**Rule sync.** The client asks and the server answers, rather than the server pushing on
connect. That avoids hooking the peer handshake - the most version-fragile part of Valheim's
netcode - and it degrades correctly: an unmodded server never answers, and the player keeps
their own settings.

Because the minimap panel also contains the biome label, hiding it would quietly remove biome
readouts - the compass redraws that label so no information is lost.

The compass is IMGUI rather than uGUI on purpose. Parenting a canvas into Valheim's HUD means
depending on the shape of that hierarchy, which Iron Gate reworks between updates; an overlay
that only depends on `Screen` and the game font survives patches that would leave a dangling
widget behind.

## Building

```
dotnet build src/NoMapWayfinding/NoMapWayfinding.csproj -c Release
```

Game assemblies must be present in `lib\` - see [lib/README.md](lib/README.md). There is no
NuGet fallback: the public `Valheim.GameLibs` package is pinned at 0.202.14, a Unity 2020 build
of the game, so it would compile against an API years removed from the one that ships.

The project targets `netstandard2.1` because Valheim runs on Unity 6 and its assemblies are
netstandard-based. `net462` compiles until you touch a game type whose members come from
`netstandard`, and it copies roughly a hundred framework facade assemblies into the output that
must never reach `BepInEx\plugins`.

Set `VALHEIM_PLUGINS` to your `BepInEx\plugins` folder and every build deploys there directly.

`Minimap.Update`, `Minimap.CenterMap` and both `MapTable` switch callbacks are private in the
shipping assembly, so they are bound by name rather than `nameof`. `PatchTargets.Verify` checks
all of them at startup and logs exactly what moved if a game update renames something, instead
of failing silently.

## License

MIT - see [LICENSE](LICENSE).

Valheim and its assemblies are Iron Gate's. Nothing in `lib\` is redistributed here; the build
expects you to supply those from your own installation.
