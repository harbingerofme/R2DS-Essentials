# R2DS-Essentials
**Risk of Rain 2 Dedicated Server Essentials** (**R2DSE**) is a mod containing a bunch of modules for any self-respecting dedicated server.

Intended to work for all clients, including vanilla, although obviously other mods you have installed may invalidate this.

Track update progress, get support and suggest new features over at the [R2DSE discord](https://discord.gg/yTfsMWP).

**Maintainers : Harb and iDeath**
**Contributors:**
    
    * Rein: ModSync module 

## Main Features

- Let you choose to enable and disable what modules you want from the config file located in `BepInEx\config`.
- Interactible Server Console to execute commands directly from the console window!
- Remove useless spam (`Filename: C:\buildslave...` and `Fallback...`) for easier monitoring.
- (almost) completely modular!
- Doesn't need a full r2api installed. *(Just it's `MMHOOK` file will do, an update is usually available for this on patch day from the modding discord.)*
- Helper Functions exposed, so other mods can depend on R2DSE.

### Installation

1. Make sure you grab at least the `MMHOOK_Assembly-CSharp.dll` file from the latest R2API release and put it in your `BepInEx/Plugins` folder.
2. Drop `R2DS-Essentials.dll` in your `BepInEx/Plugins` folder.

### Optimal Usage

If you have installed the Dedicated Server Tool through Steam, you'll have to: 
- Launch it through the Risk of Rain 2.exe directly to restore the console colors
- In the file `doorstop_config.ini` change `redirectOutputLog` to `true` to remove duplicate output lines in the server console.

## Modules



### ChatCommands

Intercepts chat messages starting with `/` and executes them as though the client send this as a console command to the server.
This means a client doesn't need a mod to issue its console commands as they can type them in chat instead.

### ExecConfig

After all catalogs have been loaded, execute the server config file of your choice located in `Risk of Rain 2_Data/Config/` (server.cfg by default).
Useful for running commands added by mods automatically.

### FixVanilla

There are *some* bugs in vanilla RoR2 on Dedicated Servers. This module alleviates some of those.


### HideIP

Hides the IP from the console output window, mostly for preventing privacy leaks during debugging.

### ModSync

*REQUIRES R2API* This module aims to prevent desync and cheating by mods. Comes with extensive configuration.

* Prevent clients with blacklisted mods from connecting.
* Prevent clients without whitelisted mods from connecting.

### MotD

Adds a configurable message of the day that is send to clients when they connect, enter a specific stage, pass time, and/or complete a certain number of stages.
Supports tokens and unity rich text.

### RetrieveUsername

Makes the users appear with their actual steam nickname instead of `???`.
  
## Planned features

- ?