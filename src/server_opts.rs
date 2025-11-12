//! The server options module contains the [`ServerOpts`] struct, which is used to generate the `FACTORIO_OPTS`
//! environment variable used by the factoriod systemd service.

use std::ffi::OsString;
use std::path::{Path, PathBuf};
use std::process::{Command, CommandArgs};

use tracing::{debug, info, warn};

use crate::utils;

/// Add a `file` option to the command if the file exists and is a file. The option will be prefixed with `flags`.
fn add_file_opt<P: AsRef<Path>>(command: &mut Command, flags: &[&str], file: P) {
    match file.as_ref().canonicalize() {
        Ok(file) if file.is_file() => {
            command.args(flags);
            command.arg(file);
        },
        Ok(file) => warn!("{} is not a file!", file.display()),
        Err(e) => debug!("failed to canonicalize {}: {}", file.as_ref().display(), e),
    }
}

/// Transform the command arguments into a vector of `OsString`s.
fn args_to_os_strings(args: CommandArgs) -> Vec<OsString> {
    args.map(|s| s.to_os_string()).collect()
}

/// Add the server options to the command. If the configuration directory does not exist, the options will not be added.
fn add_server_options(command: &mut Command, config_dir: &Path) {
    add_file_opt(command, &["--server-settings"], config_dir.join("server-settings.json"));
    add_file_opt(command, &["--use-server-whitelist", "--server-whitelist"], config_dir.join("server-whitelist.json"));
    add_file_opt(command, &["--server-banlist"], config_dir.join("server-banlist.json"));
    add_file_opt(command, &["--server-adminlist"], config_dir.join("server-adminlist.json"));
}

/// Add the save options to the command. If the state directory does not exist, `--start-server-load-latest` will be
/// used instead.
fn add_save_options(command: &mut Command, state_dir: &Path) {
    let save_dir = state_dir.join("saves");
    let args: Vec<OsString> = utils::get_latest_save(save_dir)
        .map(|save| vec![OsString::from("--start-server"), save.into_os_string()])
        .unwrap_or_else(|_| vec![OsString::from("--start-server-load-latest")]);

    command.args(args);
}

/// Invokes the `adder` function with the given `dir` if it is a directory. Trace events will be emitted if `dir` is
/// [`None`], not a directory, or does not exist.
fn add_opts<P: AsRef<Path>>(command: &mut Command, word: &str, dir: &Option<P>, adder: impl Fn(&mut Command, &Path)) {
    let dir = dir.as_ref().map(|p| p.as_ref());
    match dir {
        Some(p) if p.is_dir() => adder(command, p),
        Some(p) if !p.is_dir() => warn!("{} is not a directory!", p.display()),
        Some(p) => info!("{} does not exist", p.display()),
        None => info!("no {} directory provided, no options will be added", word),
    }
}

/// The options for the server. These can be transformed into the `FACTORIO_OPTS` environment variable used by the
/// factorio systemd service.
///
/// If directories containing configuration and state are not provided, the options will be the bare minimum, which may
/// not be desirable.
pub struct ServerOpts {
    /// The path to the configuration directory containing files like `server-settings.json`.
    config_dir: Option<PathBuf>,

    /// The path to the state directory containing directories like `saves`.
    state_dir: Option<PathBuf>,
}

impl ServerOpts {
    /// Create a new instance of the server options.
    pub fn new<P1: AsRef<Path>, P2: AsRef<Path>>(config_dir: Option<P1>, state_dir: Option<P2>) -> ServerOpts {
        ServerOpts {
            config_dir: config_dir.map(|p| p.as_ref().to_owned()),
            state_dir: state_dir.map(|p| p.as_ref().to_owned()),
        }
    }

    /// Transform the options into the `FACTORIO_OPTS` environment variable.
    pub fn to_env(&self) -> OsString {
        let mut env = OsString::from("FACTORIO_OPTS=");
        let opts = self.get_opts();
        env.push("'");
        env.push(opts.join(OsString::from(" ").as_os_str()));
        env.push("'");
        env
    }

    /// Get the value of the environment variable.
    fn get_opts(&self) -> Vec<OsString> {
        let mut command = Command::new("factorio");
        add_opts(&mut command, "config", &self.config_dir, add_server_options);
        add_opts(&mut command, "state", &self.state_dir, add_save_options);
        args_to_os_strings(command.get_args())
    }
}


#[cfg(test)]
mod tests {
    use super::*;

    use tempfile::{self, NamedTempFile, TempDir};

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

