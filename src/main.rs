//! factoriod is a utility for managing Factorio servers.
//!
//! # Features
//! 1. A file compatible with the factorio.service's `EnvironmentFile=` option is written from files in this services's
//!     configuration directory.
//! 2. The latest save file in the state directory is found and used as the server's save file.
//! 3. The game binaries are downloaded and extracted to the cache directory.

use factoriod::ServerOpts;
use systemd_directories::SystemdDirs;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    factoriod::setup_tracing(None);
    let systemd_dirs = SystemdDirs::new();
    let opts_env = systemd_dirs.cache_dir()
        .ok_or("cache dir not found")?
        .join("factorio.opts.env");

    let server_opts = ServerOpts::new(systemd_dirs.config_dir(), systemd_dirs.state_dir());
    std::fs::write(&opts_env, server_opts.to_env().as_encoded_bytes())?;

    Ok(())
}
