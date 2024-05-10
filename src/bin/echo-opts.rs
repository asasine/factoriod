//! Echoes the options for the systemd service factorio.service, that should be written to the `opts.env` file and
//! included as an `EnvironmentFile=` option in the unit file.

use std::{io::Write, path::PathBuf};

use clap::Parser;

#[derive(Parser)]
struct Args {
    /// The path to the configuration directory containing files like `server-settings.json`.
    /// If not provided, only a bare minimum `opts.env` file will be echoed.
    config_dir: Option<PathBuf>,

    /// The path to the state directory containing directories like `saves`.
    /// If not provided, the options will instruct the server to load the latest save, which may fail if no save has
    /// been loaded before.
    state_dir: Option<PathBuf>,
}

fn main() {
    factoriod::setup_tracing();
    let args = Args::parse();
    let server_opts = factoriod::ServerOpts::new(args.config_dir, args.state_dir);
    std::io::stdout().write_all(server_opts.to_env().as_encoded_bytes()).expect("failed to write to stdout");
}
