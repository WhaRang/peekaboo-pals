using System;
using System.Runtime.InteropServices;
using AOT;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Main.Scripts.Services.Qr
{
	/// <summary>
	/// The browser camera (Assets/Plugins/WebGL/SpookyWeb.jslib). Used instead of <see cref="WebCamTexture"/>, which
	/// stays black in iOS Safari. Stubs outside WebGL builds.
	/// </summary>
	public static class WebGlCamera
	{
		public enum OpenResult
		{
			Granted = 1,
			Denied = 2,
			Failed = 3
		}

		private static Action<OpenResult>? _pendingOpen;

#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")] private static extern int SpookyCam_IsSupported();
		[DllImport("__Internal")] private static extern int SpookyCam_Permission();
		[DllImport("__Internal")] private static extern void SpookyCam_Open(Action<int>? callback);
		[DllImport("__Internal")] private static extern void SpookyCam_Close();
		[DllImport("__Internal")] private static extern int SpookyCam_State();
		[DllImport("__Internal")] private static extern int SpookyCam_Width();
		[DllImport("__Internal")] private static extern int SpookyCam_Height();
		[DllImport("__Internal")] private static extern int SpookyCam_Upload(int textureId, int width, int height);
		[DllImport("__Internal")] private static extern int SpookyCam_Decode(int thorough);
		[DllImport("__Internal")] private static extern int SpookyQr_IsBusy();
		[DllImport("__Internal")] private static extern string SpookyQr_Poll();
#else
		private static int SpookyCam_IsSupported() => 0;
		private static int SpookyCam_Permission() => 0;
		private static void SpookyCam_Open(Action<int>? callback) { }
		private static void SpookyCam_Close() { }
		private static int SpookyCam_State() => 0;
		private static int SpookyCam_Width() => 0;
		private static int SpookyCam_Height() => 0;
		private static int SpookyCam_Upload(int textureId, int width, int height) => 0;
		private static int SpookyCam_Decode(int thorough) => 0;
		private static int SpookyQr_IsBusy() => 0;
		private static string? SpookyQr_Poll() => null;
#endif

		public static bool IsSupported => SpookyCam_IsSupported() != 0;

		/// <summary>0 unknown (never asked this page load), 1 granted, 2 denied.</summary>
		public static int Permission => SpookyCam_Permission();

		/// <summary>0 closed, 1 starting, 2 playing, -1 failed.</summary>
		public static int State => SpookyCam_State();

		public static int Width => SpookyCam_Width();
		public static int Height => SpookyCam_Height();
		public static bool IsDecoderBusy => SpookyQr_IsBusy() != 0;

		/// <summary>Opens the back camera. The browser asks for permission the first time; the stream stays open for the scanner.</summary>
		public static void Open(Action<OpenResult>? onResult = null)
		{
			_pendingOpen = onResult;
			SpookyCam_Open(onResult != null ? OnOpened : null);
		}

		public static void Close()
		{
			_pendingOpen = null;
			SpookyCam_Close();
		}

		public static bool Upload(Texture texture, int width, int height) => SpookyCam_Upload((int)texture.GetNativeTexturePtr(), width, height) != 0;

		public static bool Decode(bool thorough) => SpookyCam_Decode(thorough ? 1 : 0) != 0;

		public static bool TryTakeResult(out string payload)
		{
			var result = SpookyQr_Poll();
			payload = result ?? string.Empty;
			return !string.IsNullOrEmpty(result);
		}

		[MonoPInvokeCallback(typeof(Action<int>))]
		private static void OnOpened(int code)
		{
			var callback = _pendingOpen;
			_pendingOpen = null;
			callback?.Invoke((OpenResult)code);
		}
	}

	/// <summary>
	/// WebGL scanner: the browser camera is drawn into a texture each new frame, and the same frames are decoded by jsQR
	/// in a Web Worker. Browsers deliver frames upright, so there is no rotation or mirroring to undo.
	/// </summary>
	[UsedImplicitly]
	public sealed class WebGlQrCameraScanner : IQrCameraScanner, IDisposable
	{
		private const float DecodeInterval = 0.05f;
		private const int ThoroughEvery = 4;

		private Texture2D? _texture;
		private bool _open;
		private bool _freshFrame;
		private float _nextDecodeTime;
		private int _attempt;

		public bool IsRunning => _open && WebGlCamera.State > 0;
		public Texture? CameraTexture => _texture;
		public int RotationAngle => 0;
		public bool VerticallyMirrored => false;
		public bool HasAnyCamera => WebGlCamera.IsSupported;

		public bool TryStart(out string error)
		{
			error = string.Empty;
			if (_open)
				return true;

			if (!WebGlCamera.IsSupported)
			{
				error = "This browser can't use the camera. Open the game in Safari or Chrome over https.";
				return false;
			}

			WebGlCamera.Open();
			_open = true;
			_freshFrame = false;
			_nextDecodeTime = 0f;
			return true;
		}

		public void Stop()
		{
			if (_open)
				WebGlCamera.Close();
			_open = false;

			if (_texture != null)
			{
				Object.Destroy(_texture);
				_texture = null;
			}

			WebGlCamera.TryTakeResult(out _);
		}

		public void FocusCenter()
		{
			// Browsers keep continuous autofocus on; there is no focus point to set.
		}

		public bool Tick(out string payload)
		{
			if (WebGlCamera.TryTakeResult(out payload))
				return true;

			if (!_open || WebGlCamera.State != 2)
				return false;

			int width = WebGlCamera.Width;
			int height = WebGlCamera.Height;
			if (width < 64 || height < 64)
				return false;

			// Created once the real frame size is known, and again if it changes (the phone was rotated).
			if (_texture == null || _texture.width != width || _texture.height != height)
			{
				if (_texture != null)
					Object.Destroy(_texture);
				_texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
				{
					name = "BrowserCamera",
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp
				};
				_texture.Apply(false, false);
			}

			if (WebGlCamera.Upload(_texture, width, height))
				_freshFrame = true;

			if (!_freshFrame || WebGlCamera.IsDecoderBusy || UnityEngine.Time.unscaledTime < _nextDecodeTime)
				return false;

			if (WebGlCamera.Decode(++_attempt % ThoroughEvery == 0))
			{
				_freshFrame = false;
				_nextDecodeTime = UnityEngine.Time.unscaledTime + DecodeInterval;
			}

			return false;
		}

		public void Dispose() => Stop();
	}
}
