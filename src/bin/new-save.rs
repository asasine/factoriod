use factoriod::daemon::FactorioServer;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    factoriod::setup_tracing();
    let factorio_dir = factoriod::get_factorio_directory(std::env::current_dir()?)?;
    let server = FactorioServer::try_new(factorio_dir)?;
    let save_name = "save1";  // TODO: get this from the user
    server.new_save(save_name)?;
    Ok(())
}
