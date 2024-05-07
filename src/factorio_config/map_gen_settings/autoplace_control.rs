//! Settings for the placement of a resource.

use serde::{Deserialize, Serialize};

use super::{MapGenSize, MapGenSizeFloat};

/// Settings for the placement of a resource.
#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
pub struct AutoplaceControl {
    /// The frequency of the resource.
    pub frequency: MapGenSize,

    /// The size of the resource.
    pub size: MapGenSize,

    /// The richness of the resource.
    pub richness: MapGenSize,
}

impl Default for AutoplaceControl {
    fn default() -> Self {
        Self {
            frequency: MapGenSize::Float(MapGenSizeFloat::try_from(1.0).expect("valid value")),
            size: MapGenSize::Float(MapGenSizeFloat::try_from(1.0).expect("valid value")),
            richness: MapGenSize::Float(MapGenSizeFloat::try_from(1.0).expect("valid value")),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_default() {
        let default = AutoplaceControl::default();
        assert_eq!(f32::from(default.frequency), 1.0);
        assert_eq!(f32::from(default.size), 1.0);
        assert_eq!(f32::from(default.richness), 1.0);
    }

    #[test]
    fn test_serde() {
        use serde_json;
        assert_eq!(
            serde_json::to_string(&AutoplaceControl::default()).unwrap(),
            r#"{"frequency":1.0,"size":1.0,"richness":1.0}"#
        );
    }
}
