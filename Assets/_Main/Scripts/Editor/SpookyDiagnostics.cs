using System.IO;
using System.Linq;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Services.Packs.Dto;
using _Main.Scripts.Services.Qr;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace _Main.Scripts.Editor
{
	/// <summary>
	/// Sanity checks for the shipped packs: every character's QR code (link form and, for packs with a legacy prefix,
	/// the old form) must decode back to exactly its own pack + character and match the shipped hash; forged and foreign
	/// codes must be rejected.
	/// </summary>
	public static class SpookyDiagnostics
	{
		[MenuItem("Party/Packs/Verify QR Codes", priority = 30)]
		public static bool VerifyQrCodes()
		{
			if (!PackBuilder.Build(out var report))
			{
				Debug.LogError(report);
				return false;
			}

			var site = PackBuilder.LoadSite();
			var generator = new QrCodeImageGenerator();
			var reader = QrCameraScanner.CreateReader();
			byte[]? buffer = null;
			int failures = 0, checks = 0;

			foreach (var source in PackBuilder.LoadSources(new System.Collections.Generic.List<string>()))
			{
				var shippedPath = Path.Combine(PackBuilder.OutputRoot, source.Dto.Id!, PackLibrary.PackFile);
				var shipped = JsonConvert.DeserializeObject<PackDto>(File.ReadAllText(shippedPath))!;

				foreach (var character in shipped.Characters!)
				{
					string secret = source.Codes[character.Id!];
					var forms = new[] { QrPayloadCodec.EncodeLink(site.LinkBase ?? DevCodeBook.LinkBaseForTests, shipped.Id!, character.Id!, secret) }.ToList();
					if (!string.IsNullOrEmpty(shipped.LegacyPrefix))
						forms.Add(QrPayloadCodec.EncodeLegacy(shipped.LegacyPrefix!, character.Id!, secret));

					foreach (var payload in forms)
					{
						checks++;
						var texture = generator.Generate(payload, 8, 4);
						// Same pixel order a WebCamTexture hands to the scanner.
						bool decoded = QrCameraScanner.TryDecode(reader, texture.GetPixels32(), texture.width, texture.height, ref buffer, out var text);
						Object.DestroyImmediate(texture);

						bool parsed = QrPayloadCodec.TryDecode(text, out var code);
						string packId = code.PackId.Length > 0 ? code.PackId : code.LegacyPrefix == shipped.LegacyPrefix ? shipped.Id! : "?";
						bool ok = decoded && text == payload && parsed && packId == shipped.Id && code.CharacterId == character.Id &&
							PackCodes.Matches(shipped.Id!, character.Id!, code.Secret, character.Code!);
						if (!ok)
							failures++;
						Debug.Log($"[SpookyDiagnostics] {(ok ? "OK  " : "FAIL")} {shipped.Id}/{character.Id,-12} '{payload}'");
					}
				}

				var first = shipped.Characters!.First();
				bool forgedRejected = !PackCodes.Matches(shipped.Id!, first.Id!, "WRONG1", first.Code!);
				bool foreignRejected = !QrPayloadCodec.TryDecode("https://example.com", out _);
				checks += 2;
				if (!forgedRejected || !foreignRejected)
					failures++;
				Debug.Log($"[SpookyDiagnostics] {shipped.Id}: forged secret rejected: {forgedRejected}, foreign code rejected: {foreignRejected}");
			}

			Debug.Log(failures == 0 ? $"[SpookyDiagnostics] All {checks} QR checks passed." : $"[SpookyDiagnostics] {failures} of {checks} QR checks FAILED.");
			return failures == 0;
		}

		/// <summary>Batch: build packs, verify every code, export the printable sheets.</summary>
		public static void BatchVerifyAndExport()
		{
			bool ok = VerifyQrCodes();
			QrSheetExporter.Export();
			EditorApplication.Exit(ok ? 0 : 1);
		}
	}
}
