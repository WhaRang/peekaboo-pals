using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using ZXing;
using Object = UnityEngine.Object;

namespace _Main.Scripts.Services.Qr
{
	public interface IQrCameraScanner
	{
		bool IsRunning { get; }

		/// <summary>The live camera image; null until the first frame arrives.</summary>
		Texture? CameraTexture { get; }

		/// <summary>Clockwise rotation that makes the camera image upright.</summary>
		int RotationAngle { get; }

		bool VerticallyMirrored { get; }
		bool HasAnyCamera { get; }

		bool TryStart(out string error);
		void Stop();

		/// <summary>
		/// Tap to focus: runs one focus pass on the centre of the view, then hands back to the camera's continuous
		/// autofocus. (Leaving a focus point set would lock the focus on Android.)
		/// </summary>
		void FocusCenter();

		/// <summary>
		/// Call once per frame while running. Grabs a camera frame every <c>DecodeInterval</c> seconds and decodes it
		/// on a worker thread; returns true on the frame a decoded payload becomes available.
		/// </summary>
		bool Tick(out string payload);
	}

	/// <summary>
	/// Offline QR scanning on top of <see cref="WebCamTexture"/>. Frames are copied on the main thread (the only
	/// thread allowed to touch Unity textures) and handed to an <see cref="IQrFrameDecoder"/> that never blocks the
	/// UI (ZXing.Net on the thread pool). Prefers the back camera. WebGL builds use <see cref="WebGlQrCameraScanner"/>.
	/// </summary>
	[UsedImplicitly]
	public sealed class QrCameraScanner : IQrCameraScanner, IDisposable
	{
		// Decoding never runs on the main thread (thread pool on native, a Web Worker on WebGL), so we only wait a
		// little between attempts; the main-thread cost per attempt is one GetPixels32.
		private const float DecodeInterval = 0.05f;
		private const int MaxDecodeDimension = 1280;

		private readonly IQrFrameDecoder _decoder;

		private WebCamTexture? _camera;
		private Color32[]? _frame;
		private float _nextDecodeTime;
		private bool _focusPointSupported;
		private float _focusResetTime = -1f;

		public bool IsRunning => _camera != null && _camera.isPlaying;
		public Texture? CameraTexture => _camera;
		public int RotationAngle => _camera != null ? _camera.videoRotationAngle : 0;
		public bool VerticallyMirrored => _camera != null && _camera.videoVerticallyMirrored;
		public bool HasAnyCamera => WebCamTexture.devices.Length > 0;

		public QrCameraScanner(IQrFrameDecoder decoder)
		{
			_decoder = decoder;
		}

		public bool TryStart(out string error)
		{
			error = string.Empty;

			if (IsRunning)
				return true;

#if SPOOKY_AUTOPILOT && !UNITY_WEBGL
			error = "Camera disabled in autopilot.";
			return false;
#endif
			var devices = WebCamTexture.devices;
			if (devices.Length == 0)
			{
				error = "No camera found on this device.";
				return false;
			}

			var device = PickBackCamera(devices);

			try
			{
				_camera = new WebCamTexture(device.name, 1280, 720, 30) { filterMode = FilterMode.Bilinear };
				_camera.Play();
				// No focus point here on purpose: setting one switches Android from continuous autofocus to a single
				// locked focus pass, and the camera then never refocuses. Tap to focus uses FocusCenter() instead.
				_focusPointSupported = device.isAutoFocusPointSupported;
				_focusResetTime = -1f;
			}
			catch (Exception e)
			{
				error = $"Camera failed to start: {e.Message}";
				Stop();
				return false;
			}

			_nextDecodeTime = 0f;
			return true;
		}

		/// <summary>Native platforms flag the back camera; browsers only name it ("back", "rear", "environment").</summary>
		private static WebCamDevice PickBackCamera(WebCamDevice[] devices)
		{
			foreach (var candidate in devices)
			{
				var name = candidate.name.ToLowerInvariant();
				if (name.Contains("back") || name.Contains("rear") || name.Contains("environment"))
					return candidate;
			}

			foreach (var candidate in devices)
			{
				if (!candidate.isFrontFacing)
					return candidate;
			}

			return devices[devices.Length - 1];
		}

