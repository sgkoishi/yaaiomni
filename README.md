# yaaiomni
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
| `/_gc` | Trigger garbage collection. | Hidden by default. | `chireiden.omni.admin.gc`,  | For admin. |
| `/maxplayers` | Set max players. | | `chireiden.omni.admin.maxplayers` | Might cause unexpected behaviour if lower than current max. |

#### Defaults
* The permission of `/ghost`, `/setlang`, `/_debugstat` is granted to the topmost parent of `owner` with kick permission, or `newadmin`'s parent if `owner` is not found.
* The permission of toggle pvp and team is granted to the guest group as TShock's config.
  * Unable to toggle pvp and team without these permissions by default. (`.Permission.Restrict` in config)
* Version check is disabled. (`.SyncVersion` in config)
* Errors thrown from TShock's update check will be silently ignored. (`.SuppressUpdate` in config)

#### More options
* `.TrimMemory` in config can reduce memory usage.
  * Depends on the content of the map, may vary from no effect to ~600MB reduced.
  * No side effects.
* `.Mode.Vanilla` in config can switch to vanilla mode.
  * Will allow common actions that are restricted by default.
  * Will create a group `chireiden_vanilla` as the parent of the topmost parent of the registered group.

#### Only do this if you know what you are doing
* `.Soundness` in config enforce some soundness permission checks.
  * Keep it enabled unless you know what you are doing.
* `.Mitigation` in config can fix some issues that exist but not blame to TShock.
  * Keep it enabled unless you know what you are doing.
* `.Socket` in config can switch to a different socket implementation. 
  * `AnotherAsyncSocket` might help with 'memory leak'. 
  * Don't use `Hacky*` unless if you know what you are doing.
* `/_gc` triggers garbage collection.
  * Only do this if you know what you are doing.