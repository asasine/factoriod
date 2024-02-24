use std::path::Path;

use factorio_api::download;
use factorio_daemon;
use factorio_daemon::factorio_server;
use tracing::info;
use tracing_subscriber::{fmt::format::FmtSpan, EnvFilter, FmtSubscriber};

/// Set up tracing for the application. This will log all traces to the console. It additionall sets the lgo level for
/// the factoriod crates to `trace`.
fn setup_tracing() {
    let env_filter = EnvFilter::from_default_env()
        .add_directive(
            "factorio_api=trace"
                .parse()
                .expect("failed to parse directive."),
        )
        .add_directive(
            "factorio_config=trace"
                .parse()
                .expect("failed to parse directive."),
        )
        .add_directive(
            "factorio_daemon=trace"
                .parse()
                .expect("failed to parse directive."),
        );

    FmtSubscriber::builder()
        .with_span_events(FmtSpan::FULL)
        .with_env_filter(env_filter)
        .try_init()
        .expect("failed to create subscriber");
}

/// Get the path to the Factorio directory within `root`. If the directory `"factorio"` adjoined to `root` does not
/// exist, this function will download and extract the game to that directory before returning the path.
fn get_factorio_directory<P: AsRef<Path>>(
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

fn run_server<P: AsRef<Path>>(factorio_dir: P) -> factorio_server::Result<()> {
    let factorio_dir = factorio_dir.as_ref();
    let server = factorio_daemon::FactorioServer::try_new(factorio_dir)?;
    server.start()
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    setup_tracing();
    let factorio_dir = get_factorio_directory(std::env::current_dir()?)?;
    info!("factorio directory: {}", factorio_dir.display());
    run_server(factorio_dir)?;
    Ok(())
}
