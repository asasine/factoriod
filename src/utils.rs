//! Utility functions for *factoriod*.

use std::path::{Path, PathBuf};
use std::time::SystemTime;

use crate::daemon::factorio_server::{FactorioServerStartError, Result};

/// Get the modification time of a file or the default time if the file does not exist.
fn mtime_or_default<P: AsRef<Path>>(path: P) -> SystemTime {
    path
        .as_ref()
        .metadata()
        .map(|meta| meta.modified().ok())
        .ok()
        .flatten()
        .unwrap_or(SystemTime::UNIX_EPOCH)
}

/// Get all zip files in the given directory. This function will return an iterator over all zip files in the directory.
fn get_zips<'a, P: AsRef<Path>>(dir: P) -> Result<impl Iterator<Item = PathBuf> + 'a> {
    let dir = dir.as_ref().canonicalize()
        .map_err(|_| FactorioServerStartError::PathNotFound(dir.as_ref().to_path_buf().clone()))?;

    let zips = dir.read_dir()
        .map_err(|source| FactorioServerStartError::StartFailed {
            path: dir.clone(),
            source,
        })?
        .filter_map(|entry| entry.ok())
        .filter(|entry| entry.file_type().map(|ft| ft.is_file()).unwrap_or(false))
        .filter(|entry| entry.path().extension().map(|ext| ext == "zip").unwrap_or(false))
        .map(|entry| entry.path());

    Ok(zips)
}

/// Gets all save files in the given directory. This function will return a list of paths to all files in the
/// directory, sorted by their modification time in a descending manner (most recently modified is first). If no save
/// files are found in the directory, this function will return an empty list.
///
/// # Errors
/// If the `save_dir` does not exist, this function will return [`FactorioServerStartError::PathNotFound`].
/// If an error occurs while reading the directory, this function will return [`FactorioServerStartError::StartFailed`].
///
/// # Examples
/// ```
/// use factoriod::get_saves;
/// let saves = get_saves("/path/to/saves").unwrap();
/// ```
pub fn get_saves<P: AsRef<Path>>(save_dir: P) -> Result<Vec<PathBuf>> {
    let mut saves = get_zips(save_dir)?
        .collect::<Vec<_>>();

    saves.sort_by_cached_key(|path| mtime_or_default(&path));
    saves.reverse();

    Ok(saves)
}

/// Gets the latest save file in the given directory. This function will return the path to the save file with the most
/// recent modification time. If no save files are found in the directory, this function will return an error.
///
/// # Errors
/// If the `save_dir` does not exist, this function will return [`FactorioServerStartError::PathNotFound`].
/// If an error occurs while reading the directory, this function will return [`FactorioServerStartError::StartFailed`].
/// If the `save_dir` does not contain any save files, this function will return [`FactorioServerStartError::NoSaveFound`].
///
/// # Examples
/// ```
/// use factoriod::get_latest_save;
/// let latest_save = get_latest_save("/path/to/saves").unwrap();
/// ```
pub fn get_latest_save<P: AsRef<Path>>(save_dir: P) -> Result<PathBuf> {
    let latest_save = get_zips(&save_dir)?
        .max_by_key(|path| mtime_or_default(&path));

    latest_save.ok_or_else(|| FactorioServerStartError::NoSaveFound(save_dir.as_ref().into()))
}

#[cfg(test)]
mod tests {
    use std::collections::HashSet;
    use std::fs::File;
    use std::io::Write;
    use std::time::SystemTime;

    use tempfile::tempdir;

    use super::*;

    #[test]
    fn test_mtime_or_default() {
        // sanity check
        assert_ne!(SystemTime::now(), SystemTime::UNIX_EPOCH);

        let temp_dir = tempdir().unwrap();
        let file = temp_dir.path().join("test_mtime_or_default");
        assert_eq!(mtime_or_default(&file), SystemTime::UNIX_EPOCH);

        File::create(&file).unwrap().write_all(b"test").unwrap();
        assert_ne!(mtime_or_default(&file), SystemTime::UNIX_EPOCH);
    }

    /// Create a temporary directory with the given files. The files will be created in the order provided, and each
    /// file will have a different modification time. Dropping the returned `tempfile::TempDir` will remove the files.
    fn create_tempdir_with_files(files: &[&str]) -> (tempfile::TempDir, Vec<PathBuf>) {
        let temp_dir = tempdir().unwrap();
        let files = files
            .iter()
            .map(|name| temp_dir.path().join(name))
            .collect::<Vec<_>>();

        for path in &files {
            File::create(path).unwrap();

            // sleep to ensure that the mtime is different
            std::thread::sleep(std::time::Duration::from_secs(1));
        }

        (temp_dir, files)
    }

    #[test]
    fn test_get_zips() {
        assert!(get_zips("/does/not/exist").is_err());
        let (temp_dir, expected) = create_tempdir_with_files(&["a.zip", "b.zip", "c.zip"]);

        File::create(temp_dir.path().join("d.txt")).unwrap();

        let actual = get_zips(&temp_dir).unwrap().collect::<HashSet<_>>();
        assert_eq!(actual, expected.into_iter().collect());
    }

    #[test]
    fn test_get_saves() {
        assert!(get_saves("/does/not/exist").is_err());
        let (temp_dir, _expected) = create_tempdir_with_files(&[]);
        assert!(get_saves(&temp_dir).unwrap().is_empty());

        let (temp_dir, expected) = create_tempdir_with_files(&["a.zip", "b.zip", "c.zip"]);
        let actual = get_saves(&temp_dir).unwrap();

        // get_saves orders by mtime, descending, so we need to reverse the expected list
        assert_eq!(actual, expected.into_iter().rev().collect::<Vec<_>>());
    }

    #[test]
    fn test_get_latest_save() {
        assert!(get_latest_save("/does/not/exist").is_err());
        let (temp_dir, expected) = create_tempdir_with_files(&["a.zip", "b.zip", "c.zip"]);
        let actual = get_latest_save(&temp_dir).unwrap();

        assert_eq!(actual, expected[2]);
    }
}
