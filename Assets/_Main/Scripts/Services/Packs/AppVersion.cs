using System;
using UnityEngine;

namespace _Main.Scripts.Services.Packs
{
	/// <summary>
	/// The app's own version (Player Settings → Version, e.g. "2.0") compared with what packs and the website ask for.
	/// Anything that doesn't parse counts as "no requirement", so a typo never locks a pack.
	/// </summary>
	public static class AppVersion
	{
		public static string Current => Application.version;

		public static bool TryParse(string? value, out Version version)
		{
			version = null!;
			if (string.IsNullOrWhiteSpace(value))
				return false;

			var text = value!.Trim();
			if (!text.Contains("."))
				text += ".0";
			return Version.TryParse(text, out version!);
		}

		/// <summary>True when this app is at least <paramref name="required"/> (or nothing valid is required).</summary>
		public static bool Satisfies(string? required) => !IsNewer(required, Current);

		/// <summary>True when <paramref name="candidate"/> is a valid version above <paramref name="baseline"/>.</summary>
		public static bool IsNewer(string? candidate, string? baseline)
		{
			if (!TryParse(candidate, out var a))
				return false;
			return !TryParse(baseline, out var b) || a > b;
		}

		public static string Max(string? a, string? b) => IsNewer(b, a) ? b! : a ?? b ?? string.Empty;
	}
}
