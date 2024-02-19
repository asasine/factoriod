//! The *download* module mirrors the [Factorio Download API](https://wiki.factorio.com/Download_API).
//!
//! The Download API provides a way to fetch the latest versions of Factorio and their download URLs.
//!
//! # Example
//! ```no_run
//! use factorio_api::download;
//! let latest_versions = download::latest_versions();
//! println!("latest versions: {:?}", latest_versions);
//! ```

use reqwest;
use serde::{Deserialize, Serialize};

pub type Version = semver::Version;

/// The type of build for a Factorio version.
#[derive(Serialize, Deserialize, Debug, Clone, PartialEq, Eq)]
pub enum Build {
    /// The alpha build of the version.
    Alpha,

    /// The demo build of the version.
    Demo,

    /// The headless build of the version.
    Headless,
}

/// The type of distribution for a Factorio version.
#[derive(Serialize, Deserialize, Debug, Clone, PartialEq, Eq)]
pub enum Distro {
    /// The Windows 64-bit distribution as an EXE installer.
    Win64,

    /// The Windows 64-bit distribution as a ZIP with an [Inno Setup](https://en.wikipedia.org/wiki/Inno_Setup) installer.
    Win64Manual,

    /// The Windows 32-bit distribution as an EXE installer.
    Win32,

    /// The Windows 32-bit distribution as a ZIP with an [Inno Setup](https://en.wikipedia.org/wiki/Inno_Setup) installer.
    Win32Manual,

    /// The macOS distribution as a DMG.
    Osx,

    /// The Linux 64-bit distribution as a compressed tarball.
    Linux64,

    /// The Linux 32-bit distribution as a compressed tarball.
    Linux32,
}

/// Builds of a particular Factorio version.
/// The fields mirror the values of the [`Build`] enum.
#[derive(Serialize, Deserialize, Debug, Clone, PartialEq, Eq)]
pub struct Builds {
    /// The alpha build of the version.
    pub alpha: Option<Version>,

    /// The demo build of the version.
    pub demo: Option<Version>,

    /// The headless build of the version.
    pub headless: Option<Version>,
}

/// Versions of a particular Factorio release.
#[derive(Serialize, Deserialize, Debug, Clone, PartialEq, Eq)]
pub struct Versions {
    /// The stable builds of the version.
    pub stable: Option<Builds>,

    /// The experimental builds of the version.
    pub experimental: Option<Builds>,
}

/// Fetch the latest versions of Factorio.
///
/// # Example
/// ```no_run
/// use factorio_api::download;
/// let latest_versions = download::latest_versions();
/// println!("latest versions: {:?}", latest_versions);
/// ```
pub fn latest_versions() -> Result<Versions, Box<dyn std::error::Error>> {
    Ok(reqwest::blocking::get("https://factorio.com/api/latest-releases")?.json::<Versions>()?)
}

/// Fetch the latest stable headless version of Factorio.
///
/// # Example
/// ```no_run
/// use factorio_api::download;
/// let latest_stable_headless_version = download::latest_stable_headless_version();
/// println!("latest stable headless version: {:?}", latest_stable_headless_version);
/// ```
pub fn latest_stable_headless_version() -> Result<Version, Box<dyn std::error::Error>> {
    let versions = latest_versions()?;
    Ok(versions
        .stable
        .ok_or("no stable versions")?
        .headless
        .ok_or("no stable headless version")?)
}
