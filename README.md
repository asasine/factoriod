# factoriod
[![.NET](https://github.com/asasine/factoriod/actions/workflows/dotnet.yml/badge.svg)](https://github.com/asasine/factoriod/actions/workflows/dotnet.yml)

A factorio daemon for Ubuntu

## Getting started
### Prerequisites
1. Linux (tested on Ubuntu)
1. [Install .NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download)
1. Clone the repo
1. Open a terminal to the cloned repo
1. Run: `dotnet tool restore`
1. Run: `dotnet tool run dotnet-deb install`

### Launching the daemon
1. Open a terminal to the cloned repo
1. Run: `dotnet run --project src/Factoriod.Daemon`

### Building the debian package
1. Open a terminal to the cloned repo
1. Run: `dotnet tool run dotnet-deb -c Release src/Factoriod.Daemon/Factoriod.Daemon.csproj`
1. Run: `sudo apt install ./src/Factoriod.Daemon/bin/Release/net6.0/linux-x64/factoriod.*.deb`

## Configuration
The daemon reads and stores configuration in the _~/.config/factoriod_ directory by default.
This can be adjusted by modifying the [src/Factoriod.Daemon/appsettings.json](src/Factoriod.Daemon/appsettings.json) file.

### Systemd service
1. [Install the daemon](#building-the-debian-package)
1. Start the service: `sudo systemctl start factoriod`
1. To have the service start on boot: `sudo systemctl enable factoriod`

#### Directories in use
- /var/cache/factoriod/: downloaded game binaries
- /var/tmp/factoriod/: temporary files such as game updates
- /var/lib/factoriod/: save games
- /etc/factoriod/: configuration files (map settings, server settings, etc.)
