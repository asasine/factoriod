//! The *factorio-config* crate helps configure a Factorio server by creating and updating various config JSON files.
//! It provides a Rust interface for various [Factorio Lua Concepts](https://lua-api.factorio.com/latest/concepts.html).

mod map_gen_settings;
mod map_settings;
mod server_settings;

pub use map_gen_settings::*;
pub use map_settings::*;
pub use server_settings::*;
