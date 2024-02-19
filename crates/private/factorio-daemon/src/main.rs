use factorio_api::download;
use tracing_subscriber::{fmt::format::FmtSpan, EnvFilter, FmtSubscriber};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let env_filter = EnvFilter::from_default_env()
        .add_directive("factorio_api=trace".parse()?)
        .add_directive("factorio_daemon=trace".parse()?);

    FmtSubscriber::builder()
        .with_span_events(FmtSpan::FULL)
        .with_env_filter(env_filter)
        .try_init()
        .expect("failed to create subscriber");

    let latest_headless_version = download::latest_stable_headless_version()?;
    let compressed_archive = download::download_to(
        &latest_headless_version,
        download::Build::Headless,
        download::Distro::Linux64,
        std::env::current_dir()?,
    )?;

    let destination = compressed_archive.parent().ok_or("archive has no parent")?;
    download::extract_to(&compressed_archive, destination)?;

    Ok(())
}
