using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using TMPro;
using UnityEngine;

namespace _Main.Scripts.Ui.Widgets
{
	/// <summary>Numbers never jump: they roll up with a little punch and tick sounds.</summary>
	public sealed class CountUpText : MonoBehaviour
	{
		private UiContext _ctx = null!;
		private TextMeshProUGUI _label = null!;
		private Func<int, string> _format = v => v.ToString();
		private int _shown;
		private Tween? _tween;

		public int Value { get; private set; }

		public static CountUpText Attach(TextMeshProUGUI label, UiContext ctx, Func<int, string>? format = null)
		{
			var counter = label.gameObject.AddComponent<CountUpText>();
			counter._ctx = ctx;
			counter._label = label;
			if (format != null)
				counter._format = format;
			counter.SetImmediate(0);
			return counter;
		}

		public void SetImmediate(int value)
		{
			_tween?.Kill();
			Value = value;
			_shown = value;
			_label.text = _format(value);
		}

		public void AnimateTo(int value, float duration = 0.8f, bool ticks = true)
		{
			_tween?.Kill();
			int from = _shown;
			Value = value;

			if (from == value)
				return;

			if (_ctx.ReducedMotion)
				duration *= 0.4f;

			_tween = Tweens.Run(_label, duration, k =>
			{
				int next = Mathf.RoundToInt(Mathf.Lerp(from, value, k));
				if (next == _shown)
					return;

				_shown = next;
				_label.text = _format(next);
				if (ticks)
					_ctx.Audio.Play(GameSound.PointsTick, 0.9f + 0.4f * k, 0.6f);
			}, Ease.OutCubic).OnComplete(() =>
			{
				_label.text = _format(value);
				_shown = value;
				_label.transform.PunchScale(0.25f, 0.4f, 2);
			});
		}
	}
}
