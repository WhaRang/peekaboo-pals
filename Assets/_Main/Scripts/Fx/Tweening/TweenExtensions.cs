using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Fx.Tweening
{
	/// <summary>Fluent helpers for the common UI tweens. "From" values are read when the tween actually starts (after its delay).</summary>
	public static class TweenExtensions
	{
		public static Tween ScaleTo(this Transform transform, Vector3 to, float duration, Ease ease = Ease.OutBack)
		{
			var from = Vector3.zero;
			return Tweens.Run(transform, duration, () => from = transform.localScale,
				k => transform.localScale = Vector3.LerpUnclamped(from, to, k), ease, TweenChannel.Scale);
		}

		public static Tween ScaleTo(this Transform transform, float to, float duration, Ease ease = Ease.OutBack)
			=> transform.ScaleTo(new Vector3(to, to, 1f), duration, ease);

		public static Tween ScaleFromTo(this Transform transform, float from, float to, float duration, Ease ease = Ease.OutBack)
		{
			transform.localScale = new Vector3(from, from, 1f);
			return transform.ScaleTo(to, duration, ease);
		}

		/// <summary>Squash-and-stretch punch that settles back on the original scale.</summary>
		public static Tween PunchScale(this Transform transform, float strength, float duration, int vibrato = 3)
		{
			var baseScale = Vector3.one;
			return Tweens.Run(transform, duration, () => baseScale = transform.localScale, k =>
			{
				float decay = 1f - k;
				float wave = Mathf.Sin(k * Mathf.PI * vibrato) * strength * decay;
				transform.localScale = new Vector3(baseScale.x * (1f + wave), baseScale.y * (1f - wave * 0.6f), 1f);
			}, Ease.Linear, TweenChannel.Scale).OnComplete(() => transform.localScale = baseScale);
		}

		public static Tween MoveTo(this RectTransform rect, Vector2 to, float duration, Ease ease = Ease.OutCubic)
		{
			var from = Vector2.zero;
			return Tweens.Run(rect, duration, () => from = rect.anchoredPosition,
				k => rect.anchoredPosition = Vector2.LerpUnclamped(from, to, k), ease, TweenChannel.Position);
		}

		public static Tween RotateTo(this Transform transform, float toDegrees, float duration, Ease ease = Ease.OutCubic)
		{
			float from = 0f;
			return Tweens.Run(transform, duration, () => from = transform.localEulerAngles.z > 180f ? transform.localEulerAngles.z - 360f : transform.localEulerAngles.z,
				k => transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(from, toDegrees, k)), ease, TweenChannel.Rotation);
		}

		/// <summary>Rotational wobble around the current angle that settles back to it.</summary>
		public static Tween Wobble(this Transform transform, float degrees, float duration, int vibrato = 4)
		{
			float baseAngle = 0f;
			return Tweens.Run(transform, duration, () => baseAngle = transform.localEulerAngles.z, k =>
			{
				float angle = Mathf.Sin(k * Mathf.PI * 2f * vibrato) * degrees * (1f - k);
				transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + angle);
			}, Ease.Linear, TweenChannel.Rotation).OnComplete(() => transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle));
		}

		public static Tween FadeTo(this CanvasGroup group, float to, float duration, Ease ease = Ease.OutQuad)
		{
			float from = 0f;
			return Tweens.Run(group, duration, () => from = group.alpha,
				k => group.alpha = Mathf.LerpUnclamped(from, to, k), ease, TweenChannel.Alpha);
		}

		public static Tween FadeTo(this Graphic graphic, float to, float duration, Ease ease = Ease.OutQuad)
		{
			float from = 0f;
			return Tweens.Run(graphic, duration, () => from = graphic.color.a, k =>
			{
				var c = graphic.color;
				c.a = Mathf.LerpUnclamped(from, to, k);
				graphic.color = c;
			}, ease, TweenChannel.Alpha);
		}

		public static Tween ColorTo(this Graphic graphic, Color to, float duration, Ease ease = Ease.OutQuad)
		{
			var from = Color.white;
			return Tweens.Run(graphic, duration, () => from = graphic.color,
				k => graphic.color = Color.LerpUnclamped(from, to, k), ease, TweenChannel.Color);
		}

		public static Tween SizeTo(this RectTransform rect, Vector2 to, float duration, Ease ease = Ease.OutCubic)
		{
			var from = Vector2.zero;
			return Tweens.Run(rect, duration, () => from = rect.sizeDelta,
				k => rect.sizeDelta = Vector2.LerpUnclamped(from, to, k), ease, TweenChannel.Size);
		}

		public static Tween FillTo(this Image image, float to, float duration, Ease ease = Ease.OutCubic)
		{
			float from = 0f;
			return Tweens.Run(image, duration, () => from = image.fillAmount,
				k => image.fillAmount = Mathf.LerpUnclamped(from, to, k), ease, TweenChannel.Fill);
		}
	}
}
