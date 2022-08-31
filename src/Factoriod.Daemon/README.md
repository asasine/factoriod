# Daemon
The daemon consists of two parts:
- A factorio headless game server
- An API server

Both parts are started automatically when the daemon starts. The APIs control the game server through various commands.

When the game server starts, it selects the most recent save from the saves directory. If no saves are found, it waits for a command to create a new save.

## Game server
Access to the factorio game server is controlled through the `FactorioProcess` class.
This class ensures only a single instance of the game server is active at a time.
It contains many public methods for use by APIs such as launching with a provided save, creating new saves, updating to newer versions of the game, and more.

## API server
The API server enables external control of the game server. It features several APIs for managing saves, viewing and controlling the state of the game server, managing mods, and more.