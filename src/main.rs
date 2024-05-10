use std::path::Path;

use factoriod::daemon::{factorio_server, FactorioServer};
use tracing::info;

fn run_server<P: AsRef<Path>>(factorio_dir: P) -> factorio_server::Result<()> {
    let factorio_dir = factorio_dir.as_ref();
    let server = FactorioServer::try_new(factorio_dir)?;
    server.start()
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    factoriod::setup_tracing(None);
    let factorio_dir = factoriod::get_factorio_directory(std::env::current_dir()?)?;
    info!("factorio directory: {}", factorio_dir.display());
    run_server(factorio_dir)?;
    Ok(())
}
