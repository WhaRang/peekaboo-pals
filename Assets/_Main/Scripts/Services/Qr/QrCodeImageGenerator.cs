using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace _Main.Scripts.Services.Qr
{
	public interface IQrCodeImageGenerator
	{
		/// <summary>Renders <paramref name="payload"/> as a crisp black-on-white QR texture (point-filtered, with a quiet zone).</summary>
		Texture2D Generate(string payload, int pixelsPerModule = 8, int quietZoneModules = 2);
	}

	/// <summary>Offline QR encoder (ZXing.Net). Used by the host screen and the printable sheet exporter.</summary>
	[UsedImplicitly]
	public sealed class QrCodeImageGenerator : IQrCodeImageGenerator
	{
		public Texture2D Generate(string payload, int pixelsPerModule = 8, int quietZoneModules = 2)
		{
			var matrix = Encode(payload);
			int modules = matrix.Width;
			int size = (modules + quietZoneModules * 2) * pixelsPerModule;

			var pixels = new Color32[size * size];
			var white = new Color32(255, 255, 255, 255);
			var black = new Color32(20, 10, 30, 255);

			for (int y = 0; y < size; y++)
			{
				int moduleY = y / pixelsPerModule - quietZoneModules;
				for (int x = 0; x < size; x++)
				{
					int moduleX = x / pixelsPerModule - quietZoneModules;
					bool isDark = moduleX >= 0 && moduleY >= 0 && moduleX < modules && moduleY < modules && matrix[moduleX, moduleY];
					// Texture rows go bottom-up, QR rows top-down.
					pixels[(size - 1 - y) * size + x] = isDark ? black : white;
				}
			}

			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				name = $"QR {payload}"
			};
			texture.SetPixels32(pixels);
			texture.Apply(false, false);
			return texture;
		}

		public static BitMatrix Encode(string payload)
		{
			var hints = new Dictionary<EncodeHintType, object>
			{
				{ EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M },
				{ EncodeHintType.MARGIN, 0 },
				{ EncodeHintType.CHARACTER_SET, "UTF-8" }
			};

			return new QRCodeWriter().encode(payload, BarcodeFormat.QR_CODE, 0, 0, hints);
		}
	}
}
