//! Types for map generation size settings.

use nutype::nutype;
use serde::{Deserialize, Serialize};

/// A newtype for a floating point value representing a map generation size. This value must be in the range `[0, 6]`.
///
/// # Errors
/// The [`Self::try_from`] function returns a [`Result<Self, MapGenSizeFloatError>`] when the value:
/// 1. is not finite; or
/// 1. is less than 0; or
/// 1. is greater than 6.
///
/// # Examples
/// A valid value can be created using the [`Self::try_from`] function and the value can be accessed using the [`From`]
/// or [`Into`] trait implementations:
/// ```
/// use factorio_config::MapGenSizeFloat;
/// let size = MapGenSizeFloat::try_from(1.0).expect("valid map gen size");
/// assert_eq!(f32::from(size), 1.0);
/// let size: f32 = size.into();
/// assert_eq!(size, 1.0);
/// ```
///
/// An invalid value can be checked against the [`MapGenSizeFloatError`] enum:
/// ```
/// use factorio_config::{MapGenSizeFloat, MapGenSizeFloatError};
/// assert_eq!(MapGenSizeFloat::try_from(-1.0), Err(MapGenSizeFloatError::GreaterOrEqualViolated));
/// assert_eq!(MapGenSizeFloat::try_from(7.0), Err(MapGenSizeFloatError::LessOrEqualViolated));
/// assert_eq!(MapGenSizeFloat::try_from(f32::NAN), Err(MapGenSizeFloatError::FiniteViolated));
/// assert_eq!(MapGenSizeFloat::try_from(f32::INFINITY), Err(MapGenSizeFloatError::FiniteViolated));
/// ```
#[nutype(
    validate(finite, greater_or_equal = 0.0, less_or_equal = 6.0),
    derive(
        Clone,
        Copy,
        Debug,
        Deserialize,
        Eq,
        Ord,
        PartialEq,
        PartialOrd,
        Serialize,
        TryFrom
    )
)]
pub struct MapGenSizeFloat(f32);

impl From<MapGenSizeFloat> for f32 {
    fn from(size: MapGenSizeFloat) -> f32 {
        size.into_inner()
    }
}

/// All possible values for a map generation size. This models the
/// [`MapGenSize`](https://lua-api.factorio.com/latest/concepts.html#MapGenSize) type from the Factorio Lua API.
#[derive(Clone, Debug, Deserialize, PartialEq, Serialize)]
#[serde(rename_all = "kebab-case")]
pub enum MapGenSize {
    // Equivalent to `0`.
    None,

    // Equivalent to `1/2`.
    VeryLow,

    // Equivalent to `1/2`.
    VerySmall,

    // Equivalent to `1/2`.
    VeryPoor,

    // Equivalent to `1/sqrt(2)`.
    Low,

    // Equivalent to `1/sqrt(2)`.
    Small,

    // Equivalent to `1/sqrt(2)`.
    Poor,

    // Equivalent to `1`.
    Normal,

    // Equivalent to `1`.
    Medium,

    // Equivalent to `1`.
    Regular,

    // Equivalent to `sqrt(2)`.
    High,

    // Equivalent to `sqrt(2)`.
    Big,

    // Equivalent to `sqrt(2)`.
    Good,

    // Equivalent to `2`.
    VeryHigh,

    // Equivalent to `2`.
    VeryBig,

    // Equivalent to `2`.
    VeryGood,

    /// A floating point value.
    ///
    /// # Examples
    /// Named values are provided for backwards compatibility:
    /// ```
    /// use factorio_config::MapGenSize;
    /// assert_eq!(f32::from(MapGenSize::Normal), 1.0)
    /// ```
    ///
    /// Non-named values can also be created with validation using the [`MapGenSizeFloat::try_from`] function. See
    /// [`MapGenSizeFloat`] for more information since this conversion is fallible.
    /// ```
    /// use factorio_config::{MapGenSize, MapGenSizeFloat};
    /// let size = MapGenSize::Float(MapGenSizeFloat::try_from(1.0).expect("valid map gen size"));
    /// ```
    #[serde(untagged)]
    Float(MapGenSizeFloat),
}

impl Default for MapGenSize {
    fn default() -> Self {
        MapGenSize::Float(MapGenSizeFloat::try_from(1.0).expect("valid map gen size"))
    }
}

