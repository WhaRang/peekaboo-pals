using _Main.Scripts._ProjectAgnostic.Services.Input;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Services.Audio;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Services.Haptics;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Services.Permissions;
using _Main.Scripts.Services.Qr;
using _Main.Scripts.Services.Settings;
using UnityEngine;

namespace _Main.Scripts.Ui.Core
{
	/// <summary>Everything a view needs, handed down by <see cref="UiRoot"/> instead of every view resolving services itself.</summary>
	public sealed class UiContext
	{
		public IUiThemeConfig Theme { get; }
		public UiPalette Palette => Theme.Palette;
		public IPackLibrary Library { get; }

		/// <summary>The collection on screen (lobby, results, host tools). Switched from the album shelf or by a scan.</summary>
		public CollectionPack Pack { get; set; } = null!;
		public IAudioService Audio { get; }
		public IHapticsService Haptics { get; }
		public ISettingsService Settings { get; }
		public IQrCodeImageGenerator QrGenerator { get; }
		public IQrCameraScanner Scanner { get; }
		public ICameraPermissionService CameraPermission { get; }
		public IGameplayInputService Input { get; }
		public UiFonts Fonts { get; }
		public MonoBehaviour CoroutineHost { get; }

		public UiParticleGraphic FxParticles { get; set; } = null!;
		public ScreenEffects ScreenFx { get; set; } = null!;
		public RectTransform FxLayer { get; set; } = null!;
		public RectTransform PopupLayer { get; set; } = null!;
		public Canvas Canvas { get; set; } = null!;

		/// <summary>The latest state of every collection received from the ECS.</summary>
		public AlbumSnapshot Album { get; set; } = AlbumSnapshot.Empty;

		/// <summary>The latest state of the collection on screen.</summary>
		public CollectionSnapshot Snapshot => Album.Get(Pack.Id, Pack.Characters.Count);

		public bool ReducedMotion => Settings.IsReducedMotion;

		public UiContext(
			IUiThemeConfig theme,
			IPackLibrary library,
			IAudioService audio,
			IHapticsService haptics,
			ISettingsService settings,
			IQrCodeImageGenerator qrGenerator,
			IQrCameraScanner scanner,
			ICameraPermissionService cameraPermission,
			IGameplayInputService input,
			MonoBehaviour coroutineHost)
		{
			Theme = theme;
			Library = library;
			Audio = audio;
			Haptics = haptics;
			Settings = settings;
			QrGenerator = qrGenerator;
			Scanner = scanner;
			CameraPermission = cameraPermission;
			Input = input;
			CoroutineHost = coroutineHost;
			Fonts = new UiFonts(theme.TitleFont, theme.BodyFont, theme.Palette.Ink);
		}

		public Sprite? Sprite(UiSprite sprite) => Theme.GetSprite(sprite);

		public RarityTierDefinition Tier(CharacterDefinition character) => Pack.GetTier(character.Rarity);
	}
}
