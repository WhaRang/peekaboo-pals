using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using ZXing;

namespace _Main.Scripts.Services.Qr
{
	/// <summary>
	/// Turns camera frames into QR payloads without blocking the main thread. One frame is in flight at a time:
	/// the scanner submits only when <see cref="IsBusy"/> is false, and polls results with <see cref="TryTake"/>.
	/// </summary>
	public interface IQrFrameDecoder
	{
		bool IsBusy { get; }

		/// <param name="frame">Pixels in Unity order (bottom row first), as returned by GetPixels32.</param>
		void Submit(Color32[] frame, int width, int height);

		bool TryTake(out string payload);
	}

	/// <summary>
	/// Shared frame preparation. Most attempts use a fast pass on the centre of the frame (where the on-screen scan
	/// window is), downsampled; every few attempts a thorough full-frame pass catches small or off-centre codes.
	/// </summary>
	public abstract class QrFrameDecoderBase : IQrFrameDecoder
	{
		protected const float FastCrop = 0.8f;
		protected const int FastMaxDimension = 640;
		protected const int ThoroughEvery = 4;
		protected const int ThoroughMaxDimension = 1024;

		private int _attempt;

		public abstract bool IsBusy { get; }
		public abstract bool TryTake(out string payload);

		public void Submit(Color32[] frame, int width, int height)
		{
			bool thorough = ++_attempt % ThoroughEvery == 0;
			Submit(frame, width, height, thorough);
		}

		protected abstract void Submit(Color32[] frame, int width, int height, bool thorough);
	}

	/// <summary>Native builds (Android, editor): ZXing.Net on the thread pool.</summary>
	[UsedImplicitly]
	public sealed class ThreadedZxingQrDecoder : QrFrameDecoderBase
	{
		private readonly BarcodeReaderGeneric _fastReader = QrCameraScanner.CreateReader(thorough: false);
		private readonly BarcodeReaderGeneric _thoroughReader = QrCameraScanner.CreateReader(thorough: true);
		private byte[]? _luminance;
		private Task? _task;
		private volatile string? _decoded;

		public override bool IsBusy => _task is { IsCompleted: false };

		protected override void Submit(Color32[] frame, int width, int height, bool thorough)
		{
			_task = Task.Run(() =>
			{
				try
				{
					var reader = thorough ? _thoroughReader : _fastReader;
					float crop = thorough ? 1f : FastCrop;
					int maxDimension = thorough ? ThoroughMaxDimension : FastMaxDimension;
					if (QrCameraScanner.TryDecode(reader, frame, width, height, ref _luminance, out var text, crop, maxDimension))
						_decoded = text;
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[{nameof(ThreadedZxingQrDecoder)}] Decode failed: {e.Message}");
				}
			});
		}

		public override bool TryTake(out string payload)
		{
			var decoded = _decoded;
			_decoded = null;
			payload = decoded ?? string.Empty;
			return decoded != null;
		}
	}
}
