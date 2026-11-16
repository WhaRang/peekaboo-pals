using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Services.Packs.Dto;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace _Main.Scripts.Services.Packs
{
	public readonly struct UpcomingPack
	{
		public string Title { get; }
		public DateTime? Date { get; }

		public UpcomingPack(string title, DateTime? date)
		{
			Title = title;
			Date = date;
		}
	}

	/// <summary>A collection that exists online but needs a newer app than this one.</summary>
	public readonly struct LockedPack
	{
		public string Id { get; }
		public string Title { get; }
		public DateTime? Date { get; }
		public string MinAppVersion { get; }

		public LockedPack(string id, string title, DateTime? date, string minAppVersion)
		{
			Id = id;
			Title = title;
			Date = date;
			MinAppVersion = minAppVersion;
		}
	}

	/// <summary>A newer app release announced by the website (Android only: the web build is always the newest).</summary>
	public sealed class AppRelease
	{
		public string Version { get; }
		public string? DownloadUrl { get; }

		public AppRelease(string version, string? downloadUrl)
		{
			Version = version;
			DownloadUrl = downloadUrl;
		}
	}

	public enum PackCheckStatus
	{
		/// <summary>No online check yet.</summary>
		NotChecked = 0,
		Checking = 1,
		/// <summary>The last check reached the website.</summary>
		UpToDate = 2,
		/// <summary>The last check couldn't reach the website (no internet, site down).</summary>
		Offline = 3,
		/// <summary>This build doesn't check online (the editor, unless PEEKABOO_REMOTE_PACKS is set).</summary>
		Off = 4
	}

	public interface IPackLibrary
	{
		bool IsReady { get; }

		/// <summary>Every loaded pack, newest party first.</summary>
		IReadOnlyList<CollectionPack> Packs { get; }

		/// <summary>Teaser books (minus the ones whose collection has arrived).</summary>
		IReadOnlyList<UpcomingPack> Upcoming { get; }

		/// <summary>Collections that need a newer app.</summary>
		IReadOnlyList<LockedPack> Locked { get; }

		/// <summary>The pack the app opens on first launch: index.json's "featured", else the newest one.</summary>
		CollectionPack DefaultPack { get; }

		/// <summary>Bumped whenever packs are added.</summary>
		int Revision { get; }

		PackCheckStatus CheckStatus { get; }

		/// <summary>Realtime of the last finished online check, or -1.</summary>
		float LastCheckTime { get; }

		/// <summary>Newer versions of packs already on screen were downloaded; they're used from the next launch.</summary>
		bool UpdatesWaiting { get; }

		/// <summary>A newer app version announced by the website, if any.</summary>
		AppRelease? NewerApp { get; }

		/// <summary>A brand-new pack arrived while the app is running.</summary>
		event Action<CollectionPack>? PackAdded;

		/// <summary>Check status, locked packs, teasers or the app release changed.</summary>
		event Action? Changed;

		bool TryGetPack(string id, out CollectionPack pack);
		bool TryGetPackByLegacyPrefix(string prefix, out CollectionPack pack);
		bool TryGetLocked(string id, out LockedPack pack);

		/// <summary>Loads the shipped and the downloaded packs. Run once as a coroutine before the session starts.</summary>
		IEnumerator Load();

		/// <summary>
		/// Asks the website for new or updated packs and downloads them. New packs are added right away
		/// (<see cref="PackAdded"/>), updates of loaded ones wait for the next launch. Safe to call any time:
		/// while a check runs, it just waits for that one.
		/// </summary>
		/// <param name="firstPackId">Download this pack before the others (a code from it was just scanned).</param>
		IEnumerator CheckForUpdates(string? firstPackId = null);
	}

	/// <summary>
	/// Loads the collection packs: the ones shipped in StreamingAssets/Packs (built from the Packs/ folder by
	/// Party > Packs > Build Packs) and the ones the phone downloaded from the website (<see cref="PackCache"/>),
	/// whichever copy is newer. Everything goes through UnityWebRequest, so the same code reads the APK
	/// (jar:file://), the editor and the cache (file://) and the web build (https://). The web build doesn't cache:
	/// its StreamingAssets folder already is the website.
	/// </summary>
	[UsedImplicitly]
	public sealed class PackLibrary : IPackLibrary
	{
		public const string FolderName = "Packs";
		public const string IndexFile = "index.json";
		public const string PackFile = "pack.json";
		public const string ManifestFile = "manifest.json";
		/// <summary>Dev/editor override for the online Packs folder (e.g. a local test server).</summary>
		public const string RemoteOverrideVariable = "PEEKABOO_REMOTE_PACKS";

		private const int IndexTimeoutSeconds = 12;

		private readonly ICollectionRules _rules;
		private readonly List<CollectionPack> _packs = new();
		private readonly List<UpcomingPack> _upcoming = new();
		private readonly List<LockedPack> _locked = new();
		private readonly List<string> _remoteRoots = new();
		/// <summary>Hash and update time of every loaded pack.</summary>
		private readonly Dictionary<string, (string? Hash, string? Updated)> _installed = new(StringComparer.OrdinalIgnoreCase);
		/// <summary>Packs downloaded this run but not loaded (updates for the next launch), by hash.</summary>
		private readonly Dictionary<string, string> _downloaded = new(StringComparer.OrdinalIgnoreCase);
		private readonly PackCache? _cache;
		private readonly bool _onlineEnabled;
		private List<PackIndexEntryDto> _announced = new();
		private AppReleaseDto? _release;
		private string? _featuredId;
		private CollectionPack? _default;
		private bool _checking;

		public bool IsReady { get; private set; }
		public IReadOnlyList<CollectionPack> Packs => _packs;
		public IReadOnlyList<UpcomingPack> Upcoming => _upcoming.Where(u => !_packs.Any(p => string.Equals(p.Title, u.Title, StringComparison.OrdinalIgnoreCase))).ToList();
		public IReadOnlyList<LockedPack> Locked => _locked;
		public CollectionPack DefaultPack => _default ??= CreateEmptyPack();
		public int Revision { get; private set; }
		public PackCheckStatus CheckStatus { get; private set; }
		public float LastCheckTime { get; private set; } = -1f;
		public bool UpdatesWaiting { get; private set; }

		public AppRelease? NewerApp =>
#if UNITY_WEBGL && !UNITY_EDITOR
			null;
#else
			_release != null && AppVersion.IsNewer(_release.Version, AppVersion.Current) ? new AppRelease(_release.Version!.Trim(), _release.Android) : null;
#endif

		public event Action<CollectionPack>? PackAdded;
		public event Action? Changed;

		public PackLibrary(ICollectionRules rules)
		{
			_rules = rules;
			var remoteOverride = RemoteOverride();
			if (remoteOverride != null)
				_remoteRoots.Add(remoteOverride);
#if UNITY_EDITOR
			_onlineEnabled = remoteOverride != null;
#else
			_onlineEnabled = true;
#endif
#if !UNITY_WEBGL || UNITY_EDITOR
			// The editor ignores downloads unless it's testing them, so a stale download never hides pack edits.
			if (_onlineEnabled)
				_cache = new PackCache();
#endif
			CheckStatus = _onlineEnabled ? PackCheckStatus.NotChecked : PackCheckStatus.Off;
		}

		public bool TryGetPack(string id, out CollectionPack pack)
		{
			pack = _packs.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))!;
			return pack != null;
		}

		public bool TryGetPackByLegacyPrefix(string prefix, out CollectionPack pack)
		{
			pack = _packs.FirstOrDefault(p => !string.IsNullOrEmpty(p.LegacyPrefix) && string.Equals(p.LegacyPrefix, prefix, StringComparison.OrdinalIgnoreCase))!;
			return pack != null;
		}

		public bool TryGetLocked(string id, out LockedPack pack)
		{
			pack = _locked.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
			return pack.Id != null;
		}

		// ------------------------------------------------------------------ Loading

		public IEnumerator Load()
		{
			string builtInRoot = StreamingRootUrl();
			string? builtInJson = null;
			yield return PackWeb.GetText(builtInRoot + IndexFile, 0, text => builtInJson = text);
			var builtIn = PackWeb.Parse<PackIndexDto>(builtInJson, IndexFile);
			var cachedIndex = _cache?.ReadIndex();
			var cached = _cache?.ReadPacks() ?? new Dictionary<string, PackManifestDto>();

			// Teasers and the featured pack come from the newer index; announced packs and the app release from both.
			bool cachedIsNewer = IsLater(cachedIndex?.BuiltAt, builtIn?.BuiltAt);
			UseIndex(cachedIsNewer ? cachedIndex : builtIn);
			MergeAnnouncements(cachedIsNewer ? builtIn : cachedIndex);
#if UNITY_WEBGL && !UNITY_EDITOR
			// The page's own folder is the website.
			_remoteRoots.Add(builtInRoot);
#else
			AddRemoteRoot(cachedIndex?.Remote);
			AddRemoteRoot(builtIn?.Remote);
#endif

			// Per pack: the shipped copy, unless the phone downloaded a newer one.
			var sources = new List<(string Id, string Url, string? Hash, string? Updated, string? MinApp, string? FallbackUrl)>();
			foreach (var entry in builtIn?.Packs ?? new List<PackIndexEntryDto>())
			{
				if (string.IsNullOrWhiteSpace(entry.Id))
					continue;

				string id = entry.Id!.Trim();
				string url = $"{builtInRoot}{id}/";
				if (cached.TryGetValue(id, out var download) && download.Hash != entry.Hash && IsLater(download.Updated, entry.Updated))
					sources.Add((id, _cache!.FolderUrl(id), download.Hash, download.Updated, download.MinAppVersion, url));
				else
				{
					if (download != null)
						_cache!.Delete(id); // The app itself now ships the same or a newer copy.
					sources.Add((id, url, entry.Hash, entry.Updated, entry.MinAppVersion, null));
				}

				cached.Remove(id);
			}

			foreach (var pair in cached)
				sources.Add((pair.Key, _cache!.FolderUrl(pair.Key), pair.Value.Hash, pair.Value.Updated, pair.Value.MinAppVersion, null));

			if (sources.Count == 0)
				Debug.LogError($"[{nameof(PackLibrary)}] No packs in {builtInRoot}{IndexFile}. Build them with Party > Packs > Build Packs.");

			foreach (var source in sources)
			{
				if (!AppVersion.Satisfies(source.MinApp))
					continue;

				CollectionPack? pack = null;
				bool fellBack = false;
				yield return LoadPack(source.Url, built => pack = built);
				if (pack == null && source.FallbackUrl != null)
				{
					Debug.LogWarning($"[{nameof(PackLibrary)}] The downloaded {source.Id} is damaged; using the one inside the app.");
					_cache!.Delete(source.Id);
					fellBack = true;
					yield return LoadPack(source.FallbackUrl, built => pack = built);
				}

				if (pack != null)
				{
					_packs.Add(pack);
					// After a fallback the version is unknown, so the next check downloads the pack again.
					_installed[pack.Id] = fellBack ? (null, null) : (source.Hash, source.Updated);
					if (!fellBack && source.Url != $"{builtInRoot}{source.Id}/")
						Debug.Log($"[{nameof(PackLibrary)}] {pack.Id}: using the downloaded copy ({source.Updated}).");
				}
			}

			SortPacks();
			RebuildLocked();
			Revision++;

			Debug.Log($"[{nameof(PackLibrary)}] Loaded {_packs.Count} pack(s): {string.Join(", ", _packs.Select(p => $"{p.Id} ({p.Characters.Count})"))}"
				+ (_locked.Count > 0 ? $"; {_locked.Count} need a newer app" : string.Empty));
			IsReady = true;
		}

		private IEnumerator LoadPack(string packUrl, Action<CollectionPack?> done)
		{
			string? packJson = null;
			yield return PackWeb.GetText(packUrl + PackFile, 0, text => packJson = text);
			var dto = PackWeb.Parse<PackDto>(packJson, packUrl + PackFile);
			if (dto == null)
			{
				done(null);
				yield break;
			}

			yield return BuildPack(dto, packUrl, done);
		}

		private void SortPacks()
		{
			_packs.Sort((a, b) =>
			{
				int byDate = Nullable.Compare(b.Date, a.Date);
				return byDate != 0 ? byDate : string.CompareOrdinal(a.Id, b.Id);
			});

			if (!string.IsNullOrEmpty(_featuredId) && TryGetPack(_featuredId!, out var featured))
				_default = featured;
			else if (_packs.Count > 0)
				_default = _packs[0];
		}

		/// <summary>Teasers, featured pack, the announced packs (for the locked list) and the app release.</summary>
		private void UseIndex(PackIndexDto? index)
		{
			if (index == null)
				return;

			_featuredId = index.Featured;
			_release = index.App;
			_announced = new List<PackIndexEntryDto>(index.Packs ?? new List<PackIndexEntryDto>());
			_upcoming.Clear();
			foreach (var upcoming in index.Upcoming ?? new List<UpcomingPackDto>())
			{
				if (!string.IsNullOrWhiteSpace(upcoming.Title))
					_upcoming.Add(new UpcomingPack(upcoming.Title!, ParseDate(upcoming.Date)));
			}
		}

		private void MergeAnnouncements(PackIndexDto? older)
		{
			if (older == null)
				return;

			if (older.App != null && AppVersion.IsNewer(older.App.Version, _release?.Version))
				_release = older.App;
			foreach (var entry in older.Packs ?? new List<PackIndexEntryDto>())
			{
				if (!string.IsNullOrWhiteSpace(entry.Id) && !_announced.Any(a => string.Equals(a.Id, entry.Id, StringComparison.OrdinalIgnoreCase)))
					_announced.Add(entry);
			}
		}

		private void RebuildLocked()
		{
			_locked.Clear();
			foreach (var entry in _announced)
			{
				if (string.IsNullOrWhiteSpace(entry.Id) || AppVersion.Satisfies(entry.MinAppVersion) || TryGetPack(entry.Id!, out _))
					continue;
				_locked.Add(new LockedPack(entry.Id!.Trim(), string.IsNullOrWhiteSpace(entry.Title) ? entry.Id! : entry.Title!, ParseDate(entry.Date), entry.MinAppVersion!.Trim()));
			}
		}

		private void AddRemoteRoot(string? url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return;
			url = url!.Trim().TrimEnd('/') + "/";
			if (!_remoteRoots.Contains(url))
				_remoteRoots.Add(url);
		}

		private static string? RemoteOverride()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || SPOOKY_AUTOPILOT
			var value = Environment.GetEnvironmentVariable(RemoteOverrideVariable);
			if (!string.IsNullOrWhiteSpace(value))
				return value.Trim().TrimEnd('/') + "/";
