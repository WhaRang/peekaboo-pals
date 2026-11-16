using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Widgets
{
	/// <summary>
	/// Big, bouncy casual-game button: squashes down on press (the face sinks onto its shadow), springs back with an
	/// overshoot on release, plays a click and a light haptic. Optional long-press (used by the hidden host tools).
	/// </summary>
	public sealed class ChunkyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
	{
		private const float PressDepth = 10f;

		private UiContext _ctx = null!;
		private RectTransform _face = null!;
		private Vector2 _faceRestPosition;
		private bool _isPressed;
		private float _pressTime;
		private bool _longPressFired;

		public Action? Clicked;
		public Action? LongPressed;
		public float LongPressSeconds = 1.2f;
		public bool Interactable = true;
		public GameSound ClickSound = GameSound.ButtonPress;

		public RectTransform Face => _face;
		public Image FaceImage { get; private set; } = null!;
		public TextMeshProUGUI? Label { get; private set; }
		public Image? Icon { get; private set; }

		public static ChunkyButton Create(Transform parent, UiContext ctx, string name, Vector2 size, Color color,
			string? label = null, UiSprite? icon = null, float fontSize = 64f, UiSprite shape = UiSprite.RoundedRect)
		{
			var root = UiBuilder.Rect(name, parent);
			root.sizeDelta = size;

			// Transparent hit area that never moves, so the press doesn't jitter when the face squashes.
			var hit = root.gameObject.AddComponent<Image>();
			hit.color = new Color(0f, 0f, 0f, 0f);
			hit.raycastTarget = true;

			var shadow = UiBuilder.Image(root, "Shadow", ctx, shape, color.Darken(0.55f));
			shadow.rectTransform.Stretch(0f, 0f, PressDepth, -PressDepth * 0.2f);

			var faceImage = UiBuilder.Image(root, "Face", ctx, shape, color);
			faceImage.rectTransform.Stretch(0f, 0f, 0f, PressDepth);

			var gloss = UiBuilder.Image(faceImage.rectTransform, "Gloss", ctx, shape, new Color(1f, 1f, 1f, 0.22f));
			gloss.rectTransform.anchorMin = new Vector2(0f, 0.52f);
			gloss.rectTransform.anchorMax = Vector2.one;
			gloss.rectTransform.offsetMin = new Vector2(14f, 0f);
			gloss.rectTransform.offsetMax = new Vector2(-14f, -8f);

			var button = root.gameObject.AddComponent<ChunkyButton>();
			button._ctx = ctx;
			button._face = faceImage.rectTransform;
			button._faceRestPosition = button._face.anchoredPosition;
			button.FaceImage = faceImage;

			if (icon.HasValue)
			{
				var iconImage = UiBuilder.Image(faceImage.rectTransform, "Icon", ctx, icon.Value, Color.white);
				iconImage.preserveAspect = true;
				float iconSize = Mathf.Min(size.x, size.y - PressDepth) * (label == null ? 0.62f : 0.5f);
				if (label == null)
					iconImage.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(iconSize, iconSize));
				else
					iconImage.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(iconSize, iconSize), new Vector2(iconSize * 0.5f + 34f, 0f));
				button.Icon = iconImage;
			}

			if (label != null)
			{
				var text = UiBuilder.Text(faceImage.rectTransform, "Label", ctx, label, fontSize, TextStyle.Title, ctx.Palette.Cream);
				// Label starts after the icon's right edge, so the icon never sits on the words.
				float iconWidth = Mathf.Min(size.x, size.y - PressDepth) * 0.5f;
				float left = icon.HasValue ? 34f + iconWidth + 22f : 24f;
				text.rectTransform.Stretch(left, 24f, 6f, 6f);
				text.textWrappingMode = TextWrappingModes.NoWrap;
				text.AutoSize(fontSize * 0.5f);
				button.Label = text;
			}

			return button;
		}

		public void SetColor(Color color)
		{
			FaceImage.color = color;
			var shadow = transform.Find("Shadow")?.GetComponent<Image>();
			if (shadow != null)
				shadow.color = color.Darken(0.55f);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!Interactable)
				return;

			_isPressed = true;
			_longPressFired = false;
			_pressTime = 0f;

			_face.MoveTo(_faceRestPosition + new Vector2(0f, -PressDepth), 0.06f, Ease.OutQuad);
			_face.ScaleTo(new Vector3(1.06f, 0.9f, 1f), 0.08f, Ease.OutQuad);
			_ctx.Haptics.Play(HapticStrength.Light);
		}

		public void OnPointerUp(PointerEventData eventData) => Release();

		public void OnPointerExit(PointerEventData eventData)
		{
			if (_isPressed)
				Release();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!Interactable || _longPressFired || eventData.dragging)
				return;

			_ctx.Audio.Play(ClickSound, UnityEngine.Random.Range(0.95f, 1.08f));
			Clicked?.Invoke();
		}

		private void Release()
		{
			if (!_isPressed)
				return;

			_isPressed = false;
			_face.MoveTo(_faceRestPosition, 0.22f, Ease.OutBackStrong);
			_face.localScale = new Vector3(0.94f, 1.08f, 1f);
			_face.ScaleTo(Vector3.one, 0.35f, Ease.OutElastic);
		}

		private void Update()
		{
			if (!_isPressed || LongPressed == null || _longPressFired)
				return;

			_pressTime += Time.unscaledDeltaTime;
			if (_pressTime < LongPressSeconds)
				return;

			_longPressFired = true;
			Release();
			_ctx.Haptics.Play(HapticStrength.Heavy);
			LongPressed.Invoke();
		}

		/// <summary>Gentle "breathing" so the main call-to-action draws the eye.</summary>
		public void StartIdlePulse(float amount = 0.04f, float period = 1.4f)
		{
			if (_ctx.ReducedMotion)
				return;

			Tweens.Run(transform, period * 0.5f, k =>
			{
				float s = 1f + amount * k;
				transform.localScale = new Vector3(s, s, 1f);
			}, Ease.InOutSine).SetLoops(-1, true);
		}
	}
}
