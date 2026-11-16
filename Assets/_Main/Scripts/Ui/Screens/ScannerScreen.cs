using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Permissions;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Popups;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Screens
{
	/// <summary>
	/// Camera view with a spooky scanning frame and an animated scan line. Asks for the camera permission with an
	/// explanation first, handles denial, and offers manual code entry in editor/debug builds.
	/// </summary>
	public sealed class ScannerScreen : ScreenBase
	{
		private const float FrameSize = 720f;
		private const float SamePayloadCooldown = 3f;

		private RawImage _cameraImage = null!;
		private RectTransform _frame = null!;
		private RectTransform _scanLine = null!;
		private readonly Image[] _corners = new Image[4];
		private RectTransform _overlay = null!;
		private RectTransform _permissionPanel = null!;
		private TextMeshProUGUI _permissionBody = null!;
		private ChunkyButton _permissionButton = null!;
		private TextMeshProUGUI _hint = null!;
		private TextMeshProUGUI _status = null!;

		private bool _suspended;
		// The camera was started; the image shows once it has a texture (the browser camera gets one with its first frame).
		private bool _cameraActive;
		private Vector4 _lastFit;
		private string _lastPayload = string.Empty;
		private float _lastPayloadTime = -100f;

		/// <summary>Raised with the raw text of a freshly decoded code.</summary>
		public Action<string>? PayloadScanned;

		/// <summary>While suspended (a popup or reveal is running) decoded codes are ignored.</summary>
		public bool Suspended
		{
			get => _suspended;
			set
			{
				_suspended = value;
				if (!value)
					_lastPayloadTime = Time.unscaledTime;
			}
		}

		protected override void Build()
		{
			var palette = Ctx.Palette;

			var cameraBackground = UiBuilder.Image(Rect, "CameraBackground", (Sprite?)null, palette.NightDeep);
			cameraBackground.rectTransform.Stretch();
			cameraBackground.gameObject.AddComponent<RectMask2D>();

			var cameraRect = UiBuilder.Rect("Camera", cameraBackground.rectTransform);
			cameraRect.Place(new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f));
			_cameraImage = cameraRect.gameObject.AddComponent<RawImage>();
			_cameraImage.raycastTarget = false;
			_cameraImage.enabled = false;

			// Tap anywhere on the camera view to refocus.
			var tapCatcher = cameraBackground.gameObject.AddComponent<TapArea>();
			cameraBackground.raycastTarget = true;
			tapCatcher.Tapped = OnCameraTapped;

			_overlay = UiBuilder.Rect("Overlay", Rect);
			_overlay.Stretch();

			_frame = UiBuilder.Rect("Frame", _overlay);
			_frame.Place(new Vector2(0.5f, 0.55f), new Vector2(FrameSize, FrameSize));
			BuildShade(palette);

			for (int i = 0; i < 4; i++)
			{
				var corner = UiBuilder.Image(_frame, $"Corner{i}", Ctx, UiSprite.ScanCorner, palette.Slime);
				var anchor = new Vector2(i % 2, i / 2);
				var inward = new Vector2(i % 2 == 0 ? 85f : -85f, i / 2 == 0 ? 85f : -85f);
				corner.rectTransform.Place(anchor, new Vector2(170f, 170f), inward);
				// Sprite is the bottom-left corner; rotate for the others.
				float angle = i switch { 0 => 0f, 1 => 90f, 3 => 180f, _ => 270f };
				corner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
				_corners[i] = corner;
			}

			var line = UiBuilder.Image(_frame, "ScanLine", Ctx, UiSprite.SoftGlow, palette.Slime.WithAlpha(0.9f));
			_scanLine = line.rectTransform;
			_scanLine.Place(new Vector2(0.5f, 0.5f), new Vector2(FrameSize * 1.05f, 60f));
			var core = UiBuilder.Image(_scanLine, "Core", Ctx, UiSprite.Pill, Color.white.WithAlpha(0.85f));
			core.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(FrameSize * 0.92f, 8f));

			var title = UiBuilder.Text(_overlay, "Title", Ctx, "Find a hidden code!", 82f, TextStyle.Title, palette.Cream);
			title.rectTransform.StretchX(1f, 140f, -80f, 30f, 1f);
			title.AutoSize(50f);

			_hint = UiBuilder.Text(_overlay, "Hint", Ctx, "Point at a hidden QR code · tap to focus", 46f, TextStyle.BodyOutline, palette.Cream);
			_hint.AutoSize(30f);
			_hint.rectTransform.StretchX(0f, 90f, 330f, 40f, 0f);

			_status = UiBuilder.Text(_overlay, "Status", Ctx, "", 44f, TextStyle.BodyOutline, palette.Muted);
			_status.rectTransform.StretchX(0f, 80f, 250f, 40f, 0f);

			if (Application.isEditor || Debug.isDebugBuild)
			{
				var typeButton = ChunkyButton.Create(_overlay, Ctx, "TypeCode", new Vector2(420f, 120f), palette.Purple, "Type code", UiSprite.IconKeyboard, 48f);
				typeButton.Root().Place(new Vector2(0.5f, 0f), new Vector2(420f, 120f), new Vector2(0f, 120f));
				typeButton.Clicked = () => ManualCodePopup.Show(Ctx, payload => PayloadScanned?.Invoke(payload));
			}

			BuildPermissionPanel(palette);
		}

		private void BuildShade(UiPalette palette)
		{
			var shade = palette.NightDeep.WithAlpha(0.62f);
			// Four rects around the frame leave a clear window over the camera.
			var top = UiBuilder.Image(_overlay, "ShadeTop", (Sprite?)null, shade);
			top.rectTransform.anchorMin = new Vector2(0f, 0.55f);
			top.rectTransform.anchorMax = Vector2.one;
			top.rectTransform.offsetMin = new Vector2(0f, FrameSize * 0.5f);
			top.rectTransform.offsetMax = Vector2.zero;

			var bottom = UiBuilder.Image(_overlay, "ShadeBottom", (Sprite?)null, shade);
			bottom.rectTransform.anchorMin = Vector2.zero;
			bottom.rectTransform.anchorMax = new Vector2(1f, 0.55f);
			bottom.rectTransform.offsetMin = Vector2.zero;
			bottom.rectTransform.offsetMax = new Vector2(0f, -FrameSize * 0.5f);

			var left = UiBuilder.Image(_overlay, "ShadeLeft", (Sprite?)null, shade);
			left.rectTransform.anchorMin = new Vector2(0f, 0.55f);
			left.rectTransform.anchorMax = new Vector2(0.5f, 0.55f);
			left.rectTransform.offsetMin = new Vector2(0f, -FrameSize * 0.5f);
			left.rectTransform.offsetMax = new Vector2(-FrameSize * 0.5f, FrameSize * 0.5f);

			var right = UiBuilder.Image(_overlay, "ShadeRight", (Sprite?)null, shade);
			right.rectTransform.anchorMin = new Vector2(0.5f, 0.55f);
			right.rectTransform.anchorMax = new Vector2(1f, 0.55f);
			right.rectTransform.offsetMin = new Vector2(FrameSize * 0.5f, -FrameSize * 0.5f);
			right.rectTransform.offsetMax = new Vector2(0f, FrameSize * 0.5f);

			foreach (var image in new[] { top, bottom, left, right })
				image.transform.SetAsFirstSibling();
		}

		private void BuildPermissionPanel(UiPalette palette)
		{
			var panel = UiBuilder.Image(Rect, "Permission", Ctx, UiSprite.RoundedRect, palette.Night.WithAlpha(0.97f), raycast: true);
			_permissionPanel = panel.rectTransform;
			_permissionPanel.Place(new Vector2(0.5f, 0.55f), new Vector2(900f, 1040f));

			var icon = UiBuilder.Image(_permissionPanel, "Icon", Ctx, UiSprite.IconCamera, Color.white);
			icon.preserveAspect = true;
			icon.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(230f, 230f), new Vector2(0f, -170f));

			var title = UiBuilder.Text(_permissionPanel, "Title", Ctx, "Sharp eyes needed!", 74f, TextStyle.Title, palette.Cream);
			title.rectTransform.StretchX(1f, 110f, -310f, 40f, 1f);
			title.AutoSize(50f);

			_permissionBody = UiBuilder.Text(_permissionPanel, "Body", Ctx, "", 44f, TextStyle.Body, palette.Cream.WithAlpha(0.9f));
			_permissionBody.rectTransform.StretchX(1f, 330f, -430f, 60f, 1f);
			_permissionBody.AutoSize(30f);

			_permissionButton = ChunkyButton.Create(_permissionPanel, Ctx, "Allow", new Vector2(600f, 150f), palette.Slime.Darken(0.25f), "Allow camera", UiSprite.IconCamera, 58f);
			_permissionButton.Root().Place(new Vector2(0.5f, 0f), new Vector2(600f, 150f), new Vector2(0f, 130f));
			_permissionButton.StartIdlePulse();

			_permissionPanel.gameObject.SetActive(false);
		}

		protected override void OnShow()
		{
			_lastPayload = string.Empty;
			StartScanning();
			AnimateFrameIn();
		}

		protected override void OnHide()
		{
			_lastFit = Vector4.zero;
			Ctx.Scanner.Stop();
			_cameraActive = false;
			_cameraImage.enabled = false;
		}

		private void OnApplicationPause(bool paused)
		{
			if (!IsShown)
				return;

			if (paused)
			{
				Ctx.Scanner.Stop();
				_cameraActive = false;
				_cameraImage.enabled = false;
			}
			else
			{
				StartScanning();
			}
		}

		private void StartScanning()
		{
			var state = Ctx.CameraPermission.State;
			if (state == CameraPermissionState.Granted)
			{
				_permissionPanel.gameObject.SetActive(false);
				StartCamera();
				return;
			}

			ShowPermissionPanel(state);
		}

		private void ShowPermissionPanel(CameraPermissionState state)
		{
			_permissionPanel.gameObject.SetActive(true);

			if (state == CameraPermissionState.PermanentlyDenied)
			{
#if UNITY_WEBGL && !UNITY_EDITOR
				_permissionBody.text = "The browser blocked the camera.\niPhone: tap aA in the address bar → Website Settings → Camera → Allow, then reload the page.";
				_permissionButton.Label!.text = "Try again";
#else
				_permissionBody.text = "Camera access is switched off for this game.\nOpen Settings → Permissions → Camera to turn it back on, then come back here.";
				_permissionButton.Label!.text = "Open Settings";
#endif
				_permissionButton.Clicked = () => Ctx.CameraPermission.OpenAppSettings();
			}
			else
			{
				_permissionBody.text = "The camera is how you catch the pals hiding behind QR codes around the party.\nIt's only used while this screen is open. Nothing is recorded or sent anywhere.";
				_permissionButton.Label!.text = "Allow camera";
				_permissionButton.Clicked = () => Ctx.CameraPermission.Request(result =>
				{
					if (this == null || !IsShown)
						return;

					if (result == CameraPermissionState.Granted)
						StartScanning();
					else
						ShowPermissionPanel(result);
				});
			}

			_permissionPanel.localScale = Vector3.one * 0.8f;
			_permissionPanel.ScaleTo(1f, 0.4f, Ease.OutBackStrong);
		}

		private void StartCamera()
		{
			if (Ctx.Scanner.TryStart(out var error))
			{
				var texture = Ctx.Scanner.CameraTexture;
				_status.text = texture != null ? string.Empty : "Waking up the camera…";
				_cameraActive = true;
				_cameraImage.texture = texture;
				_cameraImage.enabled = texture != null;
				return;
			}

			_status.text = Application.isEditor || Debug.isDebugBuild ? $"{error} Use \"Type code\" to test." : error;
			_cameraActive = false;
			_cameraImage.enabled = false;
		}

		private void AnimateFrameIn()
		{
			if (Ctx.ReducedMotion)
				return;

			_frame.localScale = Vector3.one * 1.25f;
			_frame.ScaleTo(1f, 0.5f, Ease.OutBackStrong);
		}

		private void Update()
		{
			if (!IsShown)
				return;

			AnimateOverlay();
#if UNITY_WEBGL && !UNITY_EDITOR
			if (_cameraActive && !Ctx.Scanner.IsRunning)
				OnBrowserCameraLost();
#endif
			FitCameraImage();

			if (!Ctx.Scanner.IsRunning || !Ctx.Scanner.Tick(out var payload))
				return;

			if (_suspended)
				return;

			if (payload == _lastPayload && Time.unscaledTime - _lastPayloadTime < SamePayloadCooldown)
				return;

			_lastPayload = payload;
			_lastPayloadTime = Time.unscaledTime;
			OnCodeDetected(payload);
		}

