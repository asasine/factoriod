using System.Text.Json.Serialization;

namespace Factoriod.Models.Mods;

/// <summary>
/// A released version of a <see cref="Mod"/>.
/// </summary>
/// <param name="DownloadUrl">The relative download URL.</param>
/// <param name="FileName">The name of the downloaded file.</param>
/// <param name="ReleasedAt">The datetime when the version was released.</param>
/// <param name="Version">The version of the release.</param>
/// <param name="Sha1">The SHA1 hash of the release.</param>
public record ModRelease(string DownloadUrl, string FileName, [property: JsonPropertyName("info_json")] ModReleaseInfo Info, DateTimeOffset ReleasedAt, string Version, string Sha1);

/// <summary>
/// Additional information about a <see cref="ModRelease"/>.
/// </summary>
/// <param name="FactorioVersion">The factorio version this <see cref="ModRelease"/> is compatible with.</param>
/// <param name="Dependencies">The dependencies of this <see cref="ModRelease"/>.</param>
public record ModReleaseInfo(string FactorioVersion, IEnumerable<string>? Dependencies = null);