impl From<MapGenSize> for f32 {
    fn from(size: MapGenSize) -> f32 {
        match size {
            MapGenSize::Float(f) => f.into(),
            MapGenSize::None => 0.0,
            MapGenSize::VeryLow => 0.5,
            MapGenSize::VerySmall => 0.5,
            MapGenSize::VeryPoor => 0.5,
            MapGenSize::Low => 1.0 / 2.0_f32.sqrt(),
            MapGenSize::Small => 1.0 / 2.0_f32.sqrt(),
            MapGenSize::Poor => 1.0 / 2.0_f32.sqrt(),
            MapGenSize::Normal => 1.0,
            MapGenSize::Medium => 1.0,
            MapGenSize::Regular => 1.0,
            MapGenSize::High => 2.0_f32.sqrt(),
            MapGenSize::Big => 2.0_f32.sqrt(),
            MapGenSize::Good => 2.0_f32.sqrt(),
            MapGenSize::VeryHigh => 2.0,
            MapGenSize::VeryBig => 2.0,
            MapGenSize::VeryGood => 2.0,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    mod map_gen_size_float {
        use super::*;

        #[test]
        fn test_map_gen_size_float_try_from() {
            assert_eq!(
                MapGenSizeFloat::try_from(0.0),
                Ok(MapGenSizeFloat::new(0.0).expect("valid"))
            );
            assert_eq!(
                MapGenSizeFloat::try_from(6.0),
                Ok(MapGenSizeFloat::new(6.0).expect("valid"))
            );
            assert_eq!(
                MapGenSizeFloat::try_from(-1.0),
                Err(MapGenSizeFloatError::GreaterOrEqualViolated)
            );
            assert_eq!(
                MapGenSizeFloat::try_from(7.0),
                Err(MapGenSizeFloatError::LessOrEqualViolated)
            );
            assert_eq!(
                MapGenSizeFloat::try_from(f32::NAN),
                Err(MapGenSizeFloatError::FiniteViolated)
            );
            assert_eq!(
                MapGenSizeFloat::try_from(f32::INFINITY),
                Err(MapGenSizeFloatError::FiniteViolated)
            );
        }

        #[test]
        fn test_map_gen_size_float_f32_from_into() {
            let size = MapGenSizeFloat::try_from(4.2).expect("valid map gen size");
            assert_eq!(f32::from(size), 4.2);
            let size: f32 = size.into();
            assert_eq!(size, 4.2);
        }
    }

    mod map_gen_size {
        use super::*;

        #[test]
        fn test_map_gen_size_f32_from() {
            assert_eq!(
                f32::from(MapGenSize::Float(MapGenSizeFloat::new(4.2).expect("valid"))),
                4.2
            );
            assert_eq!(f32::from(MapGenSize::None), 0.0);
            assert_eq!(f32::from(MapGenSize::VeryLow), 0.5);
            assert_eq!(f32::from(MapGenSize::VerySmall), 0.5);
            assert_eq!(f32::from(MapGenSize::VeryPoor), 0.5);
            assert_eq!(f32::from(MapGenSize::Low), 1.0 / 2.0_f32.sqrt());
            assert_eq!(f32::from(MapGenSize::Small), 1.0 / 2.0_f32.sqrt());
            assert_eq!(f32::from(MapGenSize::Poor), 1.0 / 2.0_f32.sqrt());
            assert_eq!(f32::from(MapGenSize::Normal), 1.0);
            assert_eq!(f32::from(MapGenSize::Medium), 1.0);
            assert_eq!(f32::from(MapGenSize::Regular), 1.0);
            assert_eq!(f32::from(MapGenSize::High), 2.0_f32.sqrt());
            assert_eq!(f32::from(MapGenSize::Big), 2.0_f32.sqrt());
            assert_eq!(f32::from(MapGenSize::Good), 2.0_f32.sqrt());
            assert_eq!(f32::from(MapGenSize::VeryHigh), 2.0);
            assert_eq!(f32::from(MapGenSize::VeryBig), 2.0);
            assert_eq!(f32::from(MapGenSize::VeryGood), 2.0);
        }

        #[test]
        fn test_map_gen_size_default() {
            assert_eq!(f32::from(MapGenSize::default()), 1.0);
        }

        #[test]
        fn test_map_gen_size_serde() {
            use serde_json;

            // named values should serialize to kebab-case, and float values should serialize to their float value
            let sizes = [
                (MapGenSize::None, r#""none""#),
                (MapGenSize::VeryLow, r#""very-low""#),
                (MapGenSize::VerySmall, r#""very-small""#),
                (MapGenSize::VeryPoor, r#""very-poor""#),
                (MapGenSize::Low, r#""low""#),
                (MapGenSize::Small, r#""small""#),
                (MapGenSize::Poor, r#""poor""#),
                (MapGenSize::Normal, r#""normal""#),
                (MapGenSize::Medium, r#""medium""#),
                (MapGenSize::Regular, r#""regular""#),
                (MapGenSize::High, r#""high""#),
                (MapGenSize::Big, r#""big""#),
                (MapGenSize::Good, r#""good""#),
                (MapGenSize::VeryHigh, r#""very-high""#),
                (MapGenSize::VeryBig, r#""very-big""#),
                (MapGenSize::VeryGood, r#""very-good""#),
                (
                    MapGenSize::Float(MapGenSizeFloat::new(4.2).expect("valid")),
                    "4.2",
                ),
            ];

            for (size, json) in sizes.iter() {
                assert_eq!(serde_json::to_string(&size).expect("valid json"), *json);
                assert_eq!(
                    serde_json::from_str::<MapGenSize>(*json).expect("valid json"),
                    *size
                );
            }
        }
    }
}
