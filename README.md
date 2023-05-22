# factoriod
[![.NET](https://github.com/asasine/factoriod/actions/workflows/dotnet.yml/badge.svg)](https://github.com/asasine/factoriod/actions/workflows/dotnet.yml)

A factorio daemon for Ubuntu.

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

## Configuration
The daemon reads and stores configuration in _/etc/factoriod/_ directory by default.
This can be adjusted by modifying the [src/Factoriod.Daemon/appsettings.json](src/Factoriod.Daemon/appsettings.json) file.

### Systemd service
1. [Install the daemon](#building-the-debian-package)
1. Start the service: `sudo systemctl start factoriod`
1. To have the service start on boot: `sudo systemctl enable factoriod`
1. View the logs: `journalctl -u factoriod`

#### Directories in use
- /var/cache/factoriod/: downloaded game binaries
- /var/lib/factoriod/: save games
- /etc/factoriod/: configuration files (map settings, server settings, etc.)
