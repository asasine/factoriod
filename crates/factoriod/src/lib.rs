use std::path::Path;
pub use server_opts::*;
pub use utils::*;
use factorio_http_api::download;
use tracing_subscriber::{fmt::format::FmtSpan, EnvFilter, FmtSubscriber};

pub mod daemon;
mod server_opts;
mod utils;

/// Set up tracing for the application. This will log all traces to the console. It additionall sets the log level for
/// the factoriod crates to `trace`.
pub fn setup_tracing() {
    let env_filter = EnvFilter::from_default_env()
        .add_directive(
            "factoriod=trace"
                .parse()
                .expect("failed to parse directive."),
        );


    FmtSubscriber::builder()
        .with_span_events(FmtSpan::FULL)
        .with_env_filter(env_filter)
        .with_writer(std::io::stderr)
        .try_init()
        .expect("failed to create subscriber");
}

/// Get the path to the Factorio directory within `root`. If the directory `"factorio"` adjoined to `root` does not
/// exist, this function will download and extract the game to that directory before returning the path.
pub fn get_factorio_directory<P: AsRef<Path>>(
    root: P,
) -> Result<std::path::PathBuf, Box<dyn std::error::Error>> {
    let root = root.as_ref();
    if !root.exists() {
        return Err("root does not exist".into());
    }

    let factorio_dir = root.join("factorio");
    if root.join("factorio").exists() {
        return Ok(root.join("factorio"));
    }

    let latest_headless_version = download::latest_stable_headless_version()?;
    let compressed_archive = download::download_to(
        &latest_headless_version,
        download::Build::Headless,
        download::Distro::Linux64,
        root,
    )?;

    download::extract_to(&compressed_archive, root)?;
    Ok(factorio_dir)
}
