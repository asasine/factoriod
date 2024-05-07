//! Map settings for the game, used during map generation.

use serde::{Deserialize, Serialize};

/// Settings for the map. These settings are used to configure the map generation and the game difficulty and are saved
/// in the `map-settings.json` file. This struct models the
/// [`MapAndDifficultySettings`](https://lua-api.factorio.com/latest/concepts.html#MapAndDifficultySettings) type in
/// the Factorio Lua API.
#[derive(Serialize, Deserialize, Debug)]
pub struct MapAndDifficultySettings {
    pub difficulty_settings: DifficultySettings,
    pub pollution: PollutionMapSettings,
    pub enemy_evolution: EnemyEvolutionMapSettings,
    pub enemy_expansion: EnemyExpansionMapSettings,
    pub unit_group: UnitGroupMapSettings,
    pub steering: SteeringMapSettings,
    pub path_finder: PathFinderMapSettings,
    pub max_failed_behavior_count: u32,
}

impl Default for MapAndDifficultySettings {
    fn default() -> Self {
        Self {
            difficulty_settings: DifficultySettings::default(),
            pollution: PollutionMapSettings::default(),
            enemy_evolution: EnemyEvolutionMapSettings::default(),
            enemy_expansion: EnemyExpansionMapSettings::default(),
            unit_group: UnitGroupMapSettings::default(),
            steering: SteeringMapSettings::default(),
            path_finder: PathFinderMapSettings::default(),
            max_failed_behavior_count: 3,
        }
    }
}

/// Settings for the difficulty of the game.
#[derive(Serialize, Deserialize, Debug)]
pub struct DifficultySettings {
    pub recipe_difficulty: RecipeDifficulty,
    pub technology_difficulty: TechnologyDifficulty,
    pub technology_price_multiplier: f64,
    pub research_queue_setting: ResearchQueueSetting,
}

impl DifficultySettings {}

impl Default for DifficultySettings {
    fn default() -> Self {
        Self {
            recipe_difficulty: RecipeDifficulty::Normal,
            technology_difficulty: TechnologyDifficulty::Normal,
            technology_price_multiplier: 1.0,
            research_queue_setting: ResearchQueueSetting::AfterVictory,
        }
    }
}

/// Settings for enemy evolution.
#[derive(Serialize, Deserialize, Debug)]
pub struct EnemyEvolutionMapSettings {
    /// Whether enemy evolution is enabled.
    pub enabled: bool,

    /// The time factor for enemy evolution.
    pub time_factor: f64,

    /// The factor for enemy evolution when a spawner is destroyed.
    pub destroy_factor: f64,

    /// The factor for enemy evolution from pollution.
    pub pollution_factor: f64,
}

