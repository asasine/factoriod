//! factoriod is a utility for managing Factorio servers.
//!
//! # Features
//! 1. A file containing the factorio executable's command line options, that can either be sorued with sh or bash, or
//!     included with the `EnvironmentFile=` option in the systemd unit file.
//! 2. The latest save file in the state directory is found and used as the server's save file.
//! 3. The game binaries are downloaded and extracted to the cache directory.

use std::path::PathBuf;

use factoriod::ServerOpts;
use factoriod::api::download::{self, Build, Distro};
use systemd_directories::SystemdDirs;

/// Writes the options for the factoriod systemd service to the `factorio.opts.env` file in the cache directory.
fn write_opts_env(systemd_dirs: &SystemdDirs) -> Result<(), Box<dyn std::error::Error>> {
    let opts_env = systemd_dirs.cache_dir()
        .ok_or("cache dir not found")?
        .join("factorio.opts.env");

    let server_opts = ServerOpts::new(systemd_dirs.config_dir(), systemd_dirs.state_dir());
    tracing::info!("Writing server options to {}", opts_env.display());
    std::fs::write(&opts_env, server_opts.to_env().as_encoded_bytes())?;
    Ok(())
}

/// Downloads and extracts the latest headless Factorio server binary to the cache directory.
fn acquire_binaries(systemd_dirs: &SystemdDirs) -> Result<(), Box<dyn std::error::Error>> {
    let download_directory = systemd_dirs.cache_dir().ok_or("cache dir not found")?;
    let scan_tar_xz_paths = || -> Result<Vec<PathBuf>, std::io::Error> {
        Ok(download_directory
            .read_dir()?
            .filter_map(|entry| {
                entry.ok().and_then(|entry| {
                    if entry.file_type().ok()?.is_file() {
                        Some(entry.path())
                    } else {
                        None
                    }
                })
            })
            .filter(|path| path.extension().and_then(|ext| ext.to_str()) == Some("xz"))
            .filter(|path| {
                path.with_extension("")
                    .extension()
                    .and_then(|ext| ext.to_str())
                    == Some("tar")
            })
            .map(|path| {
                tracing::trace!("Found tar.xz archive: {}", path.display());
                path
            })
            .collect())
    };

    let mut tar_xz_paths = scan_tar_xz_paths()?;
    if tar_xz_paths.is_empty() {
        tracing::info!("No compressed binaries found in {}, downloading the latest.", download_directory.display());
        let latest_stable_headless_version = factoriod::api::download::latest_stable_headless_version()?;
        download::download_to(&latest_stable_headless_version, Build::Headless, Distro::Linux64, &download_directory)?;
        tar_xz_paths = scan_tar_xz_paths()?;
    }

    for archive in tar_xz_paths {
        let destination = archive.parent().ok_or("archive has no parent")?;
        tracing::info!("Extracting {} to {}", archive.display(), destination.display());
        download::extract_to(&archive, destination)?;
    }

    Ok(())
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    factoriod::setup_tracing();
    let systemd_dirs = SystemdDirs::new();
    write_opts_env(&systemd_dirs)?;
    acquire_binaries(&systemd_dirs)?;
    Ok(())
}
