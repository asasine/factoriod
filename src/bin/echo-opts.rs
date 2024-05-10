//! Echoes the options for the systemd service factorio.service, that should be written to the `opts.env` file and
//! included as an `EnvironmentFile=` option in the unit file.

use std::{io::Write, path::PathBuf};

use clap::Parser;

#[derive(Parser)]
struct Args {
    /// The path to the configuration directory containing files like `server-settings.json`.
    /// If not provided, only a bare minimum `opts.env` file will be echoed.
    config_dir: Option<PathBuf>,
}

fn main() {
    factoriod::setup_tracing(Some(tracing::Level::INFO));
    let args = Args::parse();
    let server_opts = factoriod::ServerOpts::new::<PathBuf>(args.config_dir);
    std::io::stdout().write_all(server_opts.to_env().as_encoded_bytes()).expect("failed to write to stdout");
}
