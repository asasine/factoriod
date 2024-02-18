use systemd_directories;

fn main() {
    println!("config dirs: {:?}", systemd_directories::config_dirs());
}
