namespace _Main.Scripts.Configs.Collection
{
	/// <summary>Rarity tiers, ordered from most common to rarest (the order is used for sorting).</summary>
	public enum CharacterRarity
	{
		Common = 0,
		Rare = 1,
		Epic = 2,
		Legendary = 3
	}

	/// <summary>How dramatic the reveal sequence is. Each tier picks one in the config.</summary>
	public enum RevealStyle
	{
		/// <summary>Quick pop + light confetti.</summary>
		Quick = 0,
		/// <summary>Bigger burst, sparkles, short shake.</summary>
		Burst = 1,
		/// <summary>Card flip, screen flash, swirl particles, rumble.</summary>
		Flip = 2,
		/// <summary>Darkened screen, build-up, particle explosion, fireworks, fanfare.</summary>
		Cinematic = 3
	}

	/// <summary>Optional per-character flourish layered on top of the tier's reveal and inspect effects. New ones get [SinceAppVersion("x.y")].</summary>
	public enum CharacterSignature
	{
		None = 0,
		/// <summary>Electric sparks, digital bits, hearts and a typewriter teaser: for the robot.</summary>
		Robot = 1
	}

	public enum HapticStrength
	{
		None = 0,
		Light = 1,
		Medium = 2,
		Heavy = 3,
		Epic = 4
	}
}
