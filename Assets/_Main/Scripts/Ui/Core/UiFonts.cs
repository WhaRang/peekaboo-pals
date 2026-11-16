using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace _Main.Scripts.Ui.Core
{
	public enum TextStyle
	{
		/// <summary>Chunky display font, thick ink outline + drop shadow. Titles, numbers, banners.</summary>
		Title = 0,
		/// <summary>Display font with a thin outline, for smaller headings.</summary>
		TitleThin = 1,
		/// <summary>Rounded body font, no effects.</summary>
		Body = 2,
		/// <summary>Body font with a thin outline, readable on busy backgrounds.</summary>
		BodyOutline = 3
	}

	/// <summary>
	/// Builds dynamic TextMeshPro font assets from the bundled TTFs at startup, plus one shared material per
	/// <see cref="TextStyle"/> so every label of a style batches together.
	/// </summary>
	public sealed class UiFonts
	{
		private readonly TMP_FontAsset? _title;
		private readonly TMP_FontAsset? _body;
		private readonly Material?[] _materials = new Material?[4];

		public UiFonts(Font? titleFont, Font? bodyFont, Color ink)
		{
			_title = Create(titleFont);
			_body = Create(bodyFont) ?? _title;
			_title ??= _body;

			if (_title != null)
			{
				_materials[(int)TextStyle.Title] = CreateMaterial(_title, ink, 0.28f, true, "Title");
				_materials[(int)TextStyle.TitleThin] = CreateMaterial(_title, ink, 0.16f, false, "TitleThin");
			}

			if (_body != null)
			{
				_materials[(int)TextStyle.Body] = _body.material;
				_materials[(int)TextStyle.BodyOutline] = CreateMaterial(_body, ink, 0.18f, false, "BodyOutline");
			}
		}

		public TMP_FontAsset? GetFont(TextStyle style) => style is TextStyle.Title or TextStyle.TitleThin ? _title : _body;
		public Material? GetMaterial(TextStyle style) => _materials[(int)style];

		private static TMP_FontAsset? Create(Font? font)
		{
			if (font == null)
				return null;

			var asset = TMP_FontAsset.CreateFontAsset(font, 72, 10, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
			if (asset == null)
			{
				Debug.LogError($"[{nameof(UiFonts)}] Could not create a font asset from '{font.name}'.");
				return null;
			}

			asset.name = font.name + " (Runtime SDF)";
			// Warm the atlas with everything the UI prints so the first reveal doesn't rasterize glyphs mid-animation.
			asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !?.,:;'\"+-/%()&#×·—–…", out _);
			return asset;
		}

		private static Material CreateMaterial(TMP_FontAsset asset, Color ink, float outline, bool underlay, string suffix)
		{
			var material = new Material(asset.material) { name = $"{asset.name} {suffix}" };
			material.EnableKeyword(ShaderUtilities.Keyword_Outline);
			material.SetFloat(ShaderUtilities.ID_OutlineWidth, outline);
			material.SetColor(ShaderUtilities.ID_OutlineColor, ink);
			material.SetFloat(ShaderUtilities.ID_FaceDilate, outline * 0.6f);

			if (underlay)
			{
				material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
				material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(ink.r, ink.g, ink.b, 0.85f));
				material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.15f);
				material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.55f);
				material.SetFloat(ShaderUtilities.ID_UnderlayDilate, outline * 1.6f);
				material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.05f);
			}

			ShaderUtilities.UpdateShaderRatios(material);
			return material;
		}
	}
}
