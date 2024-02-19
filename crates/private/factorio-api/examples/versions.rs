fn main() {
    println!("versions: {:?}", factorio_api::download::latest_versions());
    println!(
        "latest stable headless version: {:?}",
        factorio_api::download::latest_stable_headless_version()
    );
}
