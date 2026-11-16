using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Services.Qr;
using UnityEditor;
using UnityEngine;

namespace _Main.Scripts.Editor
{
	/// <summary>
	/// Writes a print-ready QR sheet per collection to &lt;project&gt;/QRSheet/&lt;packId&gt;/index.html: one cut-out tag per
	/// character (no names, so players can't tell which pal hides where), plus a host cheat sheet mapping tag numbers
	/// to characters. Tags carry the link form (site.json "linkBase" + ?c=pack.id.SECRET), so scanning one with the
	/// phone's own camera opens the web game. Open the page in Chrome and print (A4, 100% scale, no margins/headers).
	/// </summary>
	public static class QrSheetExporter
	{
		public static readonly string OutputRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "QRSheet"));

		[MenuItem("Party/Packs/Export QR Sheets", priority = 10)]
		public static void Export()
		{
			// Validates the packs and fills in any missing secrets first.
			if (!PackBuilder.Build(out var report))
			{
				Debug.LogError(report);
				return;
			}

			var site = PackBuilder.LoadSite();
			if (string.IsNullOrWhiteSpace(site.LinkBase))
			{
				Debug.LogError($"[QrSheetExporter] Set \"linkBase\" (the web build's address) in Packs/{PackBuilder.SiteFile} first.");
				return;
			}

			var rules = AssetDatabase.LoadAssetAtPath<CollectionConfig>("Assets/_Main/Configs/CollectionConfig.asset");
			var errors = new List<string>();
			var generator = new QrCodeImageGenerator();
			string? last = null;

			foreach (var source in PackBuilder.LoadSources(errors))
			{
				var dto = source.Dto;
				var root = Path.Combine(OutputRoot, dto.Id!);
				var img = Path.Combine(root, "img");
				if (Directory.Exists(root))
					Directory.Delete(root, true);
				Directory.CreateDirectory(img);

				var characters = dto.Characters!;
				var payloads = new List<string>();
				for (int i = 0; i < characters.Count; i++)
				{
					var payload = QrPayloadCodec.EncodeLink(site.LinkBase!, dto.Id!, characters[i].Id!, source.Codes[characters[i].Id!]);
					payloads.Add(payload);
					var texture = generator.Generate(payload, 16, 4);
					File.WriteAllBytes(Path.Combine(img, $"qr_{i + 1:00}.png"), texture.EncodeToPNG());
					Object.DestroyImmediate(texture);
				}

				var logo = dto.Theme?.Logo;
				CopyIfExists(logo != null ? Path.Combine(source.Folder, logo) : "Assets/_Main/Art/UI/logo.png", Path.Combine(img, "logo.png"));
				CopyIfExists("Assets/_Main/Art/UI/logo.png", Path.Combine(img, "app_logo.png"));
				CopyIfExists("Assets/_Main/Fonts/LuckiestGuy-Regular.ttf", Path.Combine(img, "LuckiestGuy-Regular.ttf"));
				CopyIfExists("Assets/_Main/Fonts/Fredoka-SemiBold.ttf", Path.Combine(img, "Fredoka-SemiBold.ttf"));

				last = Path.Combine(root, "index.html");
				File.WriteAllText(last, BuildHtml(source, payloads, rules), Encoding.UTF8);
				Debug.Log($"[QrSheetExporter] {dto.Id}: {characters.Count} tags → {last}");
			}

			foreach (var error in errors)
				Debug.LogError("[QrSheetExporter] " + error);

			if (!Application.isBatchMode && last != null)
				EditorUtility.RevealInFinder(last);
		}

		private static void CopyIfExists(string from, string to)
		{
			if (File.Exists(from))
				File.Copy(from, to, true);
		}

		private static string Esc(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

		private static string BuildHtml(PackBuilder.SourcePack source, List<string> payloads, CollectionConfig? rules)
		{
			var dto = source.Dto;
			var characters = dto.Characters!;
			string accent = dto.Theme?.Accent ?? "#ffb52e";
			string accent2 = dto.Theme?.Accent2 ?? "#4fe3b0";

			var html = new StringBuilder();
			html.Append($@"<!doctype html><html><head><meta charset=""utf-8""><title>{Esc(dto.Title)} - QR tags</title><style>
@font-face {{ font-family: 'Luckiest'; src: url('img/LuckiestGuy-Regular.ttf'); }}
@font-face {{ font-family: 'Fredoka'; src: url('img/Fredoka-SemiBold.ttf'); }}
@page {{ size: A4; margin: 10mm; }}
* {{ box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
body {{ margin: 0; font-family: 'Fredoka', sans-serif; color: #2a1240; background: #eee; }}
.page {{ width: 190mm; height: 277mm; margin: 10mm auto; background: #fff; display: grid; grid-template-columns: 1fr 1fr; grid-template-rows: repeat(3, minmax(0, 1fr)); gap: 6mm; padding: 4mm; page-break-after: always; }}
.tag {{ overflow: hidden; border: 1.2mm dashed {accent2}; border-radius: 8mm; display: flex; flex-direction: column; align-items: center; justify-content: space-between; padding: 3mm; background: linear-gradient(#2a1650, #3d1d5c); }}
.tag img.logo {{ height: 15mm; max-width: 60mm; object-fit: contain; }}
.tag .qr {{ background: #fff; border-radius: 4mm; padding: 2mm; }}
.tag .qr img {{ width: 46mm; height: 46mm; image-rendering: pixelated; display: block; }}
.tag .cta {{ font-family: 'Luckiest'; font-size: 6mm; color: {accent}; letter-spacing: .5mm; }}
.tag .num {{ font-family: 'Luckiest'; font-size: 4.2mm; color: #c9a2ff; }}
.host {{ page-break-after: always; width: 190mm; margin: 10mm auto; background: #fff; padding: 10mm; }}
.host h1 {{ font-family: 'Luckiest'; color: #e8650e; margin: 0 0 4mm; }}
.host table {{ width: 100%; border-collapse: collapse; font-size: 4mm; }}
.host td, .host th {{ border-bottom: .3mm solid #ddd; padding: 2mm; text-align: left; }}
.host code {{ font-size: 2.8mm; word-break: break-all; }}
@media print {{ body {{ background: #fff; }} .page, .host {{ margin: 0; }} }}
</style></head><body>");

			for (int page = 0; page * 6 < characters.Count; page++)
			{
				html.Append("<div class=\"page\">");
				for (int i = page * 6; i < Mathf.Min(characters.Count, page * 6 + 6); i++)
				{
					html.Append($@"<div class=""tag""><img class=""logo"" src=""img/logo.png""><div class=""qr""><img src=""img/qr_{i + 1:00}.png""></div>
<div class=""cta"">SCAN ME IF YOU CAN!</div><div class=""num"">TAG #{i + 1:00}</div></div>");
				}
				html.Append("</div>");
			}

			html.Append($"<div class=\"host\"><h1>{Esc(dto.Title)}: host cheat sheet</h1><p>Hide one tag per spot. Keep this page to yourself!</p><table><tr><th>Tag</th><th>Character</th><th>Rarity</th><th>Points</th><th>Code</th></tr>");
			for (int i = 0; i < characters.Count; i++)
			{
				var c = characters[i];
				string rarity = c.Rarity ?? "common";
				string points = rules != null && System.Enum.TryParse<CharacterRarity>(rarity, true, out var r) ? rules.GetTier(r).Points.ToString() : "?";
				html.Append($"<tr><td>#{i + 1:00}</td><td>{Esc(c.Name)}</td><td>{Esc(rarity)}</td><td>{points}</td><td><code>{Esc(payloads[i])}</code></td></tr>");
			}

			int bonus = rules != null ? rules.CompletionBonusPoints : 0;
			html.Append($"</table><p>Collecting everything adds a +{bonus} bonus. Tip: hide the Legendary somewhere sneaky (inside a cupboard door, under a chair seat).</p>");
			if (!string.IsNullOrEmpty(dto.LegacyPrefix))
				html.Append($"<p>Older tags printed as <code>{Esc(dto.LegacyPrefix)}:id:SECRET</code> keep working.</p>");
			html.Append("</div></body></html>");
			return html.ToString();
		}
	}
}
