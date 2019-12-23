# R2DS-Essentials

**Risk of Rain 2 Dedicated Server Essentials** (**R2DSE**) is a mod containing a bunch of modules for any self-respecting dedicated server.

Intended to work for all clients, including vanilla, although obviously other mods you have installed may invalidate this.

Track update progress, get support and suggest new features over at the [R2DSE discord](https://discord.gg/yTfsMWP).

## Main Features

- Let you choose to enable and disable what modules you want from the config file located in `BepInEx\config`.
- Interactible Server Console to execute commands directly from the console window!
- (almost) completely modular!
- Doesn't need a full r2api installed. *(Just it's `MMHOOK` file will do, an update is usually available for this on patch day.)*
- Helper Functions exposed, so other mods can depend on R2DSE.

### Installation:

1. Make sure you grab at least the `MMHOOK_Assembly-CSharp.dll` file from the latest R2API release and put it in your `BepInEx/Plugins` folder.
2. Drop `R2DS-Essentials.dll` in your `BepInEx/Plugins` folder.

## Modules

### RetrieveUsername

Makes the users appear with their actual steam nickname instead of `???`.


### ChatCommands

Intercepts chat messages starting with `/` and executes them as though the client send this as a console command to the server. This means a client doesn't need a mod to issue its console commands as they can type them in chat instead.

### ExecConfig

After all catalogs have been loaded, execute the server config file of your choice located in `Risk of Rain 2_Data/Config/` (server.cfg by default). Useful for running commands added by mods automatically.

### MotD

Adds a configurable message of the day that is send to clients when they connect. Supports tokens and unity rich text.

### HideIP

Hides the IP from the console output window, mostly for preventing privacy leaks during debugging.
  
## Planned features

- Stop that line spam about fallback handlers and the unity buildslave.