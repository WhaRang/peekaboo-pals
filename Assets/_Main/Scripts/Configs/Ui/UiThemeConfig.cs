using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Configs.Ui
{
	public enum UiSprite
	{
		RoundedRect = 0,
		RoundedRectSmall = 1,
		Pill = 2,
		Circle = 3,
		SoftGlow = 4,
		CardFrame = 5,
		CardInner = 6,
		CardBack = 7,
		Shine = 8,
		Rays = 9,
		Background = 10,
		Moon = 11,
		Hills = 12,
		Vignette = 13,
		GradientVertical = 14,
		IconScan = 20,
		IconCollection = 21,
		IconResults = 22,
		IconSoundOn = 23,
		IconSoundOff = 24,
		IconClose = 25,
		IconCandy = 26,
		IconStar = 27,
		IconClock = 28,
		IconPal = 29,
		IconCamera = 30,
		IconKeyboard = 31,
		IconPower = 32,
		IconTrophy = 33,
		Logo = 40,
		ScanCorner = 41,
		Banner = 42,
		Badge = 43,
		IconEdit = 44,
		IconDice = 45,
		CardFrameMetal = 46,
		Scratches = 47,
		ChipMetal = 48,
		IconInfo = 49,
		IconAlbums = 50
	}

	/// <summary>Cell index inside the particle atlas (row-major).</summary>
	public enum ParticleShape
	{
		Star = 0,
		Sparkle = 1,
		Confetti = 2,
		SoftDot = 3,
		Ember = 4,
		Fog = 5,
		Bat = 6,
		Candy = 7,
		Pumpkin = 8,
		Ring = 9,
		Streak = 10,
		Square = 11,
		Heart = 12,
		Ghost = 13,
		Swirl = 14,
		Circle = 15,
		Flame = 16,
		Smoke = 17,
		FlameTongue = 18,
		Spark = 19
	}

	public interface IUiThemeConfig
	{
		Font? TitleFont { get; }
		Font? BodyFont { get; }
		Texture2D? ParticleAtlas { get; }
		int ParticleAtlasColumns { get; }
		int ParticleAtlasRows { get; }
		Sprite? GetSprite(UiSprite sprite);

		/// <summary>"Spooky/UI/Wear": saturation/contrast/wash for float-graded card art.</summary>
		Shader? WearShader { get; }
		UiPalette Palette { get; }
	}

	[Serializable]
	public sealed class UiPalette
	{
		public Color NightDeep = new(0.06f, 0.03f, 0.14f);
		public Color Night = new(0.15f, 0.07f, 0.3f);
		public Color Purple = new(0.38f, 0.17f, 0.66f);
		public Color PurpleLight = new(0.6f, 0.4f, 0.95f);
		public Color Pumpkin = new(1f, 0.54f, 0.12f);
		public Color Slime = new(0.49f, 1f, 0.31f);
		public Color Candy = new(1f, 0.37f, 0.66f);
		public Color Gold = new(1f, 0.82f, 0.25f);
		public Color Ink = new(0.1f, 0.04f, 0.18f);
		public Color Cream = new(1f, 0.96f, 0.88f);
		public Color Muted = new(0.66f, 0.58f, 0.82f);
		public Color Danger = new(1f, 0.32f, 0.36f);
	}

	[CreateAssetMenu(fileName = "UiThemeConfig", menuName = "Configs/UiThemeConfig")]
	public sealed class UiThemeConfig : ScriptableObject, IUiThemeConfig
	{
		[Header("Fonts")]
		[SerializeField] private Font? _titleFont;
		[SerializeField] private Font? _bodyFont;

		[Header("Particles")]
		[SerializeField] private Texture2D? _particleAtlas;
		[SerializeField, Min(1)] private int _particleAtlasColumns = 4;
		[SerializeField, Min(1)] private int _particleAtlasRows = 4;

		[Header("Shaders")]
		[SerializeField] private Shader? _wearShader;

		[Header("Sprites")]
		[SerializeField] private List<SpriteEntry> _sprites = new();

		[Header("Colors")]
		[SerializeField] private UiPalette _palette = new();

		public Font? TitleFont => _titleFont;
		public Font? BodyFont => _bodyFont;
		public Texture2D? ParticleAtlas => _particleAtlas;
		public int ParticleAtlasColumns => _particleAtlasColumns;
		public int ParticleAtlasRows => _particleAtlasRows;
		public UiPalette Palette => _palette;
		public Shader? WearShader => _wearShader;

		[NonSerialized] private Dictionary<UiSprite, Sprite>? _lookup;

		public Sprite? GetSprite(UiSprite sprite)
		{
			if (_lookup == null)
			{
				_lookup = new Dictionary<UiSprite, Sprite>();
				foreach (var entry in _sprites)
				{
					if (entry.Sprite != null)
						_lookup[entry.Key] = entry.Sprite;
				}
			}

			return _lookup.TryGetValue(sprite, out var result) ? result : null;
		}

		[Serializable]
		public sealed class SpriteEntry
		{
			[SerializeField] private UiSprite _key;
			[SerializeField] private Sprite? _sprite;

			public UiSprite Key => _key;
			public Sprite? Sprite => _sprite;

			public SpriteEntry(UiSprite key, Sprite? sprite)
			{
				_key = key;
				_sprite = sprite;
			}
		}

#if UNITY_EDITOR
		public void EditorSetData(Font? titleFont, Font? bodyFont, Texture2D? particleAtlas, int columns, int rows, List<SpriteEntry> sprites, Shader? wearShader)
		{
			_wearShader = wearShader;
			_titleFont = titleFont;
			_bodyFont = bodyFont;
			_particleAtlas = particleAtlas;
			_particleAtlasColumns = columns;
			_particleAtlasRows = rows;
			_sprites = sprites;
			_lookup = null;
		}
#endif
	}
}
