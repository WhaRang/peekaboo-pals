using System;
using System.Text.RegularExpressions;

namespace _Main.Scripts.Services.Qr
{
	public readonly struct QrPayload
	{
		/// <summary>Empty for a legacy code (<see cref="LegacyPrefix"/> names its pack instead).</summary>
		public string PackId { get; }
		public string LegacyPrefix { get; }
		public string CharacterId { get; }
		public string Secret { get; }

		public QrPayload(string packId, string legacyPrefix, string characterId, string secret)
		{
			PackId = packId;
			LegacyPrefix = legacyPrefix;
			CharacterId = characterId;
			Secret = secret;
		}
	}

	/// <summary>
	/// Pure encoder/decoder for the game's QR payloads.
	/// <list type="bullet">
	/// <item>v2 (every new code): a link, <c>https://&lt;site&gt;/?c=packId.characterId.SECRET</c>. Any host is accepted, so
	/// moving the web build never breaks printed codes; scanning one with the phone's own camera opens the web game.</item>
	/// <item>Typed by hand: <c>packId.characterId.SECRET</c>.</item>
	/// <item>v1 (Halloween 2026 tags): <c>PREFIX:characterId:SECRET</c>, mapped to the pack whose legacyPrefix matches.</item>
	/// </list>
	/// Validation against the packs happens in <see cref="QrCodeResolver"/>.
	/// </summary>
	public static class QrPayloadCodec
	{
		public const string LinkParameter = "c";
		private const char LegacySeparator = ':';

		private static readonly Regex Link = new(@"[?&#]c=([A-Za-z0-9\-]+)\.([A-Za-z0-9\-_]+)\.([A-Za-z0-9]+)", RegexOptions.CultureInvariant);
		private static readonly Regex Bare = new(@"^([A-Za-z0-9\-]+)\.([A-Za-z0-9\-_]+)\.([A-Za-z0-9]+)$", RegexOptions.CultureInvariant);

		/// <summary>The link printed into new QR codes. <paramref name="linkBase"/> is the web build's address.</summary>
		public static string EncodeLink(string linkBase, string packId, string characterId, string secret)
		{
			string root = linkBase.Trim();
			if (!root.EndsWith("/") && !root.Contains("?"))
				root += "/";
			return $"{root}{(root.Contains("?") ? "&" : "?")}{LinkParameter}={packId}.{characterId}.{secret}";
		}

		public static string EncodeLegacy(string prefix, string characterId, string secret) => $"{prefix}{LegacySeparator}{characterId}{LegacySeparator}{secret}";

		public static bool TryDecode(string? raw, out QrPayload payload)
		{
			payload = default;
			if (string.IsNullOrWhiteSpace(raw))
				return false;

			string text = raw!.Trim();

			var match = Link.Match(text);
			if (!match.Success)
				match = Bare.Match(text);
			if (match.Success)
			{
				payload = new QrPayload(match.Groups[1].Value, string.Empty, match.Groups[2].Value, match.Groups[3].Value);
				return true;
			}

			if (text.Contains("://"))
				return false;

			var parts = text.Split(LegacySeparator);
			if (parts.Length != 3)
				return false;

			var prefix = parts[0].Trim();
			var id = parts[1].Trim();
			var secret = parts[2].Trim();
			if (prefix.Length == 0 || id.Length == 0 || secret.Length == 0)
				return false;

			payload = new QrPayload(string.Empty, prefix, id, secret);
			return true;
		}

		/// <summary>True when an opened page/app link carries a code (the web build was opened by scanning a tag).</summary>
		public static bool TryDecodeLaunchUrl(string? url, out string code)
		{
			code = string.Empty;
			if (string.IsNullOrEmpty(url) || !Link.IsMatch(url!))
				return false;

			code = url!;
			return true;
		}

		public static bool IsSafeId(string id) => !string.IsNullOrEmpty(id) && Regex.IsMatch(id, @"^[a-z0-9\-]+$") && !id.StartsWith("-", StringComparison.Ordinal);
	}
}
