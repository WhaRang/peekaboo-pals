using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Main.Scripts._ProjectAgnostic.GameplaySessionLifetime.Factory;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Scoring;
using _Main.Scripts.Configs.Time;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Ui;
using _Main.Scripts.Services.Packs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _Main.Scripts.Editor
{
	/// <summary>
	/// One-click (and batch-mode) project setup for Peekaboo Pals: imports TMP Essentials, configures art/audio
	/// importers, creates the config assets (seeding the game rules only when missing), the session prefab and the
	/// game scene, and sets the Android player settings. Safe to re-run: existing configs keep their edits.
	/// The characters live in collection packs (Packs/, built by <see cref="PackBuilder"/>), not here.
	/// </summary>
	public static class SpookyProjectBuilder
	{
		public const string ProductName = "Peekaboo Pals";

		public const string MainRoot = "Assets/_Main";
		public const string ScenePath = "Assets/Scenes/SpookyCollector.unity";
		private const string ConfigsRoot = MainRoot + "/Configs";
		private const string PrefabPath = MainRoot + "/Prefabs/HuntSession.prefab";
		private const string ArtUi = MainRoot + "/Art/UI";
		private const string AudioGenerated = MainRoot + "/Audio/Generated";
		private const string AudioKenney = MainRoot + "/Audio/Kenney";
		private const string Fonts = MainRoot + "/Fonts";

		private static readonly Dictionary<string, int> SliceBorders = new()
		{
			{ "rounded_rect", 50 }, { "rounded_rect_small", 26 }, { "pill", 40 }, { "card_frame", 56 }, { "card_frame_metal", 56 }, { "chip_metal", 40 }
		};

		private static readonly Dictionary<UiSprite, string> SpriteFiles = new()
		{
			{ UiSprite.RoundedRect, "rounded_rect" }, { UiSprite.RoundedRectSmall, "rounded_rect_small" }, { UiSprite.Pill, "pill" },
			{ UiSprite.Circle, "circle" }, { UiSprite.SoftGlow, "soft_glow" }, { UiSprite.CardFrame, "card_frame" },
			{ UiSprite.CardInner, "card_inner" }, { UiSprite.CardBack, "card_back" }, { UiSprite.Shine, "shine" },
			{ UiSprite.Rays, "rays" }, { UiSprite.Background, "background" }, { UiSprite.Moon, "moon" }, { UiSprite.Hills, "hills" },
			{ UiSprite.Vignette, "vignette" }, { UiSprite.GradientVertical, "gradient_vertical" },
			{ UiSprite.IconScan, "icon_scan" }, { UiSprite.IconCollection, "icon_collection" }, { UiSprite.IconResults, "icon_results" },
			{ UiSprite.IconSoundOn, "icon_sound_on" }, { UiSprite.IconSoundOff, "icon_sound_off" }, { UiSprite.IconClose, "icon_close" },
			{ UiSprite.IconCandy, "icon_candy" }, { UiSprite.IconStar, "icon_star" }, { UiSprite.IconClock, "icon_clock" },
			{ UiSprite.IconPal, "icon_pal" }, { UiSprite.IconCamera, "icon_camera" }, { UiSprite.IconKeyboard, "icon_keyboard" },
			{ UiSprite.IconPower, "icon_power" }, { UiSprite.IconTrophy, "icon_trophy" }, { UiSprite.Logo, "logo" },
			{ UiSprite.ScanCorner, "scan_corner" }, { UiSprite.Banner, "banner" }, { UiSprite.Badge, "badge" },
			{ UiSprite.IconEdit, "icon_edit" }, { UiSprite.IconDice, "icon_dice" }, { UiSprite.CardFrameMetal, "card_frame_metal" },
			{ UiSprite.Scratches, "scratches" }, { UiSprite.ChipMetal, "chip_metal" }, { UiSprite.IconInfo, "icon_info" }, { UiSprite.IconAlbums, "icon_albums" }
		};

		[MenuItem("Party/Build Game Scene", priority = 0)]
		public static void BuildAll()
		{
			if (!EnsureTmpEssentials())
				return;

			ConfigureImporters();
			var configs = CreateConfigs(resetRules: false);
			var prefab = CreateSessionPrefab();
			CreateScene(configs, prefab);
			ConfigurePlayer();
			AssetDatabase.SaveAssets();
			Debug.Log("[SpookyProjectBuilder] Done. Open Assets/Scenes/SpookyCollector.unity and press Play.");
		}

		[MenuItem("Party/Reset Game Rules To Defaults", priority = 20)]
		public static void ResetRules()
		{
			if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Reset game rules?", "Overwrite the rarity tiers and float grades in the CollectionConfig with the defaults? Collections (Packs/) are not touched.", "Reset", "Cancel"))
				return;

			CreateConfigs(resetRules: true);
			AssetDatabase.SaveAssets();
		}

		// ------------------------------------------------------------------ TMP

		private static bool HasTmpEssentials => AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null;

		/// <summary>
		/// Package imports are queued until the editor is idle, so the builder stops after starting the import and
		/// continues automatically once it completed (interactive editor). In batch mode use <see cref="BatchImportTmpEssentials"/>.
		/// </summary>
		public static bool EnsureTmpEssentials()
		{
			if (HasTmpEssentials)
				return true;

			if (!TryGetTmpPackagePath(out var path))
				return false;

			AssetDatabase.importPackageCompleted += ContinueAfterImport;
			AssetDatabase.ImportPackage(path, false);
			Debug.Log("[SpookyProjectBuilder] Importing TMP Essential Resources; the build continues when the import finishes.");
			return false;
		}

		private static void ContinueAfterImport(string packageName)
		{
			AssetDatabase.importPackageCompleted -= ContinueAfterImport;
			if (HasTmpEssentials)
				EditorApplication.delayCall += BuildAll;
		}

		/// <summary>Batch mode: <c>-executeMethod ...BatchImportTmpEssentials</c> (without -quit); exits when done.</summary>
		public static void BatchImportTmpEssentials()
		{
			if (HasTmpEssentials || !TryGetTmpPackagePath(out var path))
			{
				EditorApplication.Exit(HasTmpEssentials ? 0 : 1);
				return;
			}

			AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
			AssetDatabase.importPackageFailed += (_, error) =>
			{
				Debug.LogError($"[SpookyProjectBuilder] TMP import failed: {error}");
				EditorApplication.Exit(1);
			};
			AssetDatabase.ImportPackage(path, false);
		}

		private static bool TryGetTmpPackagePath(out string path)
		{
			var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
			path = package == null ? string.Empty : Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
			if (package != null && File.Exists(path))
				return true;

			Debug.LogError("[SpookyProjectBuilder] Could not find TMP Essential Resources. Import them via Window > TextMeshPro > Import TMP Essential Resources, then run the builder again.");
			return false;
		}

		// ------------------------------------------------------------------ Importers

		public static void ConfigureImporters()
		{
			foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { MainRoot + "/Art" }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
					continue;

				string name = Path.GetFileNameWithoutExtension(path);
				bool isAtlas = name == "particles";

				importer.textureType = isAtlas ? TextureImporterType.Default : TextureImporterType.Sprite;
				if (!isAtlas)
				{
					importer.spriteImportMode = SpriteImportMode.Single;
					var settings = new TextureImporterSettings();
					importer.ReadTextureSettings(settings);
					settings.spriteMeshType = SpriteMeshType.FullRect;
					settings.spriteGenerateFallbackPhysicsShape = false;
					importer.SetTextureSettings(settings);
					importer.spriteBorder = SliceBorders.TryGetValue(name, out int border) ? new Vector4(border, border, border, border) : Vector4.zero;
				}

				importer.alphaIsTransparency = true;
				importer.mipmapEnabled = false;
				importer.wrapMode = TextureWrapMode.Clamp;
				importer.filterMode = FilterMode.Bilinear;
				importer.maxTextureSize = 2048;
				importer.textureCompression = TextureImporterCompression.CompressedHQ;
				importer.SaveAndReimport();
			}

			foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { MainRoot + "/Audio" }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
					continue;

				bool isMusic = path.Contains("music_");
				importer.forceToMono = true;
				var settings = importer.defaultSampleSettings;
				settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
				settings.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
				settings.quality = 0.55f;
				settings.preloadAudioData = !isMusic;
				importer.defaultSampleSettings = settings;
				importer.SaveAndReimport();
			}
		}

		// ------------------------------------------------------------------ Configs

		public sealed class Configs
		{
			public TimeConfig Time = null!;
			public ScoringConfig Scoring = null!;
			public CollectionConfig Collection = null!;
			public AudioConfig Audio = null!;
			public UiThemeConfig Theme = null!;
		}

		public static Configs CreateConfigs(bool resetRules)
		{
			Directory.CreateDirectory(ConfigsRoot);
			var configs = new Configs
			{
				Time = LoadOrCreate<TimeConfig>("TimeConfig", out _),
				Scoring = LoadOrCreate<ScoringConfig>("ScoringConfig", out _),
				Collection = LoadOrCreate<CollectionConfig>("CollectionConfig", out bool collectionCreated),
				Audio = LoadOrCreate<AudioConfig>("AudioConfig", out _),
				Theme = LoadOrCreate<UiThemeConfig>("UiThemeConfig", out _)
			};

			// The hunt is untimed and combo-free.
			var scoring = new SerializedObject(configs.Scoring);
			scoring.FindProperty("<MinComboMultiplier>k__BackingField").intValue = 1;
			scoring.FindProperty("<MaxComboMultiplier>k__BackingField").intValue = 1;
			scoring.ApplyModifiedPropertiesWithoutUndo();

			var time = new SerializedObject(configs.Time);
			time.FindProperty("_gameSessionDurationInSeconds").longValue = 60L * 60 * 24 * 365;
			time.FindProperty("_countdownAnimationDurationInSeconds").longValue = 0;
			time.FindProperty("_saveIntervalInSeconds").longValue = 5;
			time.ApplyModifiedPropertiesWithoutUndo();

			if (collectionCreated || resetRules || configs.Collection.RarityTiers.Count == 0)
				SeedTiers(configs.Collection);

			if (collectionCreated || resetRules || configs.Collection.FloatGrades.Count == 0)
				SeedFloatGrades(configs.Collection);

			FillAudio(configs.Audio);
			FillTheme(configs.Theme);

			foreach (var asset in new Object[] { configs.Time, configs.Scoring, configs.Collection, configs.Audio, configs.Theme })
				EditorUtility.SetDirty(asset);

			return configs;
		}

		private static T LoadOrCreate<T>(string name, out bool created) where T : ScriptableObject
		{
			var path = $"{ConfigsRoot}/{name}.asset";
			var asset = AssetDatabase.LoadAssetAtPath<T>(path);
			created = asset == null;
			if (asset != null)
				return asset;

			asset = ScriptableObject.CreateInstance<T>();
			AssetDatabase.CreateAsset(asset, path);
			return asset;
		}

		/// <summary>Default tier names are generic; each pack renames them (Halloween: Spooky / Scary / Boo / Halloween).</summary>
		private static void SeedTiers(CollectionConfig config)
		{
			var tiers = new List<RarityTierDefinition>
			{
				RarityTierDefinition.EditorCreate(CharacterRarity.Common, "Cuddly", "Common", 10, new Vector2Int(10, 30),
					Hex("#ff8a1f"), Hex("#7ccf3a"), Hex("#ffb347"), false, 1, RevealStyle.Quick, 0.15f, 45, 0f, HapticStrength.Light, Clip(AudioGenerated, "stinger_common")),
				RarityTierDefinition.EditorCreate(CharacterRarity.Rare, "Sneaky", "Rare", 25, new Vector2Int(40, 60),
					Hex("#9b5cff"), Hex("#5b2a9e"), Hex("#c9a2ff"), false, 2, RevealStyle.Burst, 0.75f, 80, 14f, HapticStrength.Medium, Clip(AudioGenerated, "stinger_rare")),
				RarityTierDefinition.EditorCreate(CharacterRarity.Epic, "Wild", "Epic", 50, new Vector2Int(70, 85),
					Hex("#3ee86a"), Hex("#ff2fd0"), Hex("#b6ff3d"), false, 3, RevealStyle.Flip, 1.4f, 130, 24f, HapticStrength.Heavy, Clip(AudioGenerated, "stinger_epic")),
				RarityTierDefinition.EditorCreate(CharacterRarity.Legendary, "Mythic", "Legendary", 100, new Vector2Int(99, 99),
					Hex("#8fe9ff"), Hex("#0f1a3d"), Hex("#7df9ff"), true, 4, RevealStyle.Cinematic, 2.6f, 220, 34f, HapticStrength.Epic, Clip(AudioGenerated, "fanfare_legendary")),
			};

			config.EditorSetTiers(100, tiers);
		}

		/// <summary>CS-style wear bands, best first. Weights are percentages; bonus points stack on the tier points.</summary>
		private static void SeedFloatGrades(CollectionConfig config)
		{
			var grades = new List<FloatGradeDefinition>
			{
				FloatGradeDefinition.EditorCreate("Perfect Peek", 0.01f, 1f, 100, Hex("#7df9ff"), 1f, 1.4f, 1.35f, 0f, 0f, 1f, Hex("#3dd9ff"), Hex("#ff6cf0")),
				FloatGradeDefinition.EditorCreate("Fresh Find", 0.07f, 6f, 40, Hex("#ffd23f"), 0.85f, 1.28f, 1.25f, 0f, 0f, 0.75f, Hex("#ff7a12"), Hex("#fff07a")),
				FloatGradeDefinition.EditorCreate("Lightly Loved", 0.15f, 12f, 20, Hex("#c9a2ff"), 0.55f, 1.12f, 1.12f, 0f, 0.05f, 0.48f, Hex("#ff6a1a"), Hex("#ffc04a")),
				FloatGradeDefinition.EditorCreate("Well Hugged", 0.38f, 36f, 8, Hex("#8fe05a"), 0.2f, 1f, 1f, 0.04f, 0.15f, 0.22f, Hex("#ff7a2a"), Hex("#ffb35a")),
				FloatGradeDefinition.EditorCreate("Dusty", 0.45f, 15f, 3, Hex("#b4a8cc"), 0f, 0.75f, 0.9f, 0.16f, 0.38f, 0.08f, Hex("#b08a70"), Hex("#e0c8a8")),
				FloatGradeDefinition.EditorCreate("Crumbly Old", 1f, 30f, 0, Hex("#9c968c"), 0f, 0.5f, 0.82f, 0.3f, 0.62f, 0f, Hex("#8a8278"), Hex("#c8c0b4")),
			};
			config.EditorSetFloat(180, grades);
		}

		private static void FillAudio(AudioConfig config)
		{
			var sounds = new List<AudioConfig.SoundEntry>
			{
				new(GameSound.ButtonPress, Clip(AudioKenney, "click_001"), 0.7f),
				new(GameSound.TabSwitch, Clip(AudioKenney, "switch_002"), 0.6f),
				new(GameSound.PopupOpen, Clip(AudioKenney, "maximize_006"), 0.7f),
				new(GameSound.PopupClose, Clip(AudioKenney, "minimize_006"), 0.6f),
				new(GameSound.ScanBeep, Clip(AudioGenerated, "scan_beep"), 0.8f),
				new(GameSound.CardAppear, Clip(AudioGenerated, "card_appear"), 0.8f),
				new(GameSound.CardFlip, Clip(AudioKenney, "card-place-1"), 0.9f),
				new(GameSound.CardWhoosh, Clip(AudioGenerated, "whoosh"), 0.7f),
				new(GameSound.SlotFill, Clip(AudioGenerated, "slot_fill"), 0.8f),
				new(GameSound.PointsTick, Clip(AudioGenerated, "points_tick"), 0.35f),
				new(GameSound.Duplicate, Clip(AudioKenney, "lowDown"), 0.8f),
				new(GameSound.Error, Clip(AudioKenney, "error_006"), 0.7f),
				new(GameSound.CompletionFanfare, Clip(AudioGenerated, "fanfare_completion"), 1f),
				new(GameSound.Sparkle, Clip(AudioGenerated, "sparkle"), 0.6f),
				new(GameSound.Rumble, Clip(AudioGenerated, "rumble"), 0.8f),
				new(GameSound.BuildUp, Clip(AudioGenerated, "build_up"), 0.7f),
				new(GameSound.NewBadge, Clip(AudioGenerated, "new_badge"), 0.7f),
				new(GameSound.Firework, Clip(AudioGenerated, "firework"), 0.6f),
				new(GameSound.Toggle, Clip(AudioKenney, "toggle_001"), 0.7f),
				new(GameSound.FloatFire, Clip(AudioGenerated, "float_fire"), 0.75f),
				new(GameSound.FloatJackpot, Clip(AudioGenerated, "float_jackpot"), 0.85f),
			};
			config.EditorSetData(Clip(AudioGenerated, "music_lobby_loop"), 0.45f, sounds);
		}

		private static void FillTheme(UiThemeConfig theme)
		{
			var sprites = SpriteFiles.Select(pair => new UiThemeConfig.SpriteEntry(pair.Key, Sprite(ArtUi, pair.Value))).ToList();
			var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtUi}/particles.png");
			theme.EditorSetData(
				AssetDatabase.LoadAssetAtPath<Font>($"{Fonts}/LuckiestGuy-Regular.ttf"),
				AssetDatabase.LoadAssetAtPath<Font>($"{Fonts}/Fredoka-SemiBold.ttf"),
				atlas, 4, 5, sprites, AssetDatabase.LoadAssetAtPath<Shader>($"{MainRoot}/Shaders/UiWear.shader"));
		}

		// ------------------------------------------------------------------ Prefab + scene

		private static GameObject CreateSessionPrefab()
		{
			var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			if (existing != null)
				return existing;

			Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)!);
			var go = new GameObject("HuntSession");
			var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
			Object.DestroyImmediate(go);
			return prefab;
		}

		private static void CreateScene(Configs configs, GameObject sessionPrefab)
		{
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
			var camera = cameraGo.AddComponent<Camera>();
			cameraGo.AddComponent<AudioListener>();
			camera.orthographic = true;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = configs.Theme.Palette.NightDeep;
			cameraGo.transform.position = new Vector3(0f, 0f, -10f);

			var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

			var scope = new GameObject("Scope");
			var sessionParent = new GameObject("Session").transform;
			sessionParent.SetParent(scope.transform);
			var factory = scope.AddComponent<GameplaySessionObjectFactory>();

			var managerGo = new GameObject("GameManager");
			var manager = managerGo.AddComponent<SpookyGameManager>();

			var uiGo = new GameObject("UiRoot");
			var uiRoot = uiGo.AddComponent<UiRoot>();

			var factorySo = new SerializedObject(factory);
			factorySo.FindProperty("_gameplayObjectParent").objectReferenceValue = sessionParent;
			factorySo.FindProperty("_gameplayObjectPrefab").objectReferenceValue = sessionPrefab;
			factorySo.ApplyModifiedPropertiesWithoutUndo();

			// Add the installer last: LifetimeScope builds in Awake, and every reference must be set before that.
			scope.SetActive(false);
			var installer = scope.AddComponent<SpookySceneInstaller>();
			var installerSo = new SerializedObject(installer);
			installerSo.FindProperty("_gameManager").objectReferenceValue = manager;
			installerSo.FindProperty("_gameplaySessionObjectFactory").objectReferenceValue = factory;
			installerSo.FindProperty("_uiRoot").objectReferenceValue = uiRoot;
			installerSo.FindProperty("_timeConfig").objectReferenceValue = configs.Time;
			installerSo.FindProperty("_scoringConfig").objectReferenceValue = configs.Scoring;
			installerSo.FindProperty("_collectionConfig").objectReferenceValue = configs.Collection;
			installerSo.FindProperty("_audioConfig").objectReferenceValue = configs.Audio;
			installerSo.FindProperty("_uiThemeConfig").objectReferenceValue = configs.Theme;
			installerSo.ApplyModifiedPropertiesWithoutUndo();
			scope.SetActive(true);

			Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
			EditorSceneManager.SaveScene(scene, ScenePath);

			EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
		}

		// ------------------------------------------------------------------ Player settings

		/// <summary>The first version that downloads collections (1.x apps only had the packs built in).</summary>
		private const string FirstDownloadingVersion = "2.0";

		public static void ConfigurePlayer()
		{
			PlayerSettings.productName = ProductName;
			// Company name and Android id stay from the first release, so the update installs over it and keeps saves.
			PlayerSettings.companyName = "Spooky Collector";
			PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.spookycollector.hunt");
			PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
			PlayerSettings.allowedAutorotateToPortrait = true;
			PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
			PlayerSettings.allowedAutorotateToLandscapeLeft = false;
			PlayerSettings.allowedAutorotateToLandscapeRight = false;
			PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
			// Phones check the website for new collections.
			PlayerSettings.Android.forceInternetPermission = true;
			// Player Settings → Version is the app version packs and the website compare against ("2.0", "2.1"…).
			// Raise it whenever the code changes; the Android version code follows it (2.1 → 20100) so updates install.
			if (!AppVersion.TryParse(PlayerSettings.bundleVersion, out var version) || version < new System.Version(FirstDownloadingVersion))
				PlayerSettings.bundleVersion = FirstDownloadingVersion;
			AppVersion.TryParse(PlayerSettings.bundleVersion, out version);
			PlayerSettings.Android.bundleVersionCode = version.Major * 10000 + version.Minor * 100 + Math.Max(0, version.Build);
			PlayerSettings.runInBackground = false;
			PlayerSettings.iOS.cameraUsageDescription = "The camera scans the hidden party QR codes. Nothing is recorded or sent.";

			var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtUi}/app_icon.png");
			if (icon != null)
				PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

			AddAlwaysIncludedShader("TextMeshPro/Distance Field");
			AddAlwaysIncludedShader("TextMeshPro/Mobile/Distance Field");
			AddAlwaysIncludedShader("Spooky/UI/Wear");
		}

		private static void AddAlwaysIncludedShader(string shaderName)
		{
			var shader = Shader.Find(shaderName);
			if (shader == null)
				return;

			var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
			var so = new SerializedObject(graphicsSettings);
			var list = so.FindProperty("m_AlwaysIncludedShaders");
			for (int i = 0; i < list.arraySize; i++)
			{
				if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
					return;
			}

			list.InsertArrayElementAtIndex(list.arraySize);
			list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		// ------------------------------------------------------------------ helpers

		private static Sprite? Sprite(string folder, string name)
		{
			var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{name}.png");
			if (sprite == null)
				Debug.LogWarning($"[SpookyProjectBuilder] Missing sprite {folder}/{name}.png (run Tools/art/build.py).");
			return sprite;
		}

		private static AudioClip? Clip(string folder, string name)
		{
			var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}/{name}.wav") ?? AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}/{name}.ogg");
			if (clip == null)
				Debug.LogWarning($"[SpookyProjectBuilder] Missing audio clip {folder}/{name} (run Tools/audio/synth.py).");
			return clip;
		}

		private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
	}
}
