//! The [`FactorioServer`] manages a headless Factorio server.

use std::error::Error;
use std::fmt;
use std::path::{Path, PathBuf};
use std::process::Command;

use systemd_directories::SystemdDirs;
use tracing::{debug, trace, warn};

pub type Result<T> = std::result::Result<T, FactorioServerStartError>;

/// The directories used by the Factorio server.
#[derive(Debug)]
struct FactorioServerDirs {
    /// The path to the Factorio directory. This is the directory that contains the Factorio binary and data.
    factorio_dir: PathBuf,

    /// The path to the server's state directory. This directory may contain save files and other generated content.
    state_dir: PathBuf,

    /// The path to the server's configuration directory. This directory may contain JSON files that configure the
    /// server, map generation, and other settings. See [`crate::config`] for more information.
    config_dir: PathBuf,
}

/// A Factorio server instance. This struct manages the server's state and configuration directories, and provides
/// methods to start and interact with the running server.
#[derive(Debug)]
pub struct FactorioServer {
    /// The directories used by the Factorio server.
    dirs: FactorioServerDirs,
}


#[derive(Debug)]
pub enum FactorioServerStartError {
    PathNotFound(PathBuf),

    /// No save was found in the saves directory.
    NoSaveFound(PathBuf),
    StartFailed {
        path: PathBuf,
        source: std::io::Error,
    },
}

impl fmt::Display for FactorioServerStartError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            FactorioServerStartError::PathNotFound(path) => {
                write!(f, "Path not found: {}", path.display())
            },
            FactorioServerStartError::NoSaveFound(path) => {
                write!(f, "No save found in the saves directory at {}.", path.display())
            },
            FactorioServerStartError::StartFailed { path, source } => write!(
                f,
                "Failed to start Factorio server from binary {}: {}",
                path.display(),
                source
            ),
        }
    }
}

impl Error for FactorioServerStartError {
    fn source(&self) -> Option<&(dyn Error + 'static)> {
        match self {
            FactorioServerStartError::StartFailed { source, .. } => Some(source),
            _ => None,
        }
    }
}

impl FactorioServer {
    /// Tries to create a new Factorio server instance.
    ///
    /// # Errors
    /// If the `factorio_dir` does not exist, this function will return an error.
    pub fn try_new<P: AsRef<Path>>(factorio_dir: P) -> Result<Self> {
        let dirs = SystemdDirs::new();
        Ok(Self {
            dirs: FactorioServerDirs {
                factorio_dir: factorio_dir
                    .as_ref()
                    .to_path_buf()
                    .canonicalize()
                    .map_err(|_| FactorioServerStartError::PathNotFound(factorio_dir.as_ref().to_path_buf()))?,
                state_dir: dirs.state_dir().map(|p| p.to_path_buf()).unwrap_or(PathBuf::from("var/lib/factoriod")),
                config_dir: dirs.config_dir().map(|p| p.to_path_buf()).unwrap_or(PathBuf::from("etc/factoriod")),
            },
        })
    }

    /// Start the Factorio server.
    #[tracing::instrument(level = "trace")]
    pub fn start(&self) -> Result<()> {
        if !self.dirs.factorio_dir.exists() {
            return Err(FactorioServerStartError::PathNotFound(
                self.dirs.factorio_dir.clone(),
            ));
        }

        let binary = self
            .dirs
            .factorio_dir
            .join("bin/x64/factorio")
            .canonicalize()
            .map_err(|_| FactorioServerStartError::PathNotFound(self.dirs.factorio_dir.clone()))?;

        let mut command = Command::new(&binary);
        self.add_server_options(&mut command);
        self.add_save(&mut command)?;

        let mut child = command.spawn()
            .map_err(|source| FactorioServerStartError::StartFailed {
                path: binary.clone(),
                source,
            })?;

        // if waiting fails, we don't care because the process was never running
        child.wait().ok();
        Ok(())
    }

