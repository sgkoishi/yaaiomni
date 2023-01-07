# yaaiomni
[![Total Downloads](https://img.shields.io/github/downloads/sgkoishi/yaaiomni/total?label=Downloads%40Total&style=for-the-badge) ![Latest Downloads](https://img.shields.io/github/downloads-pre/sgkoishi/yaaiomni/latest/total?label=Downloads%40Latest&style=for-the-badge)](https://github.com/sgkoishi/yaaiomni/releases)

Yet another misc plugin for TShock

#### Commands

| Command | Description | Hidden | Permission | Note |
| --- | --- | --- | --- | --- |
| `/whynot` | Show recent permission queries related to your player. | Hidden by default. | `chireiden.omni.whynot` | |
| `/ghost` | Hide yourself from viewing, `/playing`, etc. | | `chireiden.omni.ghost` | |
| `/setlang` | Set language. | | `chireiden.omni.setlang` | For admin. |
| `/_pvp` | Toggle PvP. | Hidden by default. | `chireiden.omni.pvp` <br> `chireiden.omni.admin.setpvp` | |
| `/_team` | Toggle team. | Hidden by default. | `chireiden.omni.team` <br> `chireiden.omni.admin.setteam` | |
| `/_debugstat` | Show debug stats. | Hidden by default. | `chireiden.omni.admin.debugstat` | |
| `/_gc` | Trigger garbage collection. | Hidden by default. | `chireiden.omni.admin.gc`, `tshock.cfg.maintenance` | For admin. |
| `/maxplayers` | Set max players. | | `chireiden.omni.admin.maxplayers` | Might cause unexpected behaviour if lower than current max. |
| `/tileprovider` | Set tile provider. | | `chireiden.omni.admin.tileprovider` | For admin. |

#### Defaults
* The permission of `/ghost`, `/setlang`, `/_debugstat` is granted to the topmost parent of `owner` with kick permission, or `newadmin`'s parent if `owner` is not found.
* The permission of `/_gc`, `/tileprovider` is granted to the topmost parent of `owner` with maintenance permission, or `trustedadmin`'s parent if `owner` is not found.
* The permission of switch loadout, pvp and team is granted to the guest group as TShock's config.
  * Unable to switch without these permissions. (`.Permission.Restrict` in config)
* Vanilla version check is disabled. (`.SyncVersion` in config)
* Errors thrown from TShock's update check will be silently ignored. (`.SuppressUpdate` in config)

#### More options
* `.TrimMemory` in config can reduce memory usage.
  * Depends on the content of the map, may vary from no effect to ~600MB reduced.
  * No side effects.
* `.Mode.Vanilla` in config can switch to vanilla mode.
  * Will allow common actions that are restricted by default.
  * Will create a group `chireiden_vanilla` as the parent of the topmost parent of the registered group.
* `.CommandRenames` in config can rename commands.
  * It's a `Dictionary<sigOfCommandDelegate: string, newalias: List<string>>`.
  * e.g. `{"Chireiden.TShock.Omni.Plugin.Command_PermissionCheck": ["whynot123", "whynot456"]}`
* `.LavaHandler` in config can stop lava spam.
  * It does not prevent lava from spawning, but rather vacuums it after it *might* spawns.
  * If you have a lava pool and spawn lots of lava slimes (or similar) and butcher, the total amount of lava will be reduced instead of unchanged.

#### Don't touch unless you know what you are doing
* `.Soundness` in config enforce some soundness permission checks.
  * Keep it enabled unless you know what you are doing.
* `.Mitigation` in config can fix some issues that exist but not blame to TShock.
  * Keep it enabled unless you know what you are doing.
* `.Socket` in config can switch to a different socket implementation. 
  * `AnotherAsyncSocket` might help with 'memory leak'. 
  * Don't use `Hacky*` unless you know what you are doing.
* `/_gc` triggers garbage collection.
  * Only do this if you know what you are doing.
* `.TileProvider` in config can switch to a different tile provider.
  * `CheckedTypedCollection` and `CheckedGenericCollection` might slightly improve performance but potentially NRE.
  * Keep it `AsIs` unless you know what you are doing.