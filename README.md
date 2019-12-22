# R2DS-Essentials
*name not final*

Bunch of essentials modules for any self-respecting Dedicated Server.

Intended to work for vanilla clients, although obviously other mods you have installed may invalidate this.

## Main Features

- Let you Choose to enable and disable what modules you want from the config file located in `BepInEx\config`.
- Interactible Server Console to execute commands directly from the console window ! 
- Remove useless spam (`Filename: C:\buildslave...` and `Fallback...`) for easier monitoring.

## Modules

### RetrieveUsername
Makes the users appear with their actual steam nickname instead of `[unknown]`.

### ExecConfig
Execute the server config file of your choice located in `Risk of Rain 2_Data/Config/` (server.cfg by default).

### MotD
Adds a configurable message of the day that is send to clients when they connect. Supports tokens.

### HideIP
Hides the IP from the console output window, mostly for preventing privacy leaks during debugging.
  
## Planned features

- ?