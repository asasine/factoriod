//! Settings for the map generator.

mod autoplace_control;
mod map_gen_size;

use std::collections::HashMap;

use serde::{Deserialize, Serialize};

pub use autoplace_control::*;
pub use map_gen_size::*;

/// Settings for the map generator. These settings are used to create a new map and are saved in the
/// `map-gen-settings.json` file. This struct models the [`MapGenSettings`](https://lua-api.factorio.com/latest/concepts.html#MapGenSettings)
///  type in the Factorio Lua API.
#[derive(Serialize, Deserialize, Debug)]
pub struct MapGenSettings {
    /// The inverse of "water scale" in the map generator GUI.
    pub terrain_segmentation: f32,

    /// The equivalent to "water coverage" in the map generator GUI. Higher coverage means more water in larger oceans.
    /// Water level = `10 * log2(this value)`
    pub water: f32,
    pub autoplace_controls: HashMap<String, AutoplaceControl>,
    pub autoplace_settings: HashMap<String, AutoplaceSettings>,
    pub cliff_settings: CliffPlacementSettings,

    /// Use [`None`] for a random seed, number for a specific seed.
    pub seed: Option<u32>,

    /// Width of the map, in tiles. 0 means infinite.
    pub width: u32,

    /// Height of the map, in tiles. 0 means infinite.
    pub height: u32,

    /// Multiplier for "bite free zone radius".
    pub starting_area: f32,
    pub starting_points: Vec<MapPosition>,
    pub peaceful_mode: bool,

    /// Overrides for property value generators (map type).
    ///
    /// Leave "elevation" blank to get "normal" terrain.
    /// Use "elevation": "0_16-elevation" to reproduce terrain from 0.16.
    /// Use "elevation": "0_17-island" to get an island.
    ///
    /// Moisture and terrain type are also controlled via this.
    /// `"control-setting:moisture:frequency:multiplier"` is the inverse of the "moisture scale" in the map generator GUI.
    /// `"control-setting:moisture:bias"` is the "moisture bias" in the map generator GUI.
    /// `"control-setting:aux:frequency:multiplier"` is the inverse of the "terrain type scale" in the map generator GUI.
    /// `"control-setting:aux:bias"` is the "terrain type bias" in the map generator GUI
    pub property_expression_names: HashMap<String, String>,
}

impl Default for MapGenSettings {
    fn default() -> Self {
        Self {
            terrain_segmentation: 1.0,
            water: 1.0,
            autoplace_controls: HashMap::from(
                [
                    ("coal", AutoplaceControl::default()),
                    ("copper-ore", AutoplaceControl::default()),
                    ("crude-oil", AutoplaceControl::default()),
                    ("enemy-base", AutoplaceControl::default()),
                    ("iron-ore", AutoplaceControl::default()),
                    ("stone", AutoplaceControl::default()),
                    ("trees", AutoplaceControl::default()),
                    ("uranium-ore", AutoplaceControl::default()),
                ]
                .map(|(k, v)| (k.to_owned(), v)),
            ),
            autoplace_settings: HashMap::new(),
            cliff_settings: CliffPlacementSettings::default(),

            seed: None,
            width: 0,
            height: 0,
            starting_area: 1.0,
            starting_points: vec![MapPosition { x: 0.0, y: 0.0 }],
            peaceful_mode: false,
            property_expression_names: {
                let mut names = HashMap::new();
                names.insert(
                    "control-setting:moisture:frequency:multiplier".to_owned(),
                    "1".to_owned(),
                );
                names.insert("control-setting:moisture:bias".to_owned(), "0".to_owned());
                names.insert(
                    "control-setting:aux:frequency:multiplier".to_owned(),
                    "1".to_owned(),
                );
                names.insert("control-setting:aux:bias".to_owned(), "0".to_owned());
                names
            },
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct AutoplaceSettings {
    pub treat_missing_as_default: bool,
    pub settings: HashMap<String, AutoplaceControl>,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct CliffPlacementSettings {
    /// Name of the cliff prototype.
    pub name: String,

    /// Elevation of first row of cliffs.
    pub cliff_elevation_0: f32,

    /// Elevation difference between consecutive rows of cliffs. This is inversely proportional to "frequency" in the
    /// map generation GUI. Specificall, when set from the GUI the value is `40 / frequency`.
    pub cliff_elevation_interval: f32,

    /// Called "cliff continuity" in the map generator GUI. 0 will result in no cliffs, 10 will make all cliff rows
    /// completely solid.
    pub richness: f32,
}

impl Default for CliffPlacementSettings {
    fn default() -> Self {
        Self {
            name: "cliff".to_owned(),
            cliff_elevation_0: 10.0,
            cliff_elevation_interval: 40.0,
            richness: 1.0,
        }
    }
}

/// A position on the map.
#[derive(Serialize, Deserialize, Debug)]
pub struct MapPosition {
    /// The x coordinate.
    pub x: f64,

    /// The y coordinate.
    pub y: f64,
}
