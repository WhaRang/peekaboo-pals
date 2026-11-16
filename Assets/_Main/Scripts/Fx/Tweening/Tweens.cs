using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Main.Scripts.Fx.Tweening
{
	/// <summary>Which property a tween drives; starting a tween on the same target + channel replaces the running one.</summary>
	public enum TweenChannel
	{
		Custom = 0,
		Scale = 1,
		Position = 2,
		Rotation = 3,
		Color = 4,
		Alpha = 5,
		Size = 6,
		Fill = 7
	}

	public sealed class Tween
	{
		internal object? Target;
		internal TweenChannel Channel;
		internal float Delay;
		internal float Duration;
		internal float Elapsed;
		internal Ease EaseType = Ease.OutQuad;
		internal Action? StartAction;
		internal Action<float>? Apply;
		internal Action? CompleteAction;
		internal int LoopsLeft;
		internal bool Yoyo;
		internal bool Started;
		internal bool Killed;
		internal bool Forward = true;

		public bool IsAlive => !Killed;

		public Tween SetDelay(float delay)
		{
			Delay = delay;
			return this;
		}

		public Tween SetEase(Ease ease)
		{
			EaseType = ease;
			return this;
		}

		public Tween OnComplete(Action onComplete)
		{
			CompleteAction += onComplete;
			return this;
		}

		/// <summary>-1 loops forever. With <paramref name="yoyo"/> every other loop plays backwards.</summary>
		public Tween SetLoops(int loops, bool yoyo = false)
		{
			LoopsLeft = loops;
			Yoyo = yoyo;
			return this;
		}

		public void Kill(bool complete = false)
		{
			if (Killed)
				return;

			if (complete)
			{
				Apply?.Invoke(Easing.Evaluate(EaseType, 1f));
				CompleteAction?.Invoke();
			}

			Killed = true;
		}
	}

	/// <summary>
	/// Minimal allocation-light tween engine for UI juice (unscaled time). Tweens run on a hidden runner object;
	/// a tween whose Unity target was destroyed is dropped automatically.
	/// </summary>
	public static class Tweens
	{
		private static TweenRunner? _runner;

		public static Tween Run(object? target, float duration, Action<float> apply, Ease ease = Ease.OutQuad, TweenChannel channel = TweenChannel.Custom)
		{
			EnsureRunner();

			if (target != null && channel != TweenChannel.Custom)
				_runner!.KillMatching(target, channel);

			var tween = new Tween
			{
				Target = target,
				Channel = channel,
				Duration = Mathf.Max(0.0001f, duration),
				Apply = apply,
				EaseType = ease
			};
			_runner!.Add(tween);
			return tween;
		}

		/// <summary>Like <see cref="Run"/>, but <paramref name="onStart"/> runs when the delay elapsed (read "from" values there).</summary>
		public static Tween Run(object? target, float duration, Action onStart, Action<float> apply, Ease ease, TweenChannel channel)
		{
			var tween = Run(target, duration, apply, ease, channel);
			tween.StartAction = onStart;
			return tween;
		}

		public static Tween Delay(float seconds, Action callback)
		{
			var tween = Run(null, seconds, _ => { }, Ease.Linear);
			tween.CompleteAction = callback;
			return tween;
		}

		public static void Kill(object target, bool complete = false)
		{
			_runner?.KillAll(target, complete);
		}

		private static void EnsureRunner()
		{
			if (_runner != null)
				return;

			var go = new GameObject("[Tweens]") { hideFlags = HideFlags.HideInHierarchy };
			if (Application.isPlaying)
				Object.DontDestroyOnLoad(go);
			_runner = go.AddComponent<TweenRunner>();
		}

		private sealed class TweenRunner : MonoBehaviour
		{
			private readonly List<Tween> _active = new(128);
			private readonly List<Tween> _pending = new(32);
			private bool _isUpdating;

			public void Add(Tween tween)
			{
				if (_isUpdating)
					_pending.Add(tween);
				else
					_active.Add(tween);
			}

			public void KillMatching(object target, TweenChannel channel)
			{
				KillIn(_active, target, channel, false);
				KillIn(_pending, target, channel, false);
			}

			public void KillAll(object target, bool complete)
			{
				KillIn(_active, target, null, complete);
				KillIn(_pending, target, null, complete);
			}

			private static void KillIn(List<Tween> list, object target, TweenChannel? channel, bool complete)
			{
				for (int i = 0; i < list.Count; i++)
				{
					var tween = list[i];
					if (!tween.Killed && ReferenceEquals(tween.Target, target) && (channel == null || tween.Channel == channel))
						tween.Kill(complete);
				}
			}

			private void Update()
			{
				float dt = Time.unscaledDeltaTime;
				_isUpdating = true;

				for (int i = 0; i < _active.Count; i++)
				{
					var tween = _active[i];
					if (tween.Killed)
						continue;

					if (tween.Target is Object unityTarget && unityTarget == null)
					{
						tween.Killed = true;
						continue;
					}

					try
					{
						Step(tween, dt);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
						tween.Killed = true;
					}
				}

				_isUpdating = false;
				_active.RemoveAll(t => t.Killed);

				if (_pending.Count > 0)
				{
					_active.AddRange(_pending);
					_pending.Clear();
				}
			}

			private static void Step(Tween tween, float dt)
			{
				if (tween.Delay > 0f)
				{
					tween.Delay -= dt;
					if (tween.Delay > 0f)
						return;
					dt = -tween.Delay;
				}

				if (!tween.Started)
				{
					tween.Started = true;
					tween.StartAction?.Invoke();
				}

				tween.Elapsed += dt;
				float t = Mathf.Clamp01(tween.Elapsed / tween.Duration);
				float k = tween.Forward ? t : 1f - t;
				tween.Apply?.Invoke(Easing.Evaluate(tween.EaseType, k));

				if (t < 1f)
					return;

				if (tween.LoopsLeft != 0)
				{
					if (tween.LoopsLeft > 0)
						tween.LoopsLeft--;
					tween.Elapsed = 0f;
					if (tween.Yoyo)
						tween.Forward = !tween.Forward;
					return;
				}

				tween.Killed = true;
				tween.CompleteAction?.Invoke();
			}
		}
	}
}
