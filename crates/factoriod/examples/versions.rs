use factoriod::api::download;

fn main() {
    println!("versions: {:?}", download::latest_versions());
    println!(
        "latest stable headless version: {:?}",
        download::latest_stable_headless_version()
    );
}
