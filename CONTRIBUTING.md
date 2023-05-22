# Contributing
## Development
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
The debian binary package is created using [quamotion/dotnet-packaging](https://github.com/quamotion/dotnet-packaging). It can be installed as a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools).
1. Open a terminal to the cloned repo
1. Run: `dotnet tool run dotnet-deb -c Release src/Factoriod.Daemon/Factoriod.Daemon.csproj`
1. Run: `sudo apt install ./src/Factoriod.Daemon/bin/Release/net6.0/linux-x64/factoriod.*.deb`

### Releasing a new version
A new version can be released using the [.github/workflows/release.yml](.github/workflows/release.yml) workflow. This workflow will:
1. Update the project version.
1. Create a debian binary package.
1. Update the APT repository hosted on GitHub Pages. Details on this process can be found [here](https://assafmo.github.io/2019/05/02/ppa-repo-hosted-on-github.html).
1. Push a new commit to the main branch with the updated version.
1. Create a new draft release with auto-generated release notes and the debian binary package attached. Head to [Releases](https://github.com/asasine/factoriod/releases) to publish the release.

To trigger the workflow, go to [Actions > Release](https://github.com/asasine/factoriod/actions/workflows/release.yml) and click the "Run workflow" button. Enter a new version number when prompted, following [semantic versioning](https://semver.org/):
- Major version: incompatible API changes.
- Minor version: added functionality in a backwards-compatible manner, including deprecations.
- Patch version: backwards-compatible bug fixes.

At this time, the this project is still in initial development, so the version is 0.y.z. This means that the public API should not be considered stable, and may change in backwards-incompatible ways between minor or patch releases.
