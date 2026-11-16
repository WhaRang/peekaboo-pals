using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Widgets
{
	/// <summary>Chunky pill progress bar whose fill slides with an overshoot and a glint.</summary>
	public sealed class ProgressBar : MonoBehaviour
	{
		private RectTransform _fill = null!;
		private Image _fillImage = null!;
		private Image _glint = null!;
		private float _value;

		public static ProgressBar Create(Transform parent, UiContext ctx, Vector2 size, Color fillColor)
		{
			var root = UiBuilder.Rect("ProgressBar", parent);
			root.sizeDelta = size;

			var track = UiBuilder.Image(root, "Track", ctx, UiSprite.Pill, ctx.Palette.Ink.WithAlpha(0.75f));
			track.rectTransform.Stretch();

			var fillArea = UiBuilder.Rect("FillArea", root);
			fillArea.Stretch(6f, 6f, 6f, 6f);

			var fill = UiBuilder.Image(fillArea, "Fill", ctx, UiSprite.Pill, fillColor);
			var gloss = UiBuilder.Image(fill.rectTransform, "Gloss", ctx, UiSprite.Pill, new Color(1f, 1f, 1f, 0.3f));
			gloss.rectTransform.anchorMin = new Vector2(0f, 0.5f);
			gloss.rectTransform.anchorMax = Vector2.one;
			gloss.rectTransform.offsetMin = new Vector2(8f, 0f);
			gloss.rectTransform.offsetMax = new Vector2(-8f, -3f);

			var glint = UiBuilder.Image(fill.rectTransform, "Glint", ctx, UiSprite.SoftGlow, new Color(1f, 1f, 1f, 0f));
			glint.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(size.y * 2.2f, size.y * 2.2f));

			var bar = root.gameObject.AddComponent<ProgressBar>();
			bar._fill = fill.rectTransform;
			bar._fillImage = fill;
			bar._glint = glint;
			bar.Apply(0f);
			return bar;
		}

		public void SetImmediate(float value)
		{
			Tweens.Kill(this);
			_value = Mathf.Clamp01(value);
			Apply(_value);
		}

		public void AnimateTo(float value, float duration = 0.9f)
		{
			value = Mathf.Clamp01(value);
			float from = _value;
			_value = value;
			Tweens.Run(this, duration, k => Apply(Mathf.LerpUnclamped(from, value, k)), Ease.OutBack, TweenChannel.Fill);
			_glint.color = new Color(1f, 1f, 1f, 0.9f);
			_glint.FadeTo(0f, duration * 1.3f, Ease.InQuad);
		}

		private void Apply(float value)
		{
			_fillImage.enabled = value > 0.001f;
			_fill.anchorMin = Vector2.zero;
			_fill.anchorMax = new Vector2(Mathf.Max(0f, value), 1f);
			_fill.offsetMin = Vector2.zero;
			_fill.offsetMax = Vector2.zero;
		}
	}
}