impl Default for EnemyEvolutionMapSettings {
    fn default() -> Self {
        Self {
            enabled: true,
            time_factor: 0.000004,
            destroy_factor: 0.002,
            pollution_factor: 0.0000009,
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct EnemyExpansionMapSettings {
    pub enabled: bool,
    pub max_expansion_distance: u32,
    pub friendly_base_influence_radius: u32,
    pub enemy_building_influence_radius: u32,
    pub building_coefficient: f64,
    pub other_base_coefficient: f64,
    pub neighbouring_chunk_coefficient: f64,
    pub neighbouring_base_chunk_coefficient: f64,
    pub max_colliding_tiles_coefficient: f64,
    pub settler_group_min_size: u32,
    pub settler_group_max_size: u32,
    pub min_expansion_cooldown: u32,
    pub max_expansion_cooldown: u32,
}

impl Default for EnemyExpansionMapSettings {
    fn default() -> Self {
        Self {
            enabled: true,
            max_expansion_distance: 7,
            friendly_base_influence_radius: 2,
            enemy_building_influence_radius: 2,
            building_coefficient: 0.1,
            other_base_coefficient: 2.0,
            neighbouring_chunk_coefficient: 0.5,
            neighbouring_base_chunk_coefficient: 0.4,
            max_colliding_tiles_coefficient: 0.9,
            settler_group_min_size: 5,
            settler_group_max_size: 20,
            min_expansion_cooldown: 14000,
            max_expansion_cooldown: 216000,
        }
    }
}

/// Settings for unit path finding.
#[derive(Serialize, Deserialize, Debug)]
pub struct PathFinderMapSettings {
    pub fwd_2_bwd_ratio: u32,
    pub goal_pressure_ratio: f64,
    pub max_steps_worked_per_tick: f64,
    pub max_work_done_per_tick: u32,
    pub use_path_cache: bool,
    pub short_cache_size: u32,
    pub long_cache_size: u32,
    pub short_cache_min_cacheable_distance: f64,
    pub short_cache_min_algo_steps_to_cache: u32,
    pub long_cache_min_cacheable_distance: f64,
    pub cache_max_connect_to_cache_steps_multiplier: u32,
    pub cache_accept_path_start_distance_ratio: f64,
    pub cache_accept_path_end_distance_ratio: f64,
    pub negative_cache_accept_path_start_distance_ratio: f64,
    pub negative_cache_accept_path_end_distance_ratio: f64,
    pub cache_path_start_distance_rating_multiplier: f64,
    pub cache_path_end_distance_rating_multiplier: f64,
    pub stale_enemy_with_same_destination_collision_penalty: f64,
    pub ignore_moving_enemy_collision_distance: f64,
    pub enemy_with_different_destination_collision_penalty: f64,
    pub general_entity_collision_penalty: f64,
    pub general_entity_subsequent_collision_penalty: f64,
    pub extended_collision_penalty: f64,
    pub max_clients_to_accept_any_new_request: u32,
    pub max_clients_to_accept_short_new_request: u32,
    pub direct_distance_to_consider_short_request: u32,
    pub short_request_max_steps: u32,
    pub short_request_ratio: f64,
    pub min_steps_to_check_path_find_termination: u32,
    pub start_to_goal_cost_multiplier_to_terminate_path_find: f64,
    pub overload_levels: Vec<u32>,
    pub overload_multipliers: Vec<u32>,
    pub negative_path_cache_delay_interval: u32,
}

impl Default for PathFinderMapSettings {
    fn default() -> Self {
        Self {
            fwd_2_bwd_ratio: 5,
            goal_pressure_ratio: 2.0,
            max_steps_worked_per_tick: 1000.0,
            max_work_done_per_tick: 8000,
            use_path_cache: true,
            short_cache_size: 5,
            long_cache_size: 25,
            short_cache_min_cacheable_distance: 10.0,
            short_cache_min_algo_steps_to_cache: 50,
            long_cache_min_cacheable_distance: 30.0,
            cache_max_connect_to_cache_steps_multiplier: 100,
            cache_accept_path_start_distance_ratio: 0.2,
            cache_accept_path_end_distance_ratio: 0.15,
            negative_cache_accept_path_start_distance_ratio: 0.3,
            negative_cache_accept_path_end_distance_ratio: 0.3,
            cache_path_start_distance_rating_multiplier: 10.0,
            cache_path_end_distance_rating_multiplier: 20.0,
            stale_enemy_with_same_destination_collision_penalty: 30.0,
            ignore_moving_enemy_collision_distance: 5.0,
            enemy_with_different_destination_collision_penalty: 30.0,
            general_entity_collision_penalty: 10.0,
            general_entity_subsequent_collision_penalty: 3.0,
            extended_collision_penalty: 3.0,
            max_clients_to_accept_any_new_request: 10,
            max_clients_to_accept_short_new_request: 100,
            direct_distance_to_consider_short_request: 100,
            short_request_max_steps: 1000,
            short_request_ratio: 0.5,
            min_steps_to_check_path_find_termination: 2000,
            start_to_goal_cost_multiplier_to_terminate_path_find: 2000.0,
            overload_levels: vec![0, 100, 500],
            overload_multipliers: vec![2, 3, 4],
            negative_path_cache_delay_interval: 20,
        }
    }
}

/// Pollution settings for the map.
#[derive(Serialize, Deserialize, Debug)]
pub struct PollutionMapSettings {
    /// Whether pollution is enabled.
    pub enabled: bool,
    pub diffusion_ratio: f64,

    /// These are values for 60 ticks (1 simulated second) amount that is diffused to neighboring chunks.
    pub min_to_diffuse: f64,
    pub ageing: f64,
    pub expected_max_per_chunk: f64,
    pub min_to_show_per_chunk: f64,
    pub min_pollution_to_damage_trees: f64,
    pub pollution_with_max_forest_damage: f64,
    pub pollution_per_tree_damage: f64,
    pub pollution_restored_per_tree_damage: f64,
    pub max_pollution_to_restore_trees: f64,
    pub enemy_attack_pollution_consumption_modifier: f64,
}

impl Default for PollutionMapSettings {
    fn default() -> Self {
        Self {
            enabled: true,
            diffusion_ratio: 0.02,
            min_to_diffuse: 15.0,
            ageing: 1.0,
            expected_max_per_chunk: 150.0,
            min_to_show_per_chunk: 50.0,
            min_pollution_to_damage_trees: 60.0,
            pollution_with_max_forest_damage: 150.0,
            pollution_per_tree_damage: 50.0,
            pollution_restored_per_tree_damage: 10.0,
            max_pollution_to_restore_trees: 20.0,
            enemy_attack_pollution_consumption_modifier: 1.0,
        }
    }
}

/// Difficulty of the recipes.
#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "snake_case")]
pub enum RecipeDifficulty {
    /// Recipes have normal difficulty.
    Normal,

    /// Recipes are more expensive.
    Expensive,
}

/// Whether the research queue should be enabled.
#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "snake_case")]
pub enum ResearchQueueSetting {
    /// The research queue is enabled after victory.
    AfterVictory,

    /// The research queue is always enabled from the start of the game.
    Always,

    /// The research queue is never enabled.
    Never,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct SteeringMapSettings {
    pub default: SteeringMapSetting,
    pub moving: SteeringMapSetting,
}

impl Default for SteeringMapSettings {
    fn default() -> Self {
        Self {
            default: SteeringMapSetting {
                radius: 1.2,
                separation_factor: 1.2,
                separation_force: 0.005,
                force_unit_fuzzy_goto_behavior: false,
            },
            moving: SteeringMapSetting {
                radius: 3.0,
                separation_factor: 3.0,
                separation_force: 0.01,
                force_unit_fuzzy_goto_behavior: false,
            },
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct SteeringMapSetting {
    pub radius: f64,
    pub separation_factor: f64,
    pub separation_force: f64,
    pub force_unit_fuzzy_goto_behavior: bool,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "snake_case")]
pub enum TechnologyDifficulty {
    Normal,
    Expensive,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct UnitGroupMapSettings {
    pub min_group_gathering_time: u32,
    pub max_group_gathering_time: u32,
    pub max_wait_time_for_late_members: u32,
    pub min_group_radius: f64,
    pub max_group_radius: f64,
    pub max_member_speedup_when_behind: f64,
    pub max_member_slowdown_when_ahead: f64,
    pub max_group_slowdown_factor: f64,
    pub max_group_member_fallback_factor: f64,
    pub member_disown_distance: f64,
    pub tick_tolerance_when_member_arrives: u32,
    pub max_gathering_unit_groups: u32,
    pub max_unit_group_size: u32,
}

impl Default for UnitGroupMapSettings {
    fn default() -> Self {
        Self {
            min_group_gathering_time: 3600,
            max_group_gathering_time: 36000,
            max_wait_time_for_late_members: 7200,
            min_group_radius: 5.0,
            max_group_radius: 30.0,
            max_member_speedup_when_behind: 1.4,
            max_member_slowdown_when_ahead: 0.6,
            max_group_slowdown_factor: 0.3,
            max_group_member_fallback_factor: 3.0,
            member_disown_distance: 10.0,
            tick_tolerance_when_member_arrives: 60,
            max_gathering_unit_groups: 30,
            max_unit_group_size: 200,
        }
    }
}
