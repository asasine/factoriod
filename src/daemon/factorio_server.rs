//! The [`FactorioServer`] manages a headless Factorio server.

use std::error::Error;
use std::fmt;
use std::path::{Path, PathBuf};
use std::process::{Child, Command};

pub type Result<T> = std::result::Result<T, FactorioServerStartError>;

#[derive(Debug)]
pub struct FactorioServer {
    /// The path to the Factorio directory. This is the directory that contains the Factorio binary and data.
    factorio_dir: PathBuf,
}

#[derive(Debug)]
pub enum FactorioServerStartError {
    PathNotFound(PathBuf),
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
            }
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
            FactorioServerStartError::PathNotFound { .. } => None,
            FactorioServerStartError::StartFailed { source, .. } => Some(source),
        }
    }
}

impl FactorioServer {
    /// Tries to create a new Factorio server instance.
    ///
    /// # Errors
    /// If the `factorio_dir` does not exist, this function will return an error.
    pub fn try_new<P: AsRef<Path>>(factorio_dir: P) -> Result<Self> {
        Ok(Self {
            factorio_dir: factorio_dir
                .as_ref()
                .to_path_buf()
                .canonicalize()
                .map_err(|_| FactorioServerStartError::PathNotFound(factorio_dir.as_ref().to_path_buf()))?,
        })
    }

    /// Start the Factorio server.
    #[tracing::instrument(level = "trace")]
    pub fn start(&self) -> Result<()> {
        if !self.factorio_dir.exists() {
            return Err(FactorioServerStartError::PathNotFound(
                self.factorio_dir.clone(),
            ));
        }

        let binary = self
            .factorio_dir
            .join("bin/x64/factorio")
            .canonicalize()
            .map_err(|_| FactorioServerStartError::PathNotFound(self.factorio_dir.clone()))?;

        let mut child: Child = Command::new(&binary)
            .arg("--start-server")
            .spawn()
            .map_err(|source| FactorioServerStartError::StartFailed {
                path: binary.clone(),
                source,
            })?;

        // if waiting fails, we don't care because the process was never running
        child.wait().ok();
        Ok(())
    }
}
