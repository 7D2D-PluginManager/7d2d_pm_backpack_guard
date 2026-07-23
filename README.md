# BackpackGuard

- Protects a player's dropped backpack from being looted by other players for a configurable time after it drops.
- The owner can always loot their own backpack; the owner's friends can optionally be exempted from the protection.
- Protection expires automatically once the configured time has passed, and never applies to backpacks the game itself no longer tracks (only the 3 most recent drops per player are tracked).

Reads backpack ownership/drop time, player friendship and world time through the `IContainerUtil`, `IPlayerUtil`
and `IGameUtil` providers; the plugin never touches the game directly. Loot attempts are intercepted via the
`TileEntityAccessAttemptEvent` published by the PluginManager core.

## Config (`config.json`)

| Key | Description |
| --- | --- |
| `ProtectionMinutes` | Real-world minutes after the backpack drops during which only the owner (and friends, if exempted) can loot it. |
| `ExemptFriends` | `true` to let the owner's friends loot the backpack even while it is protected. |

Localization: `static/lang/en.json`, `static/lang/ru.json`.
