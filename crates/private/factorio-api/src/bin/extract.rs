use factorio_api::download;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let download_directory = std::env::current_dir()?;
    let tar_xz_paths = download_directory.read_dir()?
        .filter_map(|entry| {
            entry.ok().and_then(|entry| {
                if entry.file_type().ok()?.is_file() {
                    Some(entry.path())
                } else {
                    None
                }
            })
        })
        .filter(|path| path.extension().and_then(|ext| ext.to_str()) == Some("xz"))
        .filter(|path| path.with_extension("").extension().and_then(|ext| ext.to_str()) == Some("tar"));

    for archive in tar_xz_paths {
        let destination = archive.parent().ok_or("archive has no parent")?;
        println!("Extracting {} to {}", archive.display(), destination.display());
        download::extract_to(&archive, destination)?;
    }

    Ok(())
}
