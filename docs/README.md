# factoriod

A factorio daemon for Ubuntu

## Installation
1. Add the APT repository:
    ```bash
    sudo curl -s --compressed -o /etc/apt/trusted.gpg.d/asasine_factoriod.asc 'https://asasine.github.io/factoriod/KEY.asc'
    sudo curl -s --compressed -o /etc/apt/sources.list.d/asasine_factoriod.list https://asasine.github.io/factoriod/sources.list
    ```

1. Update local package index:
    ```bash
    sudo apt update
    ```

1. Install the daemon:
    ```bash
    sudo apt install factoriod
    ```

1. View the status of the daemon:
    ```bash
    sudo systemctl status factoriod
    ```

1. View logs:
    ```bash
    journalctl -u factoriod
    ```

## Configuration
The daemon reads static configuration from the _/etc/factoriod/_ directory by default. Modifying _/etc/factoriod/appsettings.json_ will adjust the daemon's behavior at runtime.

Dynamic configuration, accessible through the REST API, is stored in _/var/lib/factoriod/config/_. Some notable files:
- Server settings, in _./factorio_:
    - _server-settings.json_: configuration for the factorio server
    - _server-whitelist.json_: a list of allowed players for the server
    - _server-banlist.json_: a list of banned players for the server
    - _server-adminlist.json_: a list of admins for the server

If a configuration file is not found, the daemon will use the default configuration. Configuration can be customized through the REST API.

## Saves
Saves are stored at _/var/lib/factoriod/saves/_ as _*.zip_ files. The daemon will automatically load the most recent save on startup.
To load a different save, use the `PUT api/save/{name}` endpoint.
To create a new save, use the `PUT api/save/create/{name}` endpoint.
