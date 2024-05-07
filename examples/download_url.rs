use factoriod::factorio_api::download;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let version = download::Version::new(1, 1, 1);
    let download_url = download::download_url(
        &version,
        download::Build::Headless,
        download::Distro::Linux64,
    );

    println!("download url: {:?}", download_url);
    Ok(())
}