#if UNITY_WEBGL && !UNITY_EDITOR
		/// <summary>The browser refused or dropped the camera (permission revoked, camera taken by another app).</summary>
		private void OnBrowserCameraLost()
		{
			Ctx.Scanner.Stop();
			_cameraActive = false;
			_cameraImage.enabled = false;

			var state = Ctx.CameraPermission.State;
			if (state != CameraPermissionState.Granted)
			{
				_status.text = string.Empty;
				ShowPermissionPanel(state);
				return;
			}

			_status.text = "The camera stopped. Close the scanner and open it again.";
		}
#endif

		private void OnCameraTapped()
		{
			if (!Ctx.Scanner.IsRunning)
				return;

			Ctx.Scanner.FocusCenter();
			Ctx.Haptics.Play(HapticStrength.Light);
			_frame.PunchScale(0.06f, 0.3f, 2);
			Ctx.FxParticles.EmitAtWorld(_frame.position, ParticlePresets.Ring(Ctx.Palette.Slime.WithAlpha(0.6f)), 1);
		}

		private void OnCodeDetected(string payload)
		{
			Ctx.Audio.Play(GameSound.ScanBeep);
			Ctx.Haptics.Play(HapticStrength.Medium);
			_frame.PunchScale(0.12f, 0.35f, 2);
			Ctx.FxParticles.EmitAtWorld(_frame.position, ParticlePresets.Ring(Ctx.Palette.Slime), 1);
			Ctx.ScreenFx.Flash(Ctx.Palette.Slime, 0.35f, 0.3f);
			PayloadScanned?.Invoke(payload);
		}

		private void AnimateOverlay()
		{
			float time = Time.unscaledTime;
			if (Ctx.ReducedMotion)
			{
				_scanLine.anchoredPosition = Vector2.zero;
				return;
			}

			_scanLine.anchoredPosition = new Vector2(0f, Mathf.Sin(time * 2.4f) * FrameSize * 0.44f);
			float pulse = 1f + Mathf.Sin(time * 4f) * 0.04f;
			for (int i = 0; i < _corners.Length; i++)
				_corners[i].rectTransform.localScale = new Vector3(pulse, pulse, 1f);
			_hint.alpha = 0.7f + 0.3f * Mathf.Sin(time * 3f);
		}

		/// <summary>Aspect-fills the camera into the screen, honoring the sensor rotation and mirroring.</summary>
		private void FitCameraImage()
		{
			var texture = Ctx.Scanner.CameraTexture;
			if (!_cameraActive || texture == null || texture.width < 64)
				return;

			if (_cameraImage.texture != texture || !_cameraImage.enabled)
			{
				_cameraImage.texture = texture;
				_cameraImage.enabled = true;
				_status.text = string.Empty;
				_lastFit = Vector4.zero;
			}

			int angle = Ctx.Scanner.RotationAngle;
			bool mirrored = Ctx.Scanner.VerticallyMirrored;
			var area = ((RectTransform)_cameraImage.rectTransform.parent).rect;
			// Only touch the layout when something changed; resizing every frame forces a canvas rebuild.
			var fitKey = new Vector4(texture.width, texture.height, angle + (mirrored ? 1000 : 0), area.width * 10000f + area.height);
			if (fitKey == _lastFit)
				return;
			_lastFit = fitKey;

			bool sideways = angle % 180 != 0;
			float camWidth = sideways ? texture.height : texture.width;
			float camHeight = sideways ? texture.width : texture.height;

			float scale = Mathf.Max(area.width / camWidth, area.height / camHeight);

			_cameraImage.rectTransform.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
			_cameraImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
			_cameraImage.uvRect = mirrored ? new UnityEngine.Rect(0f, 1f, 1f, -1f) : new UnityEngine.Rect(0f, 0f, 1f, 1f);
		}
	}
}
