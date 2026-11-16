using System;
using _Main.Scripts._ProjectAgnostic.Providers.Serialization;
using JetBrains.Annotations;
using UnityEngine;

namespace _Main.Scripts.Services.Settings
{
	public interface ISettingsService
	{
		bool IsMuted { get; set; }
		bool IsHapticsEnabled { get; set; }

		/// <summary>The player's own reduced-motion toggle.</summary>
		bool ForceReducedMotion { get; set; }

		/// <summary>True when either the OS asks for reduced motion or the player forced it.</summary>
		bool IsReducedMotion { get; }

		/// <summary>The player's display name (generated on first launch, editable, survives "Reset progress").</summary>
		string Nickname { get; set; }

		/// <summary>The collection the player looked at last (empty = the app's featured one).</summary>
		string ViewedPackId { get; set; }

		/// <summary>The newest app version the player was already told about (so the "new version" popup shows once).</summary>
		string SeenAppVersion { get; set; }

		event Action? Changed;
	}

	/// <summary>
	/// Player settings persisted under their own PlayerPrefs key, separate from the gameplay save, so
	/// "Reset progress" (which clears the gameplay save) keeps them.
	/// </summary>
	[UsedImplicitly]
	public sealed class SettingsService : ISettingsService
	{
		private const string StorageKey = "SpookySettings";

		private readonly IGameplaySerializationProvider _serializationProvider;
		private readonly bool _systemReducedMotion;
		private SettingsData _data;

		public event Action? Changed;

		public SettingsService(IGameplaySerializationProvider serializationProvider)
		{
			_serializationProvider = serializationProvider;
			_data = Load();
			_systemReducedMotion = ReadSystemReducedMotion();

			if (string.IsNullOrWhiteSpace(_data.Nickname))
			{
				_data.Nickname = NicknameGenerator.Generate();
				Save();
			}
		}

		public bool IsMuted
		{
			get => _data.Muted;
			set => Update(ref _data.Muted, value);
		}

		public bool IsHapticsEnabled
		{
			get => _data.HapticsEnabled;
			set => Update(ref _data.HapticsEnabled, value);
		}

		public bool ForceReducedMotion
		{
			get => _data.ForceReducedMotion;
			set => Update(ref _data.ForceReducedMotion, value);
		}

		public bool IsReducedMotion => _systemReducedMotion || _data.ForceReducedMotion;

		public string Nickname
		{
			get => _data.Nickname ?? string.Empty;
			set
			{
				var clean = NicknameGenerator.Sanitize(value);
				if (clean == _data.Nickname)
					return;

				_data.Nickname = clean;
				Save();
				Changed?.Invoke();
			}
		}

		public string ViewedPackId
		{
			get => _data.ViewedPackId ?? string.Empty;
			set
			{
				if (value == _data.ViewedPackId)
					return;

				// No Changed event: nothing visible depends on it besides the screen that just switched.
				_data.ViewedPackId = value;
				Save();
			}
		}

		public string SeenAppVersion
		{
			get => _data.SeenAppVersion ?? string.Empty;
			set
			{
				if (value == _data.SeenAppVersion)
					return;

				_data.SeenAppVersion = value;
				Save();
			}
		}

		private void Update(ref bool field, bool value)
		{
			if (field == value)
				return;

			field = value;
			Save();
			Changed?.Invoke();
		}

		private SettingsData Load()
		{
			var raw = PlayerPrefs.GetString(StorageKey, string.Empty);
			if (string.IsNullOrEmpty(raw))
				return SettingsData.Default;

			var result = _serializationProvider.Deserialize<SettingsData>(raw);
			return result.IsT0 ? result.AsT0.Value : SettingsData.Default;
		}

		private void Save()
		{
			_serializationProvider.Serialize(_data).Switch(
				success =>
				{
					PlayerPrefs.SetString(StorageKey, success.Value);
					PlayerPrefs.Save();
				},
				error => Debug.LogError($"[{nameof(SettingsService)}] Failed to save settings: {error.Value}"));
		}

		/// <summary>Android has no dedicated "reduce motion" flag; "Remove animations" sets the animator duration scale to 0.</summary>
		private static bool ReadSystemReducedMotion()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
				using var global = new AndroidJavaClass("android.provider.Settings$Global");
				float scale = global.CallStatic<float>("getFloat", resolver, "animator_duration_scale", 1f);
				return scale <= 0.01f;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[{nameof(SettingsService)}] Could not read system animation scale: {e.Message}");
				return false;
			}
#elif UNITY_WEBGL && !UNITY_EDITOR
			// iOS/Android "Reduce motion" reaches the page as the prefers-reduced-motion media query.
			return _Main.Scripts.Services.Web.WebPlatform.PrefersReducedMotion;
#else
			return false;
#endif
		}

		private struct SettingsData
		{
			public bool Muted;
			public bool HapticsEnabled;
			public bool ForceReducedMotion;
			public string? Nickname;
			public string? ViewedPackId;
			public string? SeenAppVersion;

			public static SettingsData Default => new() { Muted = false, HapticsEnabled = true, ForceReducedMotion = false };
		}
	}
}
