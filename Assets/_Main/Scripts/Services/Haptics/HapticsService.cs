using System;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Services.Settings;
using JetBrains.Annotations;
using UnityEngine;

namespace _Main.Scripts.Services.Haptics
{
	public interface IHapticsService
	{
		void Play(HapticStrength strength);
	}

	/// <summary>
	/// Android vibration with amplitude control (VibrationEffect, API 26+), falling back to plain durations on older
	/// devices. No-op in the editor and when the player disabled haptics.
	/// </summary>
	[UsedImplicitly]
	public sealed class HapticsService : IHapticsService, IDisposable
	{
		private readonly ISettingsService _settings;

#if UNITY_ANDROID && !UNITY_EDITOR
		private AndroidJavaObject? _vibrator;
		private AndroidJavaClass? _vibrationEffect;
		private int _sdkInt;
		private bool _initialized;
#endif

		public HapticsService(ISettingsService settings)
		{
			_settings = settings;
		}

		public void Play(HapticStrength strength)
		{
			if (strength == HapticStrength.None || !_settings.IsHapticsEnabled)
				return;

#if UNITY_WEBGL && !UNITY_EDITOR
			_Main.Scripts.Services.Web.WebPlatform.Vibrate(strength switch
			{
				HapticStrength.Light => 15,
				HapticStrength.Medium => 30,
				HapticStrength.Heavy => 60,
				_ => 250
			});
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				EnsureInitialized();
				if (_vibrator == null)
					return;

				switch (strength)
				{
					case HapticStrength.Light:
						OneShot(18, 70);
						break;
					case HapticStrength.Medium:
						OneShot(35, 150);
						break;
					case HapticStrength.Heavy:
						OneShot(70, 255);
						break;
					case HapticStrength.Epic:
						Waveform(new long[] { 0, 60, 70, 60, 70, 140, 90, 320 }, new[] { 0, 120, 0, 180, 0, 255, 0, 255 });
						break;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[{nameof(HapticsService)}] Vibration failed: {e.Message}");
				_vibrator = null;
			}
#endif
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		private void EnsureInitialized()
		{
			if (_initialized)
				return;

			_initialized = true;
			using var version = new AndroidJavaClass("android.os.Build$VERSION");
			_sdkInt = version.GetStatic<int>("SDK_INT");

			using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			_vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

			if (_sdkInt >= 26)
				_vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
		}

		private void OneShot(long milliseconds, int amplitude)
		{
			if (_vibrationEffect != null)
			{
				using var effect = _vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
				_vibrator!.Call("vibrate", effect);
			}
			else
			{
				_vibrator!.Call("vibrate", milliseconds);
			}
		}

		private void Waveform(long[] timings, int[] amplitudes)
		{
			if (_vibrationEffect != null)
			{
				using var effect = _vibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1);
				_vibrator!.Call("vibrate", effect);
			}
			else
			{
				_vibrator!.Call("vibrate", timings, -1);
			}
		}
#endif

#if UNITY_ANDROID
		/// <summary>Never called; referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the manifest.</summary>
		[UsedImplicitly]
		private static void EnsureVibratePermissionIsAdded() => Handheld.Vibrate();
#endif

		public void Dispose()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			_vibrator?.Dispose();
			_vibrationEffect?.Dispose();
#endif
		}
	}
}
