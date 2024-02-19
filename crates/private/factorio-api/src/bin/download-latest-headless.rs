use factorio_api::download;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let latest_stable_headless_version = download::latest_stable_headless_version()?;
    let download_directory = std::env::current_dir()?;
    download::download_to(
        &latest_stable_headless_version,
        factorio_api::download::Build::Headless,
        factorio_api::download::Distro::Linux64,
        download_directory,
    )?;

    Ok(())
}
