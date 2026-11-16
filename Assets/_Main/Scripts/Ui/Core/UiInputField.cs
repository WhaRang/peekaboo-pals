using _Main.Scripts.Configs.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Core
{
	/// <summary>Code-built TMP input field in the game's style.</summary>
	public static class UiInputField
	{
		public static TMP_InputField Create(UiContext ctx, Transform parent, string placeholderText)
		{
			var palette = ctx.Palette;
			var root = UiBuilder.Rect("Input", parent);
			root.gameObject.SetActive(false);

			var background = root.gameObject.AddComponent<Image>();
			background.sprite = ctx.Sprite(UiSprite.RoundedRectSmall);
			background.type = Image.Type.Sliced;
			background.color = palette.NightDeep;

			var viewport = UiBuilder.Rect("TextArea", root);
			viewport.Stretch(30f, 30f, 14f, 14f);
			viewport.gameObject.AddComponent<RectMask2D>();

			var text = UiBuilder.Text(viewport, "Text", ctx, "", 44f, TextStyle.Body, palette.Cream, TextAlignmentOptions.Left);
			text.rectTransform.Stretch();
			text.textWrappingMode = TextWrappingModes.NoWrap;

			var placeholder = UiBuilder.Text(viewport, "Placeholder", ctx, placeholderText, 44f, TextStyle.Body, palette.Muted.WithAlpha(0.6f), TextAlignmentOptions.Left);
			placeholder.rectTransform.Stretch();
			placeholder.textWrappingMode = TextWrappingModes.NoWrap;

			var field = root.gameObject.AddComponent<TMP_InputField>();
			field.textViewport = viewport;
			field.textComponent = text;
			field.placeholder = placeholder;
			field.targetGraphic = background;
			field.fontAsset = text.font;
			field.pointSize = 44f;
			field.lineType = TMP_InputField.LineType.SingleLine;
			field.caretColor = palette.Slime;
			field.customCaretColor = true;
			field.caretWidth = 4;

			root.gameObject.SetActive(true);
			return field;
		}
	}
}
