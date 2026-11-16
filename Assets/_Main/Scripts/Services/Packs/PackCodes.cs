using System;
using System.Security.Cryptography;
using System.Text;

namespace _Main.Scripts.Services.Packs
{
	/// <summary>
	/// QR secrets never ship: the pack files only carry a hash of (pack, character, secret), so reading the public
	/// pack.json on the web host doesn't reveal the codes. The secrets live in Packs/&lt;id&gt;/codes.json on the
	/// host's computer (keep it backed up: printed codes depend on it).
	/// </summary>
	public static class PackCodes
	{
		public const int HashLength = 20;

		public static string Hash(string packId, string characterId, string secret)
		{
			string material = $"peekaboo|{packId.Trim().ToLowerInvariant()}|{characterId.Trim().ToLowerInvariant()}|{secret.Trim().ToUpperInvariant()}";
			using var sha = SHA256.Create();
			var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(material));
			var hex = new StringBuilder(HashLength);
			for (int i = 0; i < HashLength / 2; i++)
				hex.Append(digest[i].ToString("x2"));
			return hex.ToString();
		}

		public static bool Matches(string packId, string characterId, string secret, string expectedHash)
			=> !string.IsNullOrEmpty(expectedHash) && string.Equals(Hash(packId, characterId, secret), expectedHash, StringComparison.OrdinalIgnoreCase);
	}
}