    struct TempServerOptionsDir {
        temp_dir: TempDir,
        server_settings: PathBuf,
        server_whitelist: PathBuf,
        server_banlist: PathBuf,
        server_adminlist: PathBuf,
    }

    fn create_temp_server_options_dir() -> TempServerOptionsDir {
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
        TempServerOptionsDir {
            temp_dir,
            server_settings,
            server_whitelist,
            server_banlist,
            server_adminlist,
        }
    }

    fn assert_server_options(actual: &str, temp_server_options_dir: &TempServerOptionsDir) {
        assert!(actual.contains(format!("--server-settings {}", temp_server_options_dir.server_settings.display()).as_str()));
        assert!(actual.contains(format!("--use-server-whitelist --server-whitelist {}", temp_server_options_dir.server_whitelist.display()).as_str()));
        assert!(actual.contains(format!("--server-banlist {}", temp_server_options_dir.server_banlist.display()).as_str()));
        assert!(actual.contains(format!("--server-adminlist {}", temp_server_options_dir.server_adminlist.display()).as_str()));
    }

    #[test]
    fn test_add_server_options() {
        let mut command = Command::new("factorio");
        let temp_server_options_dir = create_temp_server_options_dir();
        add_server_options(&mut command, temp_server_options_dir.temp_dir.path());
        let actual = args_to_os_strings(command.get_args()).join(OsString::from(" ").as_os_str());
        let actual = actual.to_string_lossy();
        assert_server_options(&actual, &temp_server_options_dir);
    }

    struct TempSaveOptionsDir {
        temp_dir: TempDir,
        latest_save: PathBuf,
    }

    fn create_temp_save_options_dir() -> TempSaveOptionsDir {
        let temp_dir = tempfile::tempdir().unwrap();
        let save_dir = temp_dir.path().join("saves");
        std::fs::create_dir(&save_dir).unwrap();
        let latest_save = save_dir.join("latest_save.zip");
        std::fs::File::create(&latest_save).unwrap();
        TempSaveOptionsDir {
            temp_dir,
            latest_save,
        }
    }

    fn assert_save_options(actual: &str, temp_save_options_dir: &TempSaveOptionsDir) {
        assert!(actual.contains(format!("--start-server {}", temp_save_options_dir.latest_save.display()).as_str()));
    }

    #[test]
    fn test_add_save_options() {
        let mut command = Command::new("factorio");
        let temp_dir = create_temp_save_options_dir();
        add_save_options(&mut command, temp_dir.temp_dir.path());
        let actual = args_to_os_strings(command.get_args()).join(OsString::from(" ").as_os_str());
        let actual = actual.to_string_lossy();
        assert_save_options(&actual, &temp_dir)
    }

    #[test]
    fn test_add_save_options_no_save() {
        let mut command = Command::new("factorio");
        let temp_dir = tempfile::tempdir().unwrap();
        let save_dir = temp_dir.path().join("saves");
        std::fs::create_dir(&save_dir).unwrap();

        add_save_options(&mut command, &temp_dir.path());
        let actual = args_to_os_strings(command.get_args()).join(OsString::from(" ").as_os_str());
        assert_eq!(actual, "--start-server-load-latest");
    }

    #[test]
    fn test_server_opts_to_env_none() {
        let server_opts = ServerOpts::new::<PathBuf, PathBuf>(None, None);
        assert_eq!(server_opts.to_env(), OsString::from("FACTORIO_OPTS=''"));
    }

    #[test]
    fn test_server_opts_to_env_not_exist() {
        let server_opts = ServerOpts::new(Some("/path/to/config"), Some("/path/to/state"));
        assert_eq!(server_opts.to_env(), OsString::from("FACTORIO_OPTS=''"));
    }

    #[test]
    fn test_server_opts_to_env_not_dir() {
        let file = NamedTempFile::new().unwrap();
        let server_opts = ServerOpts::new(Some(file.path()), Some(file.path()));
        assert_eq!(server_opts.to_env(), OsString::from("FACTORIO_OPTS=''"));
    }

    #[test]
    fn test_server_opts_to_env() {
        let temp_server_options_dir = create_temp_server_options_dir();
        let temp_save_options_dir = create_temp_save_options_dir();
        let server_opts = ServerOpts::new(Some(temp_server_options_dir.temp_dir.path()), Some(temp_save_options_dir.temp_dir.path()));
        let actual = server_opts.to_env();
        let actual = actual.to_string_lossy();
        assert!(actual.starts_with("FACTORIO_OPTS='"));
        assert!(actual.ends_with("'"));
        assert_server_options(&actual, &temp_server_options_dir);
        assert_save_options(&actual, &temp_save_options_dir);
    }
}
