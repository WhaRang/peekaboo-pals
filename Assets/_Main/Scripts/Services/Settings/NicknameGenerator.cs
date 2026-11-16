using UnityEngine;

namespace _Main.Scripts.Services.Settings
{
	/// <summary>Cute "Adjective Noun" party nicknames, e.g. "Sneaky Gummy".</summary>
	public static class NicknameGenerator
	{
		public const int MaxLength = 16;

		private static readonly string[] Adjectives =
		{
			"Sneaky", "Giggly", "Wobbly", "Sleepy", "Fuzzy", "Snazzy", "Moonlit", "Midnight", "Peppy", "Cheeky",
			"Jolly", "Sparkly", "Grumpy", "Bouncy", "Misty", "Cosmic", "Tiny", "Mighty", "Wiggly", "Fancy"
		};

		private static readonly string[] Nouns =
		{
			"Goblin", "Gummy", "Muffin", "Yeti", "Noodle", "Bat", "Pickle", "Dragon", "Panda", "Sprite",
			"Imp", "Phantom", "Waffle", "Penguin", "Taco", "Blob", "Owl", "Cat", "Wisp", "Troll"
		};

		public static string Generate()
		{
			for (int attempt = 0; attempt < 10; attempt++)
			{
				string name = $"{Adjectives[Random.Range(0, Adjectives.Length)]} {Nouns[Random.Range(0, Nouns.Length)]}";
				if (name.Length <= MaxLength)
					return name;
			}

			return "Party Hunter";
		}

		/// <summary>Trims, removes line breaks and caps the length; empty input yields a fresh random name.</summary>
		public static string Sanitize(string? raw)
		{
			var clean = (raw ?? string.Empty).Replace("\n", " ").Replace("\r", " ").Replace("<", "").Replace(">", "").Trim();
			if (clean.Length > MaxLength)
				clean = clean.Substring(0, MaxLength).Trim();
			return clean.Length == 0 ? Generate() : clean;
		}
	}
}
