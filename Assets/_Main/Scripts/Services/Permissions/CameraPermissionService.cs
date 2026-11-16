using System;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
using _Main.Scripts.Services.Qr;
#endif

namespace _Main.Scripts.Services.Permissions
{
	public enum CameraPermissionState
	{
		Granted = 0,
		/// <summary>Never asked, or denied once: asking again shows the OS dialog.</summary>
		NotGranted = 1,
		/// <summary>Denied with "don't ask again": only the app settings screen can grant it now.</summary>
		PermanentlyDenied = 2
	}

	public interface ICameraPermissionService
	{
		CameraPermissionState State { get; }
		void Request(Action<CameraPermissionState> onResult);
		void OpenAppSettings();
	}

	/// <summary>
	/// Android camera permission flow. The UI explains why the camera is needed before calling <see cref="Request"/>,
	/// and offers the app settings screen when the permission was permanently denied. Always granted in the editor.
	/// </summary>
	[UsedImplicitly]
	public sealed class CameraPermissionService : ICameraPermissionService
	{
		private bool _wasDeniedOnce;

		public CameraPermissionState State
		{
			get
			{
#if UNITY_ANDROID && !UNITY_EDITOR
				if (Permission.HasUserAuthorizedPermission(Permission.Camera))
					return CameraPermissionState.Granted;

				// After a denial Android stops showing the rationale flag only when "don't ask again" was chosen.
				if (_wasDeniedOnce && !Permission.ShouldShowRequestPermissionRationale(Permission.Camera))
					return CameraPermissionState.PermanentlyDenied;

				return CameraPermissionState.NotGranted;
#elif UNITY_WEBGL && !UNITY_EDITOR
				// Asked through our browser camera (HTTPS/localhost required); remembered for this page load.
				return WebGlCamera.Permission switch
				{
					1 => CameraPermissionState.Granted,
					2 => CameraPermissionState.PermanentlyDenied,
					_ => _wasDeniedOnce ? CameraPermissionState.PermanentlyDenied : CameraPermissionState.NotGranted
				};
#else
				return CameraPermissionState.Granted;
#endif
			}
		}

		public void Request(Action<CameraPermissionState> onResult)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			if (Permission.HasUserAuthorizedPermission(Permission.Camera))
			{
				onResult(CameraPermissionState.Granted);
				return;
			}

			var callbacks = new PermissionCallbacks();
			callbacks.PermissionGranted += _ => onResult(CameraPermissionState.Granted);
			callbacks.PermissionDenied += _ =>
			{
				_wasDeniedOnce = true;
				onResult(State);
			};
			Permission.RequestUserPermission(Permission.Camera, callbacks);
#elif UNITY_WEBGL && !UNITY_EDITOR
			// Opening the camera is the permission prompt; the stream stays open and the scanner picks it up.
			// (Unity's RequestUserAuthorization leaves an extra camera stream running, which breaks iOS Safari.)
			WebGlCamera.Open(result =>
			{
				_wasDeniedOnce |= result == WebGlCamera.OpenResult.Denied;
				onResult(result switch
				{
					WebGlCamera.OpenResult.Granted => CameraPermissionState.Granted,
					WebGlCamera.OpenResult.Denied => CameraPermissionState.PermanentlyDenied,
					_ => CameraPermissionState.NotGranted
				});
			});
#else
			onResult(CameraPermissionState.Granted);
#endif
		}

		public void OpenAppSettings()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				string packageName = activity.Call<string>("getPackageName");
				using var uriClass = new AndroidJavaClass("android.net.Uri");
				using var uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null);
				using var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri);
				intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
				activity.Call("startActivity", intent);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[{nameof(CameraPermissionService)}] Could not open app settings: {e.Message}");
			}
#elif UNITY_WEBGL && !UNITY_EDITOR
			// A page can't open browser settings; asking again re-prompts where the browser allows it.
			Request(_ => { });
#else
			Debug.Log($"[{nameof(CameraPermissionService)}] OpenAppSettings is a no-op outside Android devices.");
#endif
		}
	}
}
