using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Configs.Collection
{
	/// <summary>What drifts through the sky behind a collection. New styles get [SinceAppVersion("x.y")] (see the attribute).</summary>
	public enum AmbientStyle
	{
		/// <summary>Twinkling sparkles only.</summary>
		Sparkles = 0,
		/// <summary>Rising orange/green embers (Halloween).</summary>
		Embers = 1,
		/// <summary>Slowly falling snowflakes (winter).</summary>
		Snow = 2,
		/// <summary>Floating confetti (birthdays).</summary>
		Confetti = 3,
		/// <summary>Rising hearts.</summary>
		Hearts = 4,
		None = 5
	}

	/// <summary>How a collection dresses the app: colors, logo, cover, backdrop and ambient effects. Every image is optional.</summary>
	public sealed class PackTheme
	{
		public Color Accent { get; }
		public Color Accent2 { get; }
		public Sprite? Logo { get; }
		public Sprite? Cover { get; }
		/// <summary>Full-screen sky (1080x1920); null keeps the app's own night sky.</summary>
		public Sprite? Background { get; }
		/// <summary>Bottom scenery strip (1080x560); null keeps the app's own hills.</summary>
		public Sprite? Scenery { get; }
		public bool Moon { get; }
		public AmbientStyle Ambient { get; }
		public bool Bats { get; }

		public PackTheme(Color accent, Color accent2, Sprite? logo, Sprite? cover, Sprite? background, Sprite? scenery, bool moon, AmbientStyle ambient, bool bats)
		{
			Accent = accent;
			Accent2 = accent2;
			Logo = logo;
			Cover = cover;
			Background = background;
			Scenery = scenery;
			Moon = moon;
			Ambient = ambient;
			Bats = bats;
		}
	}

	/// <summary>A collection's own words. Anything left out falls back to the app's generic texts.</summary>
	public sealed class PackTexts
	{
		public static readonly IReadOnlyList<string> DefaultRanks = new[]
		{
			"NEW PEEKER", "PAL SPOTTER", "SNEAKY SEEKER", "CRITTER CHASER", "PEEK MASTER", "PARTY LEGEND"
		};

		/// <summary>One line under the collection title, e.g. "Spooky friends are hiding around us".</summary>
		public string Tagline { get; }

		/// <summary>What the characters are called in sentences: "You found all 10 {friends}!"</summary>
		public string Friends { get; }

		public string HaulTitle { get; }

		/// <summary>Six ranks by progress: none, &lt;30%, &lt;60%, &lt;90%, almost, complete.</summary>
		public IReadOnlyList<string> Ranks { get; }

		public PackTexts(string? tagline, string? friends, string? haulTitle, IReadOnlyList<string>? ranks)
		{
			Tagline = string.IsNullOrWhiteSpace(tagline) ? "Pals are hiding around us" : tagline!;
			Friends = string.IsNullOrWhiteSpace(friends) ? "pals" : friends!;
			HaulTitle = string.IsNullOrWhiteSpace(haulTitle) ? "MY HAUL" : haulTitle!;
			Ranks = ranks is { Count: 6 } ? ranks : DefaultRanks;
		}

		public string Rank(int index) => Ranks[Math.Clamp(index, 0, Ranks.Count - 1)];
	}
}
