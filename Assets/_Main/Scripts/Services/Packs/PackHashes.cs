using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace _Main.Scripts.Services.Packs
{
	/// <summary>Content hashes of pack files: the builder writes them into manifest.json, phones verify downloads with them.</summary>
	public static class PackHashes
	{
		private const int Length = 32;

		public static string OfBytes(byte[] bytes)
		{
			using var sha = SHA256.Create();
			return ToHex(sha.ComputeHash(bytes));
		}

		/// <summary>One hash for the whole pack: changes whenever any file is added, removed or edited.</summary>
		public static string OfPack(IEnumerable<(string Path, string Hash)> files)
		{
			var text = new StringBuilder();
			foreach (var (path, hash) in files.OrderBy(f => f.Path, System.StringComparer.Ordinal))
				text.Append(path).Append('\n').Append(hash).Append('\n');
			return OfBytes(Encoding.UTF8.GetBytes(text.ToString()));
		}

		private static string ToHex(byte[] digest)
		{
			var hex = new StringBuilder(Length);
			for (int i = 0; i < Length / 2; i++)
				hex.Append(digest[i].ToString("x2"));
			return hex.ToString();
		}
	}
}
