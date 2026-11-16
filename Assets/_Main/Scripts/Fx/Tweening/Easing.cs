using UnityEngine;

namespace _Main.Scripts.Fx.Tweening
{
	public enum Ease
	{
		Linear = 0,
		InQuad,
		OutQuad,
		InOutQuad,
		InCubic,
		OutCubic,
		InOutCubic,
		InBack,
		OutBack,
		OutBackStrong,
		OutElastic,
		OutBounce,
		InOutSine,
		OutSine,
		InExpo,
		OutExpo
	}

	public static class Easing
	{
		public static float Evaluate(Ease ease, float t)
		{
			switch (ease)
			{
				case Ease.Linear: return t;
				case Ease.InQuad: return t * t;
				case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
				case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
				case Ease.InCubic: return t * t * t;
				case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
				case Ease.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
				case Ease.InBack:
				{
					const float c1 = 1.70158f;
					return (c1 + 1f) * t * t * t - c1 * t * t;
				}
				case Ease.OutBack: return OutBack(t, 1.70158f);
				case Ease.OutBackStrong: return OutBack(t, 3.2f);
				case Ease.OutElastic:
				{
					if (t <= 0f) return 0f;
					if (t >= 1f) return 1f;
					const float c4 = 2f * Mathf.PI / 3f;
					return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
				}
				case Ease.OutBounce: return OutBounce(t);
				case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
				case Ease.OutSine: return Mathf.Sin(t * Mathf.PI / 2f);
				case Ease.InExpo: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
				case Ease.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
				default: return t;
			}
		}

		private static float OutBack(float t, float c1)
		{
			float c3 = c1 + 1f;
			return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
		}

		private static float OutBounce(float t)
		{
			const float n1 = 7.5625f;
			const float d1 = 2.75f;

			if (t < 1f / d1)
				return n1 * t * t;
			if (t < 2f / d1)
				return n1 * (t -= 1.5f / d1) * t + 0.75f;
			if (t < 2.5f / d1)
				return n1 * (t -= 2.25f / d1) * t + 0.9375f;
			return n1 * (t -= 2.625f / d1) * t + 0.984375f;
		}
	}
}
