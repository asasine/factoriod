use clap::Parser;
use factoriod::daemon::FactorioServer;

#[derive(Parser)]
struct Args {
    /// The name of the save to create.
    name: String,
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    factoriod::setup_tracing();
    let args = Args::parse();
    let factorio_dir = factoriod::get_factorio_directory(std::env::current_dir()?)?;
    let server = FactorioServer::try_new(factorio_dir)?;
    server.new_save(&args.name)?;
    Ok(())
}
