using System;
using UnityEngine;

namespace _Main.Scripts.Configs.Collection
{
	/// <summary>Everything that makes a rarity tier look, sound and score differently.</summary>
	[Serializable]
	public sealed class RarityTierDefinition
	{
		[SerializeField] private CharacterRarity _rarity;

		[Header("Naming")]
		[Tooltip("Default themed tier name shown on cards, e.g. 'Cuddly'. Packs can rename it.")]
		[SerializeField] private string _tierName = "Spooky";
		[Tooltip("Plain rarity label, e.g. 'Common'.")]
		[SerializeField] private string _rarityLabel = "Common";

		[Header("Scoring")]
		[SerializeField, Min(0)] private int _points = 10;
		[Tooltip("Expected power range, used by the config validator only.")]
		[SerializeField] private Vector2Int _powerRange = new(10, 30);

		[Header("Look")]
		[SerializeField] private Color _primaryColor = new(1f, 0.55f, 0.1f);
		[SerializeField] private Color _secondaryColor = new(0.45f, 0.85f, 0.2f);
		[SerializeField] private Color _glowColor = new(1f, 0.7f, 0.2f);
		[Tooltip("Animated rainbow shimmer over the frame (legendary).")]
		[SerializeField] private bool _rainbowShimmer;
		[Tooltip("Number of stars/gems shown on the card.")]
		[SerializeField, Range(1, 5)] private int _stars = 1;

		[Header("Reveal")]
		[SerializeField] private RevealStyle _revealStyle = RevealStyle.Quick;
		[Tooltip("Length of the build-up before the character pops out, in seconds.")]
		[SerializeField, Min(0f)] private float _buildUpSeconds = 0.25f;
		[Tooltip("Particles emitted by the main reveal burst.")]
		[SerializeField, Min(0)] private int _burstParticles = 40;
		[SerializeField, Min(0f)] private float _screenShake;
		[SerializeField] private HapticStrength _revealHaptic = HapticStrength.Light;
		[SerializeField] private AudioClip? _revealStinger;

		public CharacterRarity Rarity => _rarity;
		public string TierName => _tierName;
		public string RarityLabel => _rarityLabel;
		public int Points => _points;
		public Vector2Int PowerRange => _powerRange;
		public Color PrimaryColor => _primaryColor;
		public Color SecondaryColor => _secondaryColor;
		public Color GlowColor => _glowColor;
		public bool RainbowShimmer => _rainbowShimmer;
		public int Stars => _stars;
		public RevealStyle RevealStyle => _revealStyle;
		public float BuildUpSeconds => _buildUpSeconds;
		public int BurstParticles => _burstParticles;
		public float ScreenShake => _screenShake;
		public HapticStrength RevealHaptic => _revealHaptic;
		public AudioClip? RevealStinger => _revealStinger;

		/// <summary>A copy with a pack's own tier name and colors; null/empty values keep the defaults.</summary>
		public RarityTierDefinition WithTheme(string? tierName, Color? primary, Color? secondary, Color? glow)
		{
			var copy = (RarityTierDefinition)MemberwiseClone();
			if (!string.IsNullOrWhiteSpace(tierName))
				copy._tierName = tierName!.Trim();
			if (primary.HasValue)
				copy._primaryColor = primary.Value;
			if (secondary.HasValue)
				copy._secondaryColor = secondary.Value;
			if (glow.HasValue)
				copy._glowColor = glow.Value;
			return copy;
		}

#if UNITY_EDITOR
		public static RarityTierDefinition EditorCreate(
			CharacterRarity rarity, string tierName, string rarityLabel, int points, Vector2Int powerRange,
			Color primary, Color secondary, Color glow, bool rainbow, int stars,
			RevealStyle revealStyle, float buildUpSeconds, int burstParticles, float screenShake,
			HapticStrength haptic, AudioClip? stinger)
		{
			return new RarityTierDefinition
			{
				_rarity = rarity, _tierName = tierName, _rarityLabel = rarityLabel, _points = points,
				_powerRange = powerRange, _primaryColor = primary, _secondaryColor = secondary, _glowColor = glow,
				_rainbowShimmer = rainbow, _stars = stars, _revealStyle = revealStyle, _buildUpSeconds = buildUpSeconds,
				_burstParticles = burstParticles, _screenShake = screenShake, _revealHaptic = haptic, _revealStinger = stinger
			};
		}
#endif
	}
}
