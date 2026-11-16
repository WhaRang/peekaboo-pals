using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Services.Packs.Dto;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace _Main.Scripts.Editor
{
	/// <summary>
	/// Turns the authoring folders in &lt;project&gt;/Packs (one folder per party, see PACKS.md) into the files the app
	/// ships in Assets/StreamingAssets/Packs: validated pack.json files with hashed codes (never the secrets), the
	/// media, a manifest.json per pack (every file's hash, so phones can verify downloads and skip unchanged packs)
	/// and index.json. Missing QR secrets are generated into each pack's codes.json, which stays on this computer.
	/// Runs automatically before every player build. "Update Website Folder" copies the result into the last web
	/// build, so a new collection reaches every phone with one Netlify upload and no app rebuild.
	/// </summary>
	public static class PackBuilder
	{
		public static readonly string SourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PackLibrary.FolderName));
		public static readonly string OutputRoot = Path.Combine(Application.streamingAssetsPath, PackLibrary.FolderName);
		public const string SiteFile = "site.json";
		public const string CodesFile = "codes.json";
		public const string WebBuildFolder = "Builds/WebGL";
		/// <summary>Where a web build keeps the packs, relative to the site root (Unity copies StreamingAssets there).</summary>
		public const string WebPacksPath = "StreamingAssets/" + PackLibrary.FolderName + "/";

		private static readonly Regex PackIdRule = new(@"^[a-z0-9]+(-[a-z0-9]+)*$");
		private static readonly Regex CharacterIdRule = new(@"^[a-z0-9]+([_-][a-z0-9]+)*$");
		private static readonly string[] VoiceExtensions = { ".wav", ".mp3", ".ogg" };

		[MenuItem("Party/Packs/Build Packs", priority = 0)]
		public static void BuildMenu()
		{
			bool ok = Build(out var report);
			Debug.Log(report);
			if (!Application.isBatchMode)
				EditorUtility.DisplayDialog(ok ? "Packs built" : "Pack problems", ok ? "Collection packs are ready. Build the app as usual." : "Some packs have errors. See the Console.", "OK");
		}

		/// <summary>
		/// Batch mode: <c>-executeMethod _Main.Scripts.Editor.PackBuilder.BatchBuild</c>. Exits with 1 on errors.
		/// Optional <c>-packsSource &lt;dir&gt; -packsOutput &lt;dir&gt;</c> build another folder (e.g. a test website).
		/// </summary>
		public static void BatchBuild()
		{
			bool ok = Build(Argument("-packsSource") ?? SourceRoot, Argument("-packsOutput") ?? OutputRoot, out var report);
			Debug.Log(report);
			EditorApplication.Exit(ok ? 0 : 1);
		}

		[MenuItem("Party/Packs/Update Website Folder", priority = 10)]
		public static void UpdateWebsiteMenu()
		{
			bool ok = UpdateWebsite(out var report);
			Debug.Log(report);
			if (Application.isBatchMode)
				return;

			if (ok)
			{
				EditorUtility.DisplayDialog("Website folder ready",
					$"The packs in {WebBuildFolder} are up to date.\n\nNetlify → your site → Deploys → drag the {WebBuildFolder} folder onto the drop area. " +
					"Phones pick the new collections up the next time they open the app online.", "OK");
				EditorUtility.RevealInFinder(Path.Combine(WebBuildRoot, "index.html"));
			}
			else
			{
				EditorUtility.DisplayDialog("Website folder not updated", report.Length > 600 ? report.Substring(0, 600) + "…" : report, "OK");
			}
		}

		/// <summary>Batch mode: <c>-executeMethod _Main.Scripts.Editor.PackBuilder.BatchUpdateWebsite</c>.</summary>
		public static void BatchUpdateWebsite()
		{
			bool ok = UpdateWebsite(out var report);
			Debug.Log(report);
			EditorApplication.Exit(ok ? 0 : 1);
		}

		private static string WebBuildRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", WebBuildFolder));

		/// <summary>Builds the packs and puts them into the existing web build (no player rebuild, takes seconds).</summary>
		public static bool UpdateWebsite(out string report)
		{
			if (!File.Exists(Path.Combine(WebBuildRoot, "index.html")))
			{
				report = $"[PackBuilder] There's no web build in {WebBuildFolder} yet. Run Party > Build WebGL once (it includes the packs).";
				return false;
			}

			if (!Build(out report))
				return false;

			var target = Path.Combine(WebBuildRoot, "StreamingAssets", PackLibrary.FolderName);
			if (Directory.Exists(target))
				Directory.Delete(target, true);
			CopyFolder(OutputRoot, target);
			report += $"\n[PackBuilder] Website folder updated: {target}\n  Next: Netlify → Deploys → drag the {WebBuildFolder} folder onto the drop area.";
			return true;
		}

		private static void CopyFolder(string from, string to)
		{
			Directory.CreateDirectory(to);
			foreach (var file in Directory.GetFiles(from))
			{
				if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
					File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
			}

			foreach (var folder in Directory.GetDirectories(from))
				CopyFolder(folder, Path.Combine(to, Path.GetFileName(folder)));
		}

		private static string? Argument(string name)
		{
			var args = Environment.GetCommandLineArgs();
			int index = Array.IndexOf(args, name);
			return index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : null;
		}

		[MenuItem("Party/Packs/Open Packs Folder", priority = 20)]
		public static void OpenSource() => EditorUtility.RevealInFinder(Path.Combine(SourceRoot, SiteFile));

		public sealed class Site
		{
			public string? LinkBase;
			public string? Featured;
			public List<UpcomingPackDto>? Upcoming;
			/// <summary>Where phones download the Android app (the Google Drive link); shown when a newer version is out.</summary>
			public string? AndroidDownload;
			/// <summary>Optional: the online Packs folder, when it's not the web build's own (linkBase + StreamingAssets/Packs/).</summary>
			public string? PacksUrl;
		}

		public sealed class SourcePack
		{
			public string Folder = string.Empty;
			public PackDto Dto = null!;
			public Dictionary<string, string> Codes = new(StringComparer.OrdinalIgnoreCase);
		}

		public static Site LoadSite(string? sourceRoot = null)
		{
			var path = Path.Combine(sourceRoot ?? SourceRoot, SiteFile);
			return File.Exists(path) ? JsonConvert.DeserializeObject<Site>(File.ReadAllText(path)) ?? new Site() : new Site();
		}

		/// <summary>Every authoring folder in Packs/ (folders starting with "_" or "." are templates/ignored).</summary>
		public static List<SourcePack> LoadSources(List<string> errors, string? sourceRoot = null)
		{
			sourceRoot ??= SourceRoot;
			var packs = new List<SourcePack>();
			if (!Directory.Exists(sourceRoot))
			{
				errors.Add($"No {sourceRoot} folder.");
				return packs;
			}

			foreach (var folder in Directory.GetDirectories(sourceRoot).OrderBy(f => f, StringComparer.Ordinal))
			{
				var name = Path.GetFileName(folder);
				if (name.StartsWith("_") || name.StartsWith("."))
					continue;

				var jsonPath = Path.Combine(folder, PackLibrary.PackFile);
				if (!File.Exists(jsonPath))
				{
					errors.Add($"{name}: no {PackLibrary.PackFile}.");
					continue;
				}

				try
				{
					var dto = JsonConvert.DeserializeObject<PackDto>(File.ReadAllText(jsonPath)) ?? new PackDto();
					var codesPath = Path.Combine(folder, CodesFile);
					var codes = File.Exists(codesPath) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(codesPath)) : null;
					packs.Add(new SourcePack
					{
						Folder = folder,
						Dto = dto,
						Codes = new Dictionary<string, string>(codes ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
					});
				}
				catch (Exception e)
				{
					errors.Add($"{name}/{PackLibrary.PackFile} is not valid JSON: {e.Message}");
				}
			}

			return packs;
		}

		public static bool Build(out string report) => Build(SourceRoot, OutputRoot, out report);

		public static bool Build(string sourceRoot, string outputRoot, out string report)
		{
			var errors = new List<string>();
			var warnings = new List<string>();
			var notes = new List<string>();
			var site = LoadSite(sourceRoot);
			var sources = LoadSources(errors, sourceRoot);
			var rules = AssetDatabase.LoadAssetAtPath<CollectionConfig>("Assets/_Main/Configs/CollectionConfig.asset");

			var legacyPrefixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var built = new List<(SourcePack Source, PackDto Shipped, List<string> Files)>();

			foreach (var source in sources)
			{
				var shipped = Validate(source, rules, errors, warnings, out var files);
				if (shipped == null)
					continue;

				if (!string.IsNullOrEmpty(shipped.LegacyPrefix))
				{
					if (legacyPrefixes.TryGetValue(shipped.LegacyPrefix!, out var other))
						errors.Add($"{shipped.Id}: legacyPrefix '{shipped.LegacyPrefix}' is already used by {other}.");
					else
						legacyPrefixes[shipped.LegacyPrefix!] = shipped.Id!;
				}

				int generated = EnsureCodes(source, shipped);
				if (generated > 0)
					notes.Add($"{shipped.Id}: generated {generated} new QR secret(s) in {CodesFile}. Print/export the tags again (Party > Packs > Export QR Sheets).");

				foreach (var character in shipped.Characters!)
					character.Code = PackCodes.Hash(shipped.Id!, character.Id!, source.Codes[character.Id!]);

				built.Add((source, shipped, files));
			}

			if (built.Count == 0)
				errors.Add("No valid packs to ship.");

			var ids = built.Select(b => b.Shipped.Id!).ToList();
			string? featured = site.Featured;
			if (!string.IsNullOrEmpty(featured) && !ids.Contains(featured!))
			{
				warnings.Add($"{SiteFile}: featured '{featured}' is not a pack; the newest pack opens first instead.");
				featured = null;
			}

			if (string.IsNullOrWhiteSpace(site.AndroidDownload))
				warnings.Add($"{SiteFile}: no \"androidDownload\" link, so phones can't be sent to a new app version.");

			if (errors.Count == 0)
				Write(outputRoot, built, featured, site);

			report = Report(outputRoot, built.Select(b => b.Shipped).ToList(), errors, warnings, notes);
			return errors.Count == 0;
		}

		// ------------------------------------------------------------------ Validation

		private static PackDto? Validate(SourcePack source, CollectionConfig? rules, List<string> errors, List<string> warnings, out List<string> files)
		{
			files = new List<string>();
			var dto = source.Dto;
			string folderName = Path.GetFileName(source.Folder);
			string where = folderName;
			int errorCount = errors.Count;

			string id = dto.Id?.Trim() ?? string.Empty;
			if (id != folderName)
				errors.Add($"{where}: \"id\" must be the folder name ('{folderName}'), got '{id}'.");
			if (!PackIdRule.IsMatch(folderName))
				errors.Add($"{where}: the folder name must be lowercase letters, digits and dashes (e.g. christmas-2026).");
			if (string.IsNullOrWhiteSpace(dto.Title))
				errors.Add($"{where}: \"title\" is missing.");
			if (!string.IsNullOrEmpty(dto.Date) && !DateTime.TryParseExact(dto.Date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
				errors.Add($"{where}: \"date\" must look like 2026-12-24.");
			if (string.IsNullOrEmpty(dto.Date))
				warnings.Add($"{where}: no \"date\"; the album shelf sorts it last.");

			var theme = dto.Theme ??= new PackThemeDto();
			CheckColor(where, "theme.accent", theme.Accent, errors);
			CheckColor(where, "theme.accent2", theme.Accent2, errors);
			theme.Logo = CheckFile(source, where, "theme.logo", theme.Logo, files, errors);
			theme.Cover = CheckFile(source, where, "theme.cover", theme.Cover, files, errors);
			theme.Background = CheckFile(source, where, "theme.background", theme.Background, files, errors);
			theme.Scenery = CheckFile(source, where, "theme.scenery", theme.Scenery, files, errors);
			if (!string.IsNullOrEmpty(theme.Ambient) && !Enum.TryParse<AmbientStyle>(theme.Ambient, true, out _))
				errors.Add($"{where}: theme.ambient must be one of {string.Join(", ", Enum.GetNames(typeof(AmbientStyle)).Select(n => n.ToLowerInvariant()))}.");

			if (dto.Texts?.Ranks != null && dto.Texts.Ranks.Count != 6)
				warnings.Add($"{where}: texts.ranks needs exactly 6 entries (none, <30%, <60%, <90%, almost, complete); the default ranks are used.");

			foreach (var pair in dto.Rarities ?? new Dictionary<string, PackRarityDto>())
			{
				if (!Enum.TryParse<CharacterRarity>(pair.Key, true, out _))
					errors.Add($"{where}: rarities.{pair.Key} is not a rarity (common, rare, epic, legendary).");
				CheckColor(where, $"rarities.{pair.Key}.color", pair.Value.Color, errors);
				CheckColor(where, $"rarities.{pair.Key}.secondary", pair.Value.Secondary, errors);
				CheckColor(where, $"rarities.{pair.Key}.glow", pair.Value.Glow, errors);
			}

			if (rules != null && dto.FloatGrades != null && dto.FloatGrades.Count > rules.FloatGrades.Count)
				warnings.Add($"{where}: floatGrades has {dto.FloatGrades.Count} names but the game has {rules.FloatGrades.Count} grades; extra names are ignored.");

			var characters = dto.Characters ?? new List<PackCharacterDto>();
			if (characters.Count == 0)
				errors.Add($"{where}: no characters.");

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var c in characters)
			{
				string cid = c.Id?.Trim() ?? string.Empty;
				string at = $"{where}/{(cid.Length > 0 ? cid : "?")}";
				if (!CharacterIdRule.IsMatch(cid))
					errors.Add($"{at}: \"id\" must be lowercase letters and digits (dashes/underscores between them).");
				else if (!seen.Add(cid))
					errors.Add($"{at}: duplicate id.");
				c.Id = cid;

				if (string.IsNullOrWhiteSpace(c.Name))
					errors.Add($"{at}: \"name\" is missing.");
				if (!Enum.TryParse<CharacterRarity>(c.Rarity ?? string.Empty, true, out var rarity))
					errors.Add($"{at}: \"rarity\" must be common, rare, epic or legendary.");
				else if (rules != null)
				{
					var tier = rules.GetTier(rarity);
					if (c.Power < tier.PowerRange.x || c.Power > tier.PowerRange.y)
						warnings.Add($"{at}: power {c.Power} is outside the usual {rarity} range {tier.PowerRange.x}-{tier.PowerRange.y}.");
				}

				if (c.Power < 1 || c.Power > 99)
					errors.Add($"{at}: \"power\" must be 1-99.");
				CheckColor(at, "color", c.Color, errors);
				if (!string.IsNullOrEmpty(c.Signature) && !Enum.TryParse<CharacterSignature>(c.Signature, true, out _))
					errors.Add($"{at}: \"signature\" must be one of {string.Join(", ", Enum.GetNames(typeof(CharacterSignature)).Select(n => n.ToLowerInvariant()))}.");

				c.Art = CheckFile(source, at, "art", string.IsNullOrWhiteSpace(c.Art) ? $"art/{cid}.png" : c.Art, files, errors, required: true);
				if (c.Art != null)
					CheckArt(Path.Combine(source.Folder, c.Art), at, warnings);
				c.Silhouette = CheckFile(source, at, "silhouette", c.Silhouette, files, errors);

				if (string.IsNullOrWhiteSpace(c.Voice))
					c.Voice = VoiceExtensions.Select(ext => $"voice/{cid}{ext}").FirstOrDefault(v => File.Exists(Path.Combine(source.Folder, v)));
				c.Voice = CheckFile(source, at, "voice", c.Voice, files, errors);
				if (c.Voice != null && c.Voice.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
					warnings.Add($"{at}: .ogg voices don't play in iPhone Safari; prefer .wav or .mp3.");

				c.Code = null;
			}

			if (characters.All(c => !string.Equals(c.Rarity, "legendary", StringComparison.OrdinalIgnoreCase)))
				warnings.Add($"{where}: no legendary character. Totally fine, but the legendary reveal is the best part.");

			// The oldest app that can open the pack: what pack.json asks for, raised to the newest feature it uses.
			if (!string.IsNullOrWhiteSpace(dto.MinAppVersion) && !AppVersion.TryParse(dto.MinAppVersion, out _))
				errors.Add($"{where}: \"minAppVersion\" must look like 2.1.");
			string required = AppVersion.Max(dto.MinAppVersion?.Trim(), FeatureVersion<AmbientStyle>(theme.Ambient));
			foreach (var c in characters)
				required = AppVersion.Max(required, FeatureVersion<CharacterSignature>(c.Signature));
			dto.MinAppVersion = string.IsNullOrEmpty(required) ? null : required;
			if (AppVersion.IsNewer(dto.MinAppVersion, PlayerSettings.bundleVersion))
				warnings.Add($"{where}: needs app {dto.MinAppVersion}, but the app is version {PlayerSettings.bundleVersion}. Raise Player Settings → Version and ship the app first, or phones show it as \"update the app\".");

			return errors.Count == errorCount ? dto : null;
		}

		/// <summary>The [SinceAppVersion] of an enum value named in pack.json, if any.</summary>
		private static string? FeatureVersion<T>(string? value) where T : struct, Enum
		{
			if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse<T>(value!.Trim(), true, out var parsed))
				return null;
			var field = typeof(T).GetField(parsed.ToString());
			return field?.GetCustomAttributes(typeof(SinceAppVersionAttribute), false).OfType<SinceAppVersionAttribute>().FirstOrDefault()?.Version;
		}

		private static void CheckColor(string where, string field, string? value, List<string> errors)
		{
			if (!string.IsNullOrWhiteSpace(value) && !ColorUtility.TryParseHtmlString(value, out _))
				errors.Add($"{where}: {field} '{value}' is not a color like #ff8a1f.");
		}

		/// <summary>Returns the normalized relative path (forward slashes) when the file exists.</summary>
		private static string? CheckFile(SourcePack source, string where, string field, string? relative, List<string> files, List<string> errors, bool required = false)
		{
			if (string.IsNullOrWhiteSpace(relative))
			{
				if (required)
					errors.Add($"{where}: {field} is missing.");
				return null;
			}

			var normalized = relative!.Trim().Replace('\\', '/').TrimStart('/');
			if (normalized.Contains(".."))
			{
				errors.Add($"{where}: {field} must stay inside the pack folder.");
				return null;
			}

			if (!File.Exists(Path.Combine(source.Folder, normalized)))
			{
				errors.Add($"{where}: {field} file '{normalized}' not found in {Path.GetFileName(source.Folder)}/.");
				return null;
			}

			if (!files.Contains(normalized))
				files.Add(normalized);
			return normalized;
		}

		private static void CheckArt(string path, string where, List<string> warnings)
		{
			var bytes = File.ReadAllBytes(path);
			var texture = new Texture2D(2, 2);
			if (!texture.LoadImage(bytes))
			{
				warnings.Add($"{where}: art is not a readable PNG.");
			}
			else
			{
				if (texture.width != texture.height)
					warnings.Add($"{where}: art is {texture.width}x{texture.height}; square art (512x512) fits the cards best.");
				if (texture.width > 1024)
					warnings.Add($"{where}: art is {texture.width}px wide; 512px is plenty and loads faster.");
				if (bytes.Length > 400 * 1024)
					warnings.Add($"{where}: art is {bytes.Length / 1024} KB; try to keep each image under ~200 KB.");
			}

			UnityEngine.Object.DestroyImmediate(texture);
		}

		// ------------------------------------------------------------------ Codes

		private static int EnsureCodes(SourcePack source, PackDto shipped)
		{
			int generated = 0;
			foreach (var character in shipped.Characters!)
			{
				if (source.Codes.TryGetValue(character.Id!, out var secret) && !string.IsNullOrWhiteSpace(secret))
				{
					source.Codes[character.Id!] = secret.Trim().ToUpperInvariant();
					continue;
				}

				source.Codes[character.Id!] = NewSecret();
				generated++;
			}

			if (generated > 0)
			{
				var ordered = shipped.Characters!.ToDictionary(c => c.Id!, c => source.Codes[c.Id!]);
				foreach (var extra in source.Codes.Where(p => !ordered.ContainsKey(p.Key)))
					ordered[extra.Key] = extra.Value;
				File.WriteAllText(Path.Combine(source.Folder, CodesFile), JsonConvert.SerializeObject(ordered, Formatting.Indented) + "\n");
			}

			return generated;
		}

		public static string NewSecret()
		{
			const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
			var bytes = new byte[6];
			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
				rng.GetBytes(bytes);
			return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
		}

		// ------------------------------------------------------------------ Output

		private static void Write(string outputRoot, List<(SourcePack Source, PackDto Shipped, List<string> Files)> built, string? featured, Site site)
		{
			// A pack whose files didn't change keeps its "updated" time, so phones don't download it again.
			var previous = ReadManifests(outputRoot);
			string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);

			if (Directory.Exists(outputRoot))
				Directory.Delete(outputRoot, true);
			if (File.Exists(outputRoot + ".meta"))
				File.Delete(outputRoot + ".meta");
			Directory.CreateDirectory(outputRoot);

			var settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
				ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
			};
			var entries = new List<PackIndexEntryDto>();
			foreach (var (source, shipped, files) in built)
			{
				var target = Path.Combine(outputRoot, shipped.Id!);
				Directory.CreateDirectory(target);
				var manifestFiles = new List<PackFileDto>();
				foreach (var file in files)
				{
					var destination = Path.Combine(target, file);
					Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
					File.Copy(Path.Combine(source.Folder, file), destination, true);
					manifestFiles.Add(Describe(destination, file));
				}

				var packPath = Path.Combine(target, PackLibrary.PackFile);
				File.WriteAllText(packPath, JsonConvert.SerializeObject(shipped, settings) + "\n");
				manifestFiles.Add(Describe(packPath, PackLibrary.PackFile));

				string hash = PackHashes.OfPack(manifestFiles.Select(f => (f.Path!, f.Hash!)));
				string updated = previous.TryGetValue(shipped.Id!, out var old) && old.Hash == hash && !string.IsNullOrEmpty(old.Updated) ? old.Updated! : now;
				var manifest = new PackManifestDto
				{
					Id = shipped.Id,
					Hash = hash,
					Updated = updated,
					MinAppVersion = shipped.MinAppVersion,
					Files = manifestFiles.OrderBy(f => f.Path, StringComparer.Ordinal).ToList()
				};
				File.WriteAllText(Path.Combine(target, PackLibrary.ManifestFile), JsonConvert.SerializeObject(manifest, settings) + "\n");

				entries.Add(new PackIndexEntryDto
				{
					Id = shipped.Id,
					Title = shipped.Title,
					Date = shipped.Date,
					Hash = hash,
					Updated = updated,
					MinAppVersion = shipped.MinAppVersion,
					Size = manifestFiles.Sum(f => f.Size)
				});
			}

			var index = new PackIndexDto
			{
				BuiltAt = now,
				Featured = featured,
				Packs = entries,
				Upcoming = site.Upcoming,
				Remote = RemoteUrl(site),
				App = new AppReleaseDto { Version = PlayerSettings.bundleVersion, Android = string.IsNullOrWhiteSpace(site.AndroidDownload) ? null : site.AndroidDownload!.Trim() }
			};
			File.WriteAllText(Path.Combine(outputRoot, PackLibrary.IndexFile), JsonConvert.SerializeObject(index, settings) + "\n");
			if (Path.GetFullPath(outputRoot).StartsWith(Path.GetFullPath(Application.dataPath), StringComparison.Ordinal))
				AssetDatabase.Refresh();
		}

		/// <summary>The online Packs folder phones check: site.json's packsUrl, else the web build's own folder.</summary>
		public static string? RemoteUrl(Site site)
		{
			if (!string.IsNullOrWhiteSpace(site.PacksUrl))
				return site.PacksUrl!.Trim().TrimEnd('/') + "/";
			return string.IsNullOrWhiteSpace(site.LinkBase) ? null : site.LinkBase!.Trim().TrimEnd('/') + "/" + WebPacksPath;
		}

		private static PackFileDto Describe(string path, string relative)
		{
			var bytes = File.ReadAllBytes(path);
			return new PackFileDto { Path = relative, Hash = PackHashes.OfBytes(bytes), Size = bytes.LongLength };
		}

		private static Dictionary<string, PackManifestDto> ReadManifests(string outputRoot)
		{
			var manifests = new Dictionary<string, PackManifestDto>(StringComparer.OrdinalIgnoreCase);
			if (!Directory.Exists(outputRoot))
				return manifests;

			foreach (var folder in Directory.GetDirectories(outputRoot))
			{
				var path = Path.Combine(folder, PackLibrary.ManifestFile);
				if (!File.Exists(path))
					continue;
				try
				{
					var manifest = JsonConvert.DeserializeObject<PackManifestDto>(File.ReadAllText(path));
					if (manifest?.Id != null)
						manifests[manifest.Id] = manifest;
				}
				catch (Exception)
				{
					// Unreadable: treated as changed.
				}
			}

			return manifests;
		}

		private static string Report(string outputRoot, List<PackDto> packs, List<string> errors, List<string> warnings, List<string> notes)
		{
			var lines = new List<string> { $"[PackBuilder] {(errors.Count == 0 ? "OK" : "FAILED")}: {packs.Count} pack(s) → {outputRoot}" };
			lines.AddRange(packs.Select(p => $"  • {p.Id}: \"{p.Title}\", {p.Characters!.Count} characters" + (p.MinAppVersion != null ? $", needs app {p.MinAppVersion}+" : string.Empty)));
			lines.AddRange(errors.Select(e => "  ERROR " + e));
			lines.AddRange(warnings.Select(w => "  warning " + w));
			lines.AddRange(notes.Select(n => "  note " + n));
			return string.Join("\n", lines);
		}

		/// <summary>Every player build ships freshly built packs (and stops when a pack has errors).</summary>
		private sealed class BuildHook : IPreprocessBuildWithReport
		{
			public int callbackOrder => 0;

			public void OnPreprocessBuild(BuildReport report)
			{
				bool ok = Build(out var text);
				Debug.Log(text);
				if (!ok)
					throw new BuildFailedException("Collection packs have errors (see the log above). Fix them in the Packs folder.");
			}
		}
	}
}