#endif
			return null;
		}

		private static string StreamingRootUrl()
		{
			string path = Application.streamingAssetsPath.TrimEnd('/') + "/" + FolderName + "/";
			// Android (jar:file://) and WebGL (https://) already hand out URLs; the editor and desktop give a plain path.
			return path.Contains("://") ? path : new Uri(Path.GetFullPath(path)).AbsoluteUri.TrimEnd('/') + "/";
		}

		// ------------------------------------------------------------------ Online

		public IEnumerator CheckForUpdates(string? firstPackId = null)
		{
			if (!_onlineEnabled || _remoteRoots.Count == 0)
			{
				CheckStatus = PackCheckStatus.Off;
				yield break;
			}

			if (_checking)
			{
				while (_checking)
					yield return null;
				yield break;
			}

			_checking = true;
			CheckStatus = PackCheckStatus.Checking;
			Changed?.Invoke();

			bool reached = false;
			yield return CheckOnline(firstPackId, result => reached = result);

			_checking = false;
			CheckStatus = reached ? PackCheckStatus.UpToDate : PackCheckStatus.Offline;
			LastCheckTime = UnityEngine.Time.realtimeSinceStartup;
			Changed?.Invoke();
		}

		private IEnumerator CheckOnline(string? firstPackId, Action<bool> reached)
		{
			string? json = null;
			string? root = null;
			foreach (var candidate in _remoteRoots.ToList())
			{
				yield return PackWeb.GetText($"{candidate}{IndexFile}?t={DateTime.UtcNow.Ticks}", IndexTimeoutSeconds, text => json = text);
				if (json != null)
				{
					root = candidate;
					break;
				}
			}

			var index = PackWeb.Parse<PackIndexDto>(json, "online " + IndexFile);
			if (index?.Packs == null || root == null)
			{
				reached(false);
				yield break;
			}

			reached(true);
			_cache?.WriteIndex(json!);
			if (_cache != null)
				AddRemoteRoot(index.Remote);
			UseIndex(index);

			foreach (var entry in index.Packs.OrderBy(e => string.Equals(e.Id, firstPackId, StringComparison.OrdinalIgnoreCase) ? 0 : 1))
			{
				if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Hash) || !AppVersion.Satisfies(entry.MinAppVersion))
					continue;

				string id = entry.Id!.Trim();
				bool loaded = _installed.TryGetValue(id, out var have);
				if (loaded && (have.Hash == entry.Hash || !IsLater(entry.Updated, have.Updated)))
					continue;
				if (_downloaded.TryGetValue(id, out var waitingHash) && waitingHash == entry.Hash)
					continue;

				string packUrl;
				if (_cache == null)
				{
					// The web build reads straight from the website; changes to a pack on screen need a page reload.
					if (loaded)
					{
						UpdatesWaiting = true;
						continue;
					}

					packUrl = $"{root}{Uri.EscapeDataString(id)}/";
				}
				else
				{
					bool ok = false;
					yield return _cache.Download(root, entry, result => ok = result);
					if (!ok)
						continue;

					_downloaded[id] = entry.Hash!;
					if (loaded)
					{
						UpdatesWaiting = true;
						continue;
					}

					packUrl = _cache.FolderUrl(id);
				}

				CollectionPack? pack = null;
				yield return LoadPack(packUrl, built => pack = built);
				if (pack == null || TryGetPack(pack.Id, out _))
					continue;

				_packs.Add(pack);
				_installed[pack.Id] = (entry.Hash, entry.Updated);
				_downloaded.Remove(pack.Id);
				SortPacks();
				Revision++;
				Debug.Log($"[{nameof(PackLibrary)}] New collection arrived: {pack.Id} ({pack.Characters.Count} characters).");
				PackAdded?.Invoke(pack);
			}

			RebuildLocked();
		}

		private static bool IsLater(string? a, string? b)
		{
			if (!DateTime.TryParse(a, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var first))
				return false;
			return !DateTime.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var second) || first.ToUniversalTime() > second.ToUniversalTime();
		}

		// ------------------------------------------------------------------ Building

		private IEnumerator BuildPack(PackDto dto, string packUrl, Action<CollectionPack?> done)
		{
			if (string.IsNullOrWhiteSpace(dto.Id) || dto.Characters == null || dto.Characters.Count == 0)
			{
				Debug.LogError($"[{nameof(PackLibrary)}] Skipping {packUrl}: no id or no characters.");
				done(null);
				yield break;
			}

			string packId = dto.Id!.Trim();
			var theme = dto.Theme ?? new PackThemeDto();
			var media = new MediaBatch(packUrl);

			var logo = media.Image(theme.Logo);
			var cover = media.Image(theme.Cover);
			var background = media.Image(theme.Background);
			var scenery = media.Image(theme.Scenery);

			var characterMedia = new List<(PackCharacterDto Dto, MediaBatch.ImageSlot Art, MediaBatch.ImageSlot Silhouette, MediaBatch.AudioSlot Voice)>();
			foreach (var character in dto.Characters)
			{
				if (string.IsNullOrWhiteSpace(character.Id))
					continue;
				var art = media.Image(string.IsNullOrWhiteSpace(character.Art) ? $"art/{character.Id}.png" : character.Art, readable: string.IsNullOrWhiteSpace(character.Silhouette));
				characterMedia.Add((character, art, media.Image(character.Silhouette), media.Audio(character.Voice)));
			}

			yield return media.Run();

			var characters = new List<CharacterDefinition>();
			foreach (var (c, art, silhouette, voice) in characterMedia)
			{
				var rarity = ParseEnum(c.Rarity, CharacterRarity.Common);
				var artSprite = art.Sprite;
				var silhouetteSprite = silhouette.Sprite ?? (art.Texture != null ? CreateSilhouette(art.Texture, c.Id!) : null);
				// The art's CPU copy was only needed for the silhouette.
				if (art.Texture != null && art.Texture.isReadable)
					art.Texture.Apply(false, true);

				characters.Add(new CharacterDefinition(packId, c.Id!.Trim(), c.Name ?? c.Id!, rarity, Mathf.Clamp(c.Power, 1, 99),
					c.Description ?? string.Empty, ParseColor(c.Color, Color.white), artSprite, silhouetteSprite, voice.Clip,
					ParseEnum(c.Signature, CharacterSignature.None), c.Teaser ?? string.Empty, c.Code ?? string.Empty));
			}

			var tiers = _rules.RarityTiers.Select(tier =>
			{
				PackRarityDto? over = null;
				dto.Rarities?.TryGetValue(tier.Rarity.ToString().ToLowerInvariant(), out over);
				return over == null ? tier : tier.WithTheme(over.Name, ParseOptionalColor(over.Color), ParseOptionalColor(over.Secondary), ParseOptionalColor(over.Glow));
			}).ToList();

			var grades = _rules.FloatGrades.Select((grade, i) => grade.WithName(dto.FloatGrades != null && i < dto.FloatGrades.Count ? dto.FloatGrades[i] : null)).ToList();

			var packTheme = new PackTheme(
				ParseColor(theme.Accent, new Color(1f, 0.6f, 0.24f)), ParseColor(theme.Accent2, new Color(0.31f, 0.89f, 0.69f)),
				logo.Sprite, cover.Sprite, background.Sprite, scenery.Sprite, theme.Moon ?? true, ParseEnum(theme.Ambient, AmbientStyle.Sparkles), theme.Bats ?? false);
			var texts = new PackTexts(dto.Tagline, dto.Texts?.Friends, dto.Texts?.HaulTitle, dto.Texts?.Ranks);

			done(new CollectionPack(_rules, packId, string.IsNullOrWhiteSpace(dto.Title) ? packId : dto.Title!, ParseDate(dto.Date), texts.Tagline,
				dto.LegacyPrefix, packTheme, texts, characters, tiers, grades));
		}

		private CollectionPack CreateEmptyPack()
		{
			var texts = new PackTexts("New collections download by themselves when the phone is online", null, null, null);
			var theme = new PackTheme(new Color(1f, 0.6f, 0.24f), new Color(0.31f, 0.89f, 0.69f), null, null, null, null, true, AmbientStyle.Sparkles, false);
			return new CollectionPack(_rules, "none", "No collections yet", null, texts.Tagline, null, theme, texts, new List<CharacterDefinition>(),
				_rules.RarityTiers, _rules.FloatGrades);
		}

		/// <summary>Same rule as Tools/art/build.py: solid where the art is solid, without the soft ground shadow.</summary>
		private static Sprite CreateSilhouette(Texture2D art, string id)
		{
			var pixels = art.GetPixels32();
			for (int i = 0; i < pixels.Length; i++)
			{
				byte a = pixels[i].a;
				pixels[i] = new Color32(255, 255, 255, a > 200 ? (byte)255 : a < 90 ? (byte)0 : a);
			}

			var texture = new Texture2D(art.width, art.height, TextureFormat.RGBA32, false)
			{
				name = id + "_silhouette",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
			texture.SetPixels32(pixels);
			texture.Apply(false, true);
			return MediaBatch.ToSprite(texture);
		}

		// ------------------------------------------------------------------ Parsing

		private static T ParseEnum<T>(string? value, T fallback) where T : struct
			=> !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value!.Trim(), true, out var parsed) ? parsed : fallback;

		private static Color ParseColor(string? hex, Color fallback) => ParseOptionalColor(hex) ?? fallback;

		private static Color? ParseOptionalColor(string? hex)
			=> !string.IsNullOrWhiteSpace(hex) && ColorUtility.TryParseHtmlString(hex!.Trim(), out var color) ? color : null;

		private static DateTime? ParseDate(string? value)
			=> DateTime.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

		/// <summary>Downloads a pack's images and sounds in parallel.</summary>
		private sealed class MediaBatch
		{
			public sealed class ImageSlot
			{
				public Texture2D? Texture;
				public Sprite? Sprite;
			}

			public sealed class AudioSlot
			{
				public AudioClip? Clip;
			}

			private readonly string _baseUrl;
			private readonly List<(UnityWebRequest Request, Action<UnityWebRequest> OnSuccess)> _requests = new();

			public MediaBatch(string baseUrl)
			{
				_baseUrl = baseUrl;
			}

			public ImageSlot Image(string? file, bool readable = false)
			{
				var slot = new ImageSlot();
				if (string.IsNullOrWhiteSpace(file))
					return slot;

				string name = Path.GetFileNameWithoutExtension(file);
				_requests.Add((UnityWebRequest.Get(_baseUrl + file!.Trim()), request =>
				{
					var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
					if (!texture.LoadImage(request.downloadHandler.data, !readable))
					{
						Debug.LogError($"[{nameof(PackLibrary)}] {request.url} is not a PNG/JPG image.");
						UnityEngine.Object.Destroy(texture);
						return;
					}

					slot.Texture = texture;
					slot.Sprite = ToSprite(texture);
				}));
				return slot;
			}

			public AudioSlot Audio(string? file)
			{
				var slot = new AudioSlot();
				if (string.IsNullOrWhiteSpace(file))
					return slot;

				var type = Path.GetExtension(file).ToLowerInvariant() switch
				{
					".mp3" => AudioType.MPEG,
					".ogg" => AudioType.OGGVORBIS,
					_ => AudioType.WAV
				};
				_requests.Add((UnityWebRequestMultimedia.GetAudioClip(_baseUrl + file!.Trim(), type), request =>
				{
					var clip = DownloadHandlerAudioClip.GetContent(request);
					if (clip != null)
						clip.name = Path.GetFileNameWithoutExtension(file);
					slot.Clip = clip;
				}));
				return slot;
			}

			public IEnumerator Run()
			{
				foreach (var (request, _) in _requests)
					request.SendWebRequest();

				while (_requests.Any(r => !r.Request.isDone))
					yield return null;

				foreach (var (request, onSuccess) in _requests)
				{
					if (request.result == UnityWebRequest.Result.Success)
					{
						try
						{
							onSuccess(request);
						}
						catch (Exception e)
						{
							Debug.LogError($"[{nameof(PackLibrary)}] Could not load {request.url}: {e.Message}");
						}
					}
					else
					{
						Debug.LogError($"[{nameof(PackLibrary)}] Could not download {request.url}: {request.error}");
					}

					request.Dispose();
				}

				_requests.Clear();
			}

			public static Sprite ToSprite(Texture2D texture)
			{
				var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
				sprite.name = texture.name;
				return sprite;
			}
		}
	}
}
