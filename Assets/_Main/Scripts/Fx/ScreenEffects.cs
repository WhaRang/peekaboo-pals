using _Main.Scripts.Fx.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Fx
{
	/// <summary>Full-screen juice: camera-style shake of the content root, color flashes and dimming.</summary>
	public sealed class ScreenEffects : MonoBehaviour
	{
		private RectTransform _shakeRoot = null!;
		private Image _flash = null!;
		private Image _dim = null!;

		private float _shakeStrength;
		private float _shakeTimeLeft;
		private float _shakeDuration;
		private float _seed;

		public bool ReducedMotion { get; set; }

		public void Setup(RectTransform shakeRoot, Image flash, Image dim)
		{
			_shakeRoot = shakeRoot;
			_flash = flash;
			_dim = dim;
			_flash.color = new Color(1f, 1f, 1f, 0f);
			_dim.color = new Color(0f, 0f, 0f, 0f);
			_flash.raycastTarget = false;
			_dim.raycastTarget = false;
			_seed = Random.value * 100f;
		}

		/// <summary>Strength in reference pixels.</summary>
		public void Shake(float strength, float duration)
		{
			if (ReducedMotion || strength <= 0f)
				return;

			_shakeStrength = Mathf.Max(_shakeStrength * (_shakeTimeLeft / Mathf.Max(0.01f, _shakeDuration)), strength);
			_shakeDuration = duration;
			_shakeTimeLeft = duration;
		}

		public void Flash(Color color, float peakAlpha = 0.85f, float duration = 0.45f)
		{
			if (ReducedMotion)
				peakAlpha *= 0.35f;

			color.a = peakAlpha;
			_flash.color = color;
			_flash.FadeTo(0f, duration, Ease.OutQuad);
		}

		public void Dim(float alpha, float duration)
		{
			_dim.FadeTo(alpha, duration, Ease.OutQuad);
		}

		private void Update()
		{
			if (_shakeTimeLeft <= 0f)
				return;

			_shakeTimeLeft -= Time.unscaledDeltaTime;
			if (_shakeTimeLeft <= 0f)
			{
				_shakeRoot.anchoredPosition = Vector2.zero;
				_shakeRoot.localRotation = Quaternion.identity;
				return;
			}

			float k = _shakeTimeLeft / _shakeDuration;
			float amount = _shakeStrength * k * k;
			float time = Time.unscaledTime * 38f;
			var offset = new Vector2(Mathf.PerlinNoise(_seed, time) - 0.5f, Mathf.PerlinNoise(time, _seed) - 0.5f) * (2f * amount);
			_shakeRoot.anchoredPosition = offset;
			_shakeRoot.localRotation = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(_seed + 7f, time) - 0.5f) * amount * 0.08f);
		}
	}
}
