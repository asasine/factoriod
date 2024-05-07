//! Server settings for a Factorio server.

use serde::{Deserialize, Serialize};

/// The server settings. These are reflected in the `server-settings.json` file and can be modified after generating a
/// map. The changes will take effect after the server is restarted.
#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "snake_case")]
pub struct ServerSettings {
    /// Name of the game as it will appear in the game listing.
    pub name: String,

    /// Description of the game that will appear in the game listing.
    pub description: String,

    /// Tags that will be used to filter the game listing.
    pub tags: Vec<String>,

    /// Maximum number of players allowed, admins can join even a full server. 0 means unlimited.
    pub max_players: u32,

    /// Visibility of the game in the game listing.
    pub visibility: Visibility,

    /// Your factorio.com login username. Required for games with [`Visibility::public`] set to [`true`].
    pub username: String,

    /// When set to [`true`], the server will only allow clients that have a valid Factorio.com account.
    pub require_user_verification: bool,

    /// Optional. Default value is 0. 0 means unlimited.
    pub max_upload_in_kilobytes_per_second: u32,

    /// Optional. Default value is 5. 0 means unlimited.
    pub max_upload_slots: u32,

    /// Optional. One tick is 16ms in default speed. Default value is 0. 0 means no minimum.
    pub minimum_latency_in_ticks: u32,

    /// Network tick rate. Maximum rate game updates packets are sent at before bundling them together. Minimum value is 6, maximum value is 240.
    pub max_heartbeats_per_second: u32,

    /// Players that played on this map already can join even when the max player limit was reached.
    pub ignore_player_limit_for_returning_players: bool,

    /// Whether to allow players to use commands.
    pub allow_commands: AllowCommands,

    /// Autosave interval in minutes.
    pub autosave_interval: u32,

    /// Server autosave slots. It is cycled through when the server autosaves.
    pub autosave_slots: u32,

    /// How many minutes until someone is kicked when doing nothing. 0 is never.
    pub afk_autokick_interval: u32,

    /// Whether the server should be paused when no players are present.
    pub auto_pause: bool,

    pub only_admins_can_pause_the_game: bool,

    /// Whether autosaves should be saved only on server or also on all connected clients. Default is true.
    pub autosave_only_on_server: bool,

    /// Highly experimental feature, enable on at your own risk of losing your saves. On UNIX systems, server will fork
    /// itself to create an autosave. Autosaving on connected Windows clients will be disabled regardless of
    /// [`Self::autosave_only_on_server`] option.
    pub non_blocking_saving: bool,

    /// Long network messages are split into segments that are sent over multiple ticks. Their size depends on the
    /// number of peers currently connected. Increasing the segment size will increase upload bandwidth requirement for
    /// the server and download bandwidth requirement for clients. This setting only affects server outbound messages.
    /// Changing these settings can have a negative impact on connection stability for some clients.
    pub minimum_segment_size: u32,
    pub minimum_segment_size_peer_count: u32,
    pub maximum_segment_size: u32,
    pub maximum_segment_size_peer_count: u32,
}

impl Default for ServerSettings {
    fn default() -> Self {
        Self {
            name: "factoriod".to_owned(),
            description: "factoriod".to_owned(),
            tags: Vec::new(),
            max_players: 0,
            visibility: Visibility::default(),
            username: "".to_owned(),
            require_user_verification: true,
            max_upload_in_kilobytes_per_second: 0,
            max_upload_slots: 5,
            minimum_latency_in_ticks: 0,
            max_heartbeats_per_second: 60,
            ignore_player_limit_for_returning_players: false,
            allow_commands: AllowCommands::AdminsOnly,
            autosave_interval: 10,
            autosave_slots: 5,
            afk_autokick_interval: 0,
            auto_pause: true,
            only_admins_can_pause_the_game: true,
            autosave_only_on_server: true,
            non_blocking_saving: false,
            minimum_segment_size: 25,
            minimum_segment_size_peer_count: 20,
            maximum_segment_size: 100,
            maximum_segment_size_peer_count: 10,
        }
    }
}

pub struct ServerSettingsWithSecrets {
    pub server_settings: ServerSettings,

    /// Your factorio.com login credentials. Required for games with [`Visibility::public`] set to [`true`].
    pub password: String,

    /// Authentication token. May be used instead of [`Self::password`] for games with [`Visibility::public`] set to
    /// [`true`].
    pub token: String,

    /// Password for joining the game. Empty string means no password.
    pub game_password: String,
}

impl Default for ServerSettingsWithSecrets {
    fn default() -> Self {
        Self {
            server_settings: ServerSettings::default(),
            password: "".to_owned(),
            token: "".to_owned(),
            game_password: "".to_owned(),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "snake_case")]
/// What commands are allowed in the game.
pub enum AllowCommands {
    /// All players can use commands.
    True,

    /// No commands are allowed.
    False,

    /// Only admins can use commands.
    AdminsOnly,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct Visibility {
    /// Game will be published on the official Factorio matching server.
    pub public: bool,

    /// Game will be broadcast on LAN.
    pub lan: bool,
}

impl Default for Visibility {
    fn default() -> Self {
        Self {
            public: true,
            lan: true,
        }
    }
}