		public void Stop()
		{
			if (_camera != null)
			{
				if (_camera.isPlaying)
					_camera.Stop();

				Object.Destroy(_camera);
				_camera = null;
			}

			_decoder.TryTake(out _);
		}

		public void FocusCenter()
		{
			if (_camera == null || !_camera.isPlaying || !_focusPointSupported)
				return;

			_camera.autoFocusPoint = new Vector2(0.5f, 0.5f);
			_focusResetTime = UnityEngine.Time.unscaledTime + 2.5f;
		}

		public bool Tick(out string payload)
		{
			if (_focusResetTime > 0f && UnityEngine.Time.unscaledTime >= _focusResetTime && _camera != null)
			{
				// Back to the camera's default continuous autofocus.
				_focusResetTime = -1f;
				_camera.autoFocusPoint = null;
			}

			if (_decoder.TryTake(out payload))
				return true;

			if (_camera == null || !_camera.isPlaying || !_camera.didUpdateThisFrame || _decoder.IsBusy)
				return false;

			// WebCamTexture reports 16x16 until the first real frame arrives.
			if (_camera.width < 64 || _camera.height < 64)
				return false;

			if (UnityEngine.Time.unscaledTime < _nextDecodeTime)
				return false;

			_nextDecodeTime = UnityEngine.Time.unscaledTime + DecodeInterval;

			int width = _camera.width;
			int height = _camera.height;

			if (_frame == null || _frame.Length != width * height)
				_frame = new Color32[width * height];

			_camera.GetPixels32(_frame);
			_decoder.Submit(_frame, width, height);
			return false;
		}

		public static BarcodeReaderGeneric CreateReader(bool thorough = true) => new()
		{
			AutoRotate = false,
			Options =
			{
				PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
				TryHarder = thorough,
				TryInverted = thorough
			}
		};

		/// <summary>
		/// Decodes one camera frame (Unity pixel order: bottom row first). <paramref name="crop"/> &lt; 1 decodes only a
		/// centred square of that fraction of the short side; big regions are downsampled to <paramref name="maxDimension"/>.
		/// Rows are flipped while building the luminance plane. Thread-safe as long as each thread owns its reader and buffer.
		/// </summary>
		public static bool TryDecode(BarcodeReaderGeneric reader, Color32[] frame, int width, int height, ref byte[]? luminance, out string text,
			float crop = 1f, int maxDimension = MaxDecodeDimension)
		{
			int regionWidth = width;
			int regionHeight = height;
			if (crop < 1f)
			{
				regionWidth = regionHeight = Mathf.RoundToInt(Mathf.Min(width, height) * crop);
			}

			int left = (width - regionWidth) / 2;
			int top = (height - regionHeight) / 2;
			int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(regionWidth, regionHeight) / (float)maxDimension));
			int lumWidth = regionWidth / step;
			int lumHeight = regionHeight / step;

			if (luminance == null || luminance.Length != lumWidth * lumHeight)
				luminance = new byte[lumWidth * lumHeight];

			for (int y = 0; y < lumHeight; y++)
			{
				int sourceRow = (height - 1 - (top + y * step)) * width + left;
				int targetRow = y * lumWidth;
				for (int x = 0; x < lumWidth; x++)
				{
					var c = frame[sourceRow + x * step];
					// Integer Rec.601 luma.
					luminance[targetRow + x] = (byte)((c.r * 77 + c.g * 150 + c.b * 29) >> 8);
				}
			}

			var result = reader.Decode(new PlanarYUVLuminanceSource(luminance, lumWidth, lumHeight, 0, 0, lumWidth, lumHeight, false));
			text = result?.Text ?? string.Empty;
			return !string.IsNullOrEmpty(text);
		}

		public void Dispose() => Stop();
	}
}
