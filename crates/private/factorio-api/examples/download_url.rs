fn main() -> Result<(), Box<dyn std::error::Error>> {
    let version = factorio_api::download::Version::new(1, 1, 1);
    let download_url = factorio_api::download::download_url(
        &version,
        factorio_api::download::Build::Headless,
        factorio_api::download::Distro::Linux64,
    );

    println!("download url: {:?}", download_url);
    Ok(())
}
