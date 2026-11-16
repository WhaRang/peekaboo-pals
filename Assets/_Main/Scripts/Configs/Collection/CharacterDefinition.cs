using UnityEngine;

namespace _Main.Scripts.Configs.Collection
{
	/// <summary>
	/// One collectible character of a collection pack, built at runtime from the pack's <c>pack.json</c> and media
	/// (see PACKS.md). Exactly one QR code (pack + id + secret) unlocks it; only a hash of the secret ships.
	/// </summary>
	public sealed class CharacterDefinition
	{
		/// <summary>Stable id inside its pack. Changing it invalidates printed codes and saved progress for this character.</summary>
		public string Id { get; }
		public string PackId { get; }
		public string DisplayName { get; }
		public CharacterRarity Rarity { get; }
		public int Power { get; }
		public string Description { get; }
		public Color ThemeColor { get; }
		public Sprite? Art { get; }

		/// <summary>White silhouette (tinted dark by the views) shown while the character is still locked.</summary>
		public Sprite? Silhouette { get; }
		public AudioClip? Voice { get; }

		/// <summary>Optional flourish layered on top of the tier's reveal and inspect effects.</summary>
		public CharacterSignature Signature { get; }

		/// <summary>Replaces the tier's teaser line during a cinematic reveal. Empty = the tier default.</summary>
		public string RevealTeaser { get; }

		/// <summary>Hash of the QR secret (<see cref="_Main.Scripts.Services.Packs.PackCodes"/>).</summary>
		public string CodeHash { get; }

		public CharacterDefinition(string packId, string id, string displayName, CharacterRarity rarity, int power, string description,
			Color themeColor, Sprite? art, Sprite? silhouette, AudioClip? voice, CharacterSignature signature, string revealTeaser, string codeHash)
		{
			PackId = packId;
			Id = id;
			DisplayName = displayName;
			Rarity = rarity;
			Power = power;
			Description = description;
			ThemeColor = themeColor;
			Art = art;
			Silhouette = silhouette;
			Voice = voice;
			Signature = signature;
			RevealTeaser = revealTeaser;
			CodeHash = codeHash;
		}
	}
}
