using System;
using System.Collections.Generic;
using System.IO;
using _Main.Scripts.Services.Qr;
using Newtonsoft.Json;
using UnityEngine;

namespace _Main.Scripts.Services.Packs
{
	/// <summary>
	/// Editor/debug-only access to the real QR secrets (Packs/&lt;id&gt;/codes.json on the host's computer), so the debug
	/// "simulate a scan" buttons and the autopilot can fire real codes. Release builds never carry secrets: there this
	/// always comes back empty. Outside the editor, point PEEKABOO_PACKS_SOURCE at the Packs folder.
	/// </summary>
	public static class DevCodeBook
	{
		public const string LinkBaseForTests = "https://peekaboo.test/";

		private static readonly Dictionary<string, Dictionary<string, string>?> Cache = new(StringComparer.OrdinalIgnoreCase);

		public static bool TryGetPayload(string packId, string characterId, out string payload)
		{
			payload = string.Empty;
			if (!TryGetSecret(packId, characterId, out var secret))
				return false;

			payload = QrPayloadCodec.EncodeLink(LinkBaseForTests, packId, characterId, secret);
			return true;
		}

		public static bool TryGetSecret(string packId, string characterId, out string secret)
		{
			secret = string.Empty;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || SPOOKY_AUTOPILOT
			var codes = Load(packId);
			return codes != null && codes.TryGetValue(characterId, out secret!);
#else
			return false;
#endif
		}

#if UNITY_EDITOR || DEVELOPMENT_BUILD || SPOOKY_AUTOPILOT
		private static Dictionary<string, string>? Load(string packId)
		{
			if (Cache.TryGetValue(packId, out var cached))
				return cached;

			Dictionary<string, string>? codes = null;
			foreach (var root in Roots())
			{
				var path = Path.Combine(root, packId, "codes.json");
				if (!File.Exists(path))
					continue;

				try
				{
					codes = new Dictionary<string, string>(JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path))!, StringComparer.OrdinalIgnoreCase);
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[{nameof(DevCodeBook)}] {path}: {e.Message}");
				}

				break;
			}

			Cache[packId] = codes;
			return codes;
		}

		private static IEnumerable<string> Roots()
		{
			var fromEnvironment = Environment.GetEnvironmentVariable("PEEKABOO_PACKS_SOURCE");
			if (!string.IsNullOrEmpty(fromEnvironment))
				yield return fromEnvironment;
			yield return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packs"));
		}
#endif
	}
}
