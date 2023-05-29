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
The daemon reads and stores configuration in _/etc/factoriod/_ directory by default. Some notable files:
- _appsettings.json_: configuration for the daemon
- Server settings
    - _server-settings.json_: configuration for the server
    - _server-whitelist.json_: a list of allowed players for the server
    - _server-banlist.json_: a list of banned players for the server
    - _server-adminlist.json_: a list of admins for the server

If a configuration file is not found, the daemon will use the default configuration. Custom configurations can be created by copying an example configuration file to _/etc/factoriod/_, modifying it, and restarting the daemon.

```bash
# copy the example to the configuration directory
sudo -u factorio cp /var/cache/factoriod/factorio/data/server-settings.example.json /etc/factoriod/server-settings.json

# edit it
vim /etc/factoriod/server-settings.json

# then restart the daemon
sudo systemctl restart factoriod
```
