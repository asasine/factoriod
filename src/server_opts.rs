//! The server options module contains the `ServerOpts` struct, which is used to generate the `FACTORIO_OPTS`
//! environment variable used by the systemd service factorio.service.

use std::{ffi::OsString, path::{Path, PathBuf}, process::{Command, CommandArgs}};

/// Add a `file` option to the command if the file exists and is a file. The option will be prefixed with `flags`.
fn add_file_opt<P: AsRef<Path>>(command: &mut Command, flags: &[&str], file: P) {
    match file.as_ref().canonicalize() {
        Ok(file) if file.is_file() => {
            command.args(flags);
            command.arg(file);
        },
        Ok(file) => tracing::warn!("{} is not a file!", file.display()),
        Err(e) => tracing::info!("failed to canonicalize {}: {}", file.as_ref().display(), e),
    }
}

/// Transform the command arguments into a vector of `OsString`s.
fn args_to_os_strings(args: CommandArgs) -> Vec<OsString> {
    args.map(|s| s.to_os_string()).collect()
}

/// The options for the server. These can be transformed into the `FACTORIO_OPTS` environment variable used by the
/// systemd service factorio.service. If the configuration directory does not exist, the options will be the bare
/// minimum to start the server, which may not be desirable.
pub struct ServerOpts {
    /// The path to the configuration directory containing files like `server-settings.json`.
    config_dir: Option<PathBuf>,
}

impl ServerOpts {
    /// Create a new instance of the server options.
    pub fn new<P1: AsRef<Path>>(config_dir: Option<P1>) -> ServerOpts {
        ServerOpts {
            config_dir: config_dir.map(|p| p.as_ref().to_owned()),
        }
    }

    /// Transform the options into the `FACTORIO_OPTS` environment variable.
    pub fn to_env(&self) -> OsString {
        let mut env = OsString::from("FACTORIO_OPTS=");
        let opts = self.get_opts();
        env.push("'");

        // push each opt, separated by a space, with the last opt not having a trailing space
        let size = opts.len();
        for (i, opt) in opts.into_iter().enumerate() {
            env.push(opt);
            if i < size - 1 {
                env.push(" ");
            }
        }

        env.push("'");
        env
    }

    /// Get the value of the environment variable.
    fn get_opts(&self) -> Vec<OsString> {
        let mut command = Command::new("factorio");
        let config_dir = match self.config_dir.as_ref() {
            Some(p) if p.is_dir() => p,
            Some(p) if !p.is_dir() => {
                tracing::warn!("{} is not a directory!", p.display());
                command.arg("--start-server-load-latest");
                return args_to_os_strings(command.get_args());
            },
            Some(p) => {
                tracing::info!("{}  not exist", p.display());
                command.arg("--start-server-load-latest");
                return args_to_os_strings(command.get_args());
            },
            None => {
                tracing::info!("no config directory provided, using default options");
                command.arg("--start-server-load-latest");
                return args_to_os_strings(command.get_args());
            }
        };

        add_file_opt(&mut command, &["--server-settings"], config_dir.join("server-settings.json"));
        add_file_opt(&mut command, &["--use-server-whitelist", "--server-whitelist"], config_dir.join("server-whitelist.json"));
        add_file_opt(&mut command, &["--server-banlist"], config_dir.join("server-banlist.json"));
        add_file_opt(&mut command, &["--server-adminlist"], config_dir.join("server-adminlist.json"));
        args_to_os_strings(command.get_args())
    }
}


#[cfg(test)]
mod tests {
    use super::*;

    use tempfile::{self, NamedTempFile};

    #[test]
    fn test_add_file_opt_doesnt_exist() -> Result<(), Box<dyn std::error::Error>> {
        let mut command = Command::new("factorio");
        let file = Path::new("/path/to/file");
        add_file_opt(&mut command, &["--server-settings"], file);
        assert_eq!(command.get_args().into_iter().len(), 0);
        Ok(())
    }

    #[test]
    fn test_add_file_opt() -> Result<(), Box<dyn std::error::Error>> {
        let mut command = Command::new("factorio");
        let file = NamedTempFile::new()?;
        add_file_opt(&mut command, &["--server-settings"], &file);
        assert_eq!(command.get_args().collect::<Vec<_>>(), vec!["--server-settings", file.path().to_str().unwrap()]);
        Ok(())
    }

    #[test]
    fn test_args_to_os_strings() {
        let mut command = Command::new("bin");
        let args = command.arg("a").arg("b").arg("c").get_args();
        let os_strings = args_to_os_strings(args);
        assert_eq!(os_strings, vec!["a", "b", "c"].iter().map(OsString::from).collect::<Vec<_>>());
    }

    #[test]
    fn test_server_opts_to_env_none() {
        let server_opts = ServerOpts::new::<PathBuf>(None);
        assert_eq!(server_opts.to_env(), OsString::from("FACTORIO_OPTS='--start-server-load-latest'"));
    }

    #[test]
    fn test_server_opts_to_env_not_exist() {
        let server_opts = ServerOpts::new(Some("/path/to/config"));
        assert_eq!(server_opts.to_env(), OsString::from("FACTORIO_OPTS='--start-server-load-latest'"));
    }

    #[test]
    fn test_server_opts_to_env_not_dir() {
        let file = NamedTempFile::new().unwrap();
        let server_opts = ServerOpts::new(Some(file.path()));
        assert_eq!(server_opts.to_env(), OsString::from("FACTORIO_OPTS='--start-server-load-latest'"));
    }

    #[test]
    fn test_server_opts_to_env() {
        let temp_dir = tempfile::tempdir().unwrap();
        let create_file = |name| {
            let path = temp_dir.path().join(name);
            std::fs::File::create(&path).unwrap();
            path
        };

        let server_settings = create_file("server-settings.json");
        let server_whitelist = create_file("server-whitelist.json");
        let server_banlist = create_file("server-banlist.json");
        let server_adminlist = create_file("server-adminlist.json");

        let server_opts = ServerOpts::new(Some(temp_dir.path()));
        let actual = server_opts.to_env();
        let actual = actual.to_string_lossy();
        assert!(actual.starts_with("FACTORIO_OPTS='"));
        assert!(actual.ends_with("'"));
        assert!(actual.contains(format!("--server-settings {}", server_settings.display()).as_str()));
        assert!(actual.contains(format!("--use-server-whitelist --server-whitelist {}", server_whitelist.display()).as_str()));
        assert!(actual.contains(format!("--server-banlist {}", server_banlist.display()).as_str()));
        assert!(actual.contains(format!("--server-adminlist {}", server_adminlist.display()).as_str()));
    }
}
