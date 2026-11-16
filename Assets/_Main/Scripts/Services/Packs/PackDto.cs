using System.Collections.Generic;
using UnityEngine.Scripting;

// JSON shapes of the shipped pack files (written by Party > Packs > Build Packs, documented in PACKS.md).
// Plain public fields: Newtonsoft fills them by (case-insensitive) name; link.xml keeps them from being stripped.
namespace _Main.Scripts.Services.Packs.Dto
{
	/// <summary>
	/// <c>Packs/index.json</c>: which packs exist (with their content hash, so phones can tell what changed), which one
	/// the app opens on first launch, teaser books, where the online copy lives and the latest app release.
	/// The same file ships inside the app and on the website; phones keep a copy of the website's one.
	/// </summary>
	[Preserve]
	public sealed class PackIndexDto
	{
		public const int CurrentVersion = 2;

		public int Version = CurrentVersion;
		/// <summary>UTC ISO-8601 time of the build; the newer of the shipped and the downloaded index wins.</summary>
		public string? BuiltAt;
		public string? Featured;
		public List<PackIndexEntryDto>? Packs;
		public List<UpcomingPackDto>? Upcoming;
		/// <summary>URL of the online Packs folder (ends with a slash), checked for new and updated packs.</summary>
		public string? Remote;
		public AppReleaseDto? App;
	}

	[Preserve]
	public sealed class PackIndexEntryDto
	{
		public string? Id;
		public string? Title;
		public string? Date;
		/// <summary>Hash over every file of the pack (see <see cref="PackManifestDto"/>).</summary>
		public string? Hash;
		/// <summary>UTC ISO-8601 time the content last changed: a phone only downloads a pack newer than its own copy.</summary>
		public string? Updated;
		/// <summary>Oldest app version that can open this pack; older apps show it as "update the app".</summary>
		public string? MinAppVersion;
		/// <summary>Total bytes, shown nowhere yet; handy when reading the file.</summary>
		public long Size;
	}

	/// <summary><c>Packs/&lt;id&gt;/manifest.json</c>: every file of a pack with its hash, so downloads can be verified.</summary>
	[Preserve]
	public sealed class PackManifestDto
	{
		public string? Id;
		public string? Hash;
		public string? Updated;
		public string? MinAppVersion;
		public List<PackFileDto>? Files;
	}

	[Preserve]
	public sealed class PackFileDto
	{
		/// <summary>Relative to the pack folder, forward slashes (pack.json included).</summary>
		public string? Path;
		public string? Hash;
		public long Size;
	}

	/// <summary>The newest app release: phones running an older version are told where to get it.</summary>
	[Preserve]
	public sealed class AppReleaseDto
	{
		public string? Version;
		/// <summary>Where the Android APK is downloaded (the Google Drive link).</summary>
		public string? Android;
	}

	/// <summary>A teaser book on the album shelf for a party that has no pack yet.</summary>
	[Preserve]
	public sealed class UpcomingPackDto
	{
		public string? Title;
		/// <summary>yyyy-MM-dd.</summary>
		public string? Date;
	}

	/// <summary><c>Packs/&lt;id&gt;/pack.json</c>.</summary>
	[Preserve]
	public sealed class PackDto
	{
		public string? Id;
		public string? Title;
		/// <summary>yyyy-MM-dd, the party date (sorts the album shelf, newest first).</summary>
		public string? Date;
		public string? Tagline;
		/// <summary>Old payload prefix (<c>PREFIX:id:secret</c>) still accepted for this pack, e.g. "SPOOKY".</summary>
		public string? LegacyPrefix;
		/// <summary>Optional: the oldest app version allowed to open this pack (e.g. "2.1" when it relies on a new effect).</summary>
		public string? MinAppVersion;
		public PackThemeDto? Theme;
		public PackTextsDto? Texts;
		/// <summary>Keys: common, rare, epic, legendary.</summary>
		public Dictionary<string, PackRarityDto>? Rarities;
		/// <summary>Names for the float grades, best first (same count as the game's grades).</summary>
		public List<string>? FloatGrades;
		public List<PackCharacterDto>? Characters;
	}

	[Preserve]
	public sealed class PackThemeDto
	{
		public string? Accent;
		public string? Accent2;
		public string? Logo;
		public string? Cover;
		public string? Background;
		public string? Scenery;
		public bool? Moon;
		/// <summary>sparkles, embers, snow, confetti, hearts, none.</summary>
		public string? Ambient;
		public bool? Bats;
	}

	[Preserve]
	public sealed class PackTextsDto
	{
		public string? Friends;
		public string? HaulTitle;
		public List<string>? Ranks;
	}

	[Preserve]
	public sealed class PackRarityDto
	{
		public string? Name;
		public string? Color;
		public string? Secondary;
		public string? Glow;
	}

	[Preserve]
	public sealed class PackCharacterDto
	{
		public string? Id;
		public string? Name;
		/// <summary>common, rare, epic, legendary.</summary>
		public string? Rarity;
		public int Power;
		public string? Description;
		public string? Color;
		/// <summary>Path inside the pack folder; defaults to art/&lt;id&gt;.png.</summary>
		public string? Art;
		/// <summary>Optional custom locked silhouette; generated from the art when missing.</summary>
		public string? Silhouette;
		/// <summary>Path inside the pack folder (.wav, .mp3 or .ogg); optional.</summary>
		public string? Voice;
		/// <summary>none, robot.</summary>
		public string? Signature;
		public string? Teaser;
		/// <summary>Shipped files only: hash of the QR secret (never the secret itself).</summary>
		public string? Code;
	}
}
