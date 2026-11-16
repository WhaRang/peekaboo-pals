using _Main.Scripts.Configs.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Core
{
	/// <summary>
	/// Code-first uGUI construction helpers. All sizes are in reference units of the 1080x1920 portrait canvas.
	/// Keeping the UI in code (instead of hand-made prefabs) keeps every screen data-driven from the configs.
	/// </summary>
	public static class UiBuilder
	{
		public static RectTransform Rect(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.layer = 5; // UI
			var rect = (RectTransform)go.transform;
			rect.SetParent(parent, false);
			return rect;
		}

		public static RectTransform Stretch(this RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = new Vector2(left, bottom);
			rect.offsetMax = new Vector2(-right, -top);
			return rect;
		}

		/// <summary>Anchors the rect to a single point of its parent (0..1 coordinates) with a fixed size.</summary>
		public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position = default, Vector2? pivot = null)
		{
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
			rect.sizeDelta = size;
			rect.anchoredPosition = position;
			return rect;
		}

		/// <summary>Stretches horizontally between the parent's edges, fixed height, anchored at <paramref name="anchorY"/>.</summary>
		public static RectTransform StretchX(this RectTransform rect, float anchorY, float height, float y = 0f, float inset = 0f, float pivotY = 0.5f)
		{
			rect.anchorMin = new Vector2(0f, anchorY);
			rect.anchorMax = new Vector2(1f, anchorY);
			rect.pivot = new Vector2(0.5f, pivotY);
			rect.sizeDelta = new Vector2(-inset * 2f, height);
			rect.anchoredPosition = new Vector2(0f, y);
			return rect;
		}

		public static Image Image(Transform parent, string name, Sprite? sprite, Color color, bool raycast = false)
		{
			var rect = Rect(name, parent);
			var image = rect.gameObject.AddComponent<Image>();
			image.sprite = sprite;
			image.color = color;
			image.raycastTarget = raycast;
			if (sprite != null && sprite.border != Vector4.zero)
				image.type = UnityEngine.UI.Image.Type.Sliced;
			return image;
		}

		public static Image Image(Transform parent, string name, UiContext ctx, UiSprite sprite, Color color, bool raycast = false)
			=> Image(parent, name, ctx.Sprite(sprite), color, raycast);

		public static TextMeshProUGUI Text(Transform parent, string name, UiContext ctx, string text, float size, TextStyle style, Color color,
			TextAlignmentOptions alignment = TextAlignmentOptions.Center)
		{
			var rect = Rect(name, parent);
			var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
			var font = ctx.Fonts.GetFont(style);
			if (font != null)
				label.font = font;
			var material = ctx.Fonts.GetMaterial(style);
			if (material != null)
				label.fontSharedMaterial = material;

			label.text = text;
			label.fontSize = size;
			label.color = color;
			label.alignment = alignment;
			label.raycastTarget = false;
			label.textWrappingMode = TextWrappingModes.Normal;
			label.overflowMode = TextOverflowModes.Overflow;
			label.extraPadding = true;
			// The title font's space is narrow (0.2 em) and its thick outline closes it up ("PUMPKINIS"): widen it.
			if (style is TextStyle.Title or TextStyle.TitleThin)
				label.wordSpacing = 28f;
			return label;
		}

		/// <summary>Shrinks the font to fit its rect, never below <paramref name="minSize"/>.</summary>
		public static TextMeshProUGUI AutoSize(this TextMeshProUGUI label, float minSize)
		{
			label.enableAutoSizing = true;
			label.fontSizeMax = label.fontSize;
			label.fontSizeMin = minSize;
			return label;
		}

		public static RectTransform Root(this Component component) => (RectTransform)component.transform;

		public static CanvasGroup Group(this Component component)
		{
			var group = component.GetComponent<CanvasGroup>();
			return group != null ? group : component.gameObject.AddComponent<CanvasGroup>();
		}

		public static Color WithAlpha(this Color color, float alpha)
		{
			color.a = alpha;
			return color;
		}

		public static Color Darken(this Color color, float amount)
			=> Color.Lerp(color, Color.black, amount).WithAlpha(color.a);

		public static Color Lighten(this Color color, float amount)
			=> Color.Lerp(color, Color.white, amount).WithAlpha(color.a);

		public static void SetLayerRecursively(this GameObject go, int layer)
		{
			go.layer = layer;
			foreach (Transform child in go.transform)
				child.gameObject.SetLayerRecursively(layer);
		}
	}
}