    #[tracing::instrument(level = "trace")]
    pub fn new_save(&self, name: &str) -> Result<()> {
        let binary = self
            .dirs
            .factorio_dir
            .join("bin/x64/factorio")
            .canonicalize()
            .map_err(|_| FactorioServerStartError::PathNotFound(self.dirs.factorio_dir.clone()))?;

        let mut command = Command::new(&binary);
        let saves_dir = self.dirs.state_dir.join("saves");
        std::fs::create_dir_all(&saves_dir).map_err(|source| FactorioServerStartError::StartFailed {
            path: saves_dir.clone(),
            source,
        })?;

        let save_file = self.dirs.state_dir.join("saves").join(if name.ends_with(".zip") {
            name.into()
        } else {
            format!("{}.zip", name)
        });

        command.arg("--create").arg(save_file);

        if self.dirs.config_dir.is_dir() {
            if let Ok(map_gen_settings) = self.dirs.config_dir.join("map-gen-settings.json").canonicalize() {
                if map_gen_settings.is_file() {
                    command.arg("--map-gen-settings").arg(map_gen_settings);
                } else {
                    warn!("{} is not a file!", map_gen_settings.display());
                }
            } else {
                trace!("map-gen-settings.json not found in {}", self.dirs.config_dir.display());
            }

            if let Ok(map_settings) = self.dirs.config_dir.join("map-settings.json").canonicalize() {
                if map_settings.is_file() {
                    command.arg("--map-settings").arg(map_settings);
                } else {
                    warn!("{} is not a file!", map_settings.display());
                }
            } else {
                trace!("map-settings.json not found in {}", self.dirs.config_dir.display());
            }
        }

        let mut child = command.spawn()
            .map_err(|source| FactorioServerStartError::StartFailed {
                path: binary.clone(),
                source,
            })?;

        // if waiting fails, we don't care because the process was never running
        child.wait().ok();
        Ok(())
    }

    /// Add the server's configuration to the given command, if any.
    ///
    /// The supported configuration files are:
    /// - `server-settings.json`
    /// - `server-whitelist.json`
    /// - `server-banlist.json`
    /// - `server-adminlist.json`
    fn add_server_options(&self, command: &mut Command) {
        if !self.dirs.config_dir.is_dir() {
            trace!("config directory does not exist: {}", self.dirs.config_dir.display());
            return;
        }

        if let Ok(server_settings) = self.dirs.config_dir.join("server-settings.json").canonicalize() {
            if server_settings.is_file() {
                command.arg("--server-settings").arg(server_settings);
            } else {
                warn!("{} is not a file!", server_settings.display());
            }
        } else {
            trace!("server-settings.json not found in {}", self.dirs.config_dir.display());
        }

        if let Ok(server_whitelist) = self.dirs.config_dir.join("server-whitelist.json").canonicalize() {
            if server_whitelist.is_file() {
                command.arg("--use-server-whitelist").arg("--server-whitelist").arg(server_whitelist);
            } else {
                warn!("{} is not a file!", server_whitelist.display());
            }
        } else {
            trace!("server-whitelist.json not found in {}", self.dirs.config_dir.display());
        }

        if let Ok(server_banlist) = self.dirs.config_dir.join("server-banlist.json").canonicalize() {
            if server_banlist.exists() && server_banlist.is_file() {
                command.arg("--server-banlist").arg(server_banlist);
            } else {
                warn!("{} is not a file!", server_banlist.display());
            }
        } else {
            trace!("server-banlist.json not found in {}", self.dirs.config_dir.display());
        }

        if let Ok(server_adminlist) = self.dirs.config_dir.join("server-adminlist.json").canonicalize() {
            if server_adminlist.exists() && server_adminlist.is_file() {
                command.arg("--server-adminlist").arg(server_adminlist);
            } else {
                warn!("{} is not a file!", server_adminlist.display());
            }
        } else {
            trace!("server-adminlist.json not found in {}", self.dirs.config_dir.display());
        }
    }

    /// Add the save to the given command, if any.
    fn add_save(&self, command: &mut Command) -> Result<&Self> {
        let save_dir = self.dirs.state_dir.join("saves");
        match self.dirs.state_dir.join("saves").canonicalize() {
            Ok(save_dir) if save_dir.is_dir() => {
                let latest_save = crate::get_latest_save(&save_dir)?;
                debug!("latest save: {}", latest_save.display());
                command.arg("--start-server").arg(latest_save);
                Ok(self)
            },
            Ok(save_dir) => {
                warn!("{} is not a directory", save_dir.display());
                return Err(FactorioServerStartError::PathNotFound(save_dir.clone()));
            }
            _ => {
                warn!("saves directory does not exist at {}. A save must be created first before continuing.", save_dir.display());
                return Err(FactorioServerStartError::PathNotFound(save_dir.clone()));
            }
        }
    }
}
