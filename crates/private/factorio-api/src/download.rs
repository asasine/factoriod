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
use strum;
use xz2;
use tar;

pub type Version = semver::Version;

/// The type of build for a Factorio version.
#[derive(Serialize, Deserialize, Debug, Clone, PartialEq, Eq, strum::Display)]
#[strum(serialize_all = "kebab-case")]
pub enum Build {
    /// The alpha build of the version.
    Alpha,

    /// The demo build of the version.
    Demo,

    /// The headless build of the version.
    Headless,
}

/// The type of distribution for a Factorio version.
#[derive(Serialize, Deserialize, Debug, Clone, PartialEq, Eq, strum::Display)]
#[strum(serialize_all = "kebab-case")]
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

/// Get the download URL for a Factorio version.
///
/// # Example
/// ```
/// use factorio_api::download;
/// let version = download::Version::parse("1.1.0").unwrap();
/// let download_url = download::download_url(Version)
pub fn download_url(version: &Version, build: Build, distro: Distro) -> String {
    format!(
        "https://factorio.com/get-download/{}/{}/{}",
        version, build, distro
    )
}

/// Downloads a Factorio version to a directory.
///
/// # Example
/// ```no_run
/// use factorio_api::download;
/// let version = download::Version::new(1, 1, 1);
/// download::download_to(&version, download::Build::Headless, download::Distro::Linux64, "/tmp/factorio")?;
/// ```
pub fn download_to<P: AsRef<std::path::Path>>(
    version: &Version,
    build: Build,
    distro: Distro,
    directory: P,
) -> Result<(), Box<dyn std::error::Error>> {
    let directory = directory.as_ref();
    if directory.is_file() {
        return Err("directory is a file".into());
    }

    std::fs::create_dir_all(directory)?;

    let url = download_url(version, build, distro);
    let mut response = reqwest::blocking::get(&url)?;
    let filename = response
        .url()
        .path_segments()
        .and_then(|segments| segments.last())
        .and_then(|name| if name.is_empty() { None } else { Some(name) })
        .unwrap_or("factorio.tar.xz");

    let destination = directory.join(filename);
    let mut file = std::fs::File::create(destination)?;
    response.copy_to(&mut file)?;
    Ok(())
}

/// Extracts a `.tar.xz` archive to a directory.
///
/// # Example
/// ```no_run
/// use factorio_api::download;
/// download::extract_to("/tmp/factorio.tar.xz", "/tmp/factorio")?;
/// ```
pub fn extract_to<P1: AsRef<std::path::Path>, P2: AsRef<std::path::Path>>(
    archive: P1,
    directory: P2,
) -> Result<(), Box<dyn std::error::Error>> {
    let archive = archive.as_ref();
    let directory = directory.as_ref();
    if !archive.is_file() {
        return Err("archive is not a file".into());
    }

    if archive.extension().and_then(|ext| ext.to_str()) != Some("xz") {
        return Err("archive is not an .xz file".into());
    }

    if archive.with_extension("").extension().and_then(|ext| ext.to_str()) != Some("tar") {
        return Err("archive is not a .tar.xz file".into());
    }

    if directory.is_file() {
        return Err("directory is a file".into());
    }

    std::fs::create_dir_all(directory)?;

    let file = std::fs::File::open(archive)?;
    let reader = std::io::BufReader::new(file);
    let decoder = xz2::read::XzDecoder::new(reader);
    let mut archive = tar::Archive::new(decoder);
    archive.unpack(directory)?;
    Ok(())
}