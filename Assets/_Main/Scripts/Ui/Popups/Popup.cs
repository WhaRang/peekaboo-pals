using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Popups
{
	/// <summary>
	/// A modal card that scales in with a bounce over a dimmed backdrop. Content is added by the caller into
	/// <see cref="Content"/>; buttons stack at the bottom.
	/// </summary>
	public sealed class Popup : MonoBehaviour, IPointerClickHandler
	{
		private UiContext _ctx = null!;
		private CanvasGroup _group = null!;
		private RectTransform _panel = null!;
		private RectTransform _buttons = null!;
		private int _buttonCount;
		private bool _closing;

		public RectTransform Panel => _panel;
		public RectTransform Content { get; private set; } = null!;
		public bool CloseOnBackdrop { get; set; }
		public event Action? Closed;
		public bool IsOpen => this != null && !_closing;

		/// <param name="frameless">No panel, just content over the dimmed backdrop (used for the card-first inspect view).</param>
		public static Popup Create(UiContext ctx, Vector2 panelSize, Color? accent = null, bool frameless = false)
		{
			var root = UiBuilder.Rect("Popup", ctx.PopupLayer);
			root.Stretch();

			var backdrop = root.gameObject.AddComponent<Image>();
			backdrop.color = ctx.Palette.NightDeep.WithAlpha(0.72f);
			backdrop.raycastTarget = true;

			var popup = root.gameObject.AddComponent<Popup>();
			popup._ctx = ctx;
			popup._group = root.Group();

			var color = accent ?? ctx.Palette.Purple;

			var glow = UiBuilder.Image(root, "Glow", ctx, UiSprite.SoftGlow, color.WithAlpha(0.45f));
			glow.rectTransform.Place(new Vector2(0.5f, 0.5f), panelSize * 1.5f);

			popup._panel = UiBuilder.Rect("Panel", root);
			popup._panel.Place(new Vector2(0.5f, 0.5f), panelSize);

			if (frameless)
			{
				backdrop.color = ctx.Palette.NightDeep.WithAlpha(0.93f);
				glow.color = color.WithAlpha(0.18f);
				popup.Content = UiBuilder.Rect("Content", popup._panel);
				popup.Content.Stretch();
				popup._buttons = UiBuilder.Rect("Buttons", popup._panel);
				popup._buttons.StretchX(0f, 150f, 0f, 48f, 0f);
				popup.Open();
				return popup;
			}

			var shadow = UiBuilder.Image(popup._panel, "Shadow", ctx, UiSprite.RoundedRect, ctx.Palette.Ink.WithAlpha(0.8f));
			shadow.rectTransform.Stretch(0f, 0f, 18f, -18f);
			var border = UiBuilder.Image(popup._panel, "Border", ctx, UiSprite.RoundedRect, color);
			border.rectTransform.Stretch();
			var face = UiBuilder.Image(popup._panel, "Face", ctx, UiSprite.RoundedRect, ctx.Palette.Night);
			face.rectTransform.Stretch(12f, 12f, 12f, 12f);
			var sheen = UiBuilder.Image(face.rectTransform, "Sheen", ctx, UiSprite.GradientVertical, color.WithAlpha(0.35f));
			sheen.rectTransform.Stretch(0f, 0f, 0f, panelSize.y * 0.45f);

			popup.Content = UiBuilder.Rect("Content", popup._panel);
			popup.Content.Stretch(40f, 40f, 40f, 200f);

			popup._buttons = UiBuilder.Rect("Buttons", popup._panel);
			popup._buttons.StretchX(0f, 150f, 50f, 48f, 0f);

			popup.Open();
			return popup;
		}

		public TextMeshProUGUI AddTitle(string text, float y, float size = 78f, Color? color = null)
		{
			var label = UiBuilder.Text(Content, "Title", _ctx, text, size, TextStyle.Title, color ?? _ctx.Palette.Cream);
			label.rectTransform.StretchX(1f, size * 2.4f, -y, 0f, 1f);
			label.AutoSize(size * 0.55f);
			return label;
		}

		public TextMeshProUGUI AddBody(string text, float y, float height = 180f, float size = 46f)
		{
			var label = UiBuilder.Text(Content, "Body", _ctx, text, size, TextStyle.Body, _ctx.Palette.Cream.WithAlpha(0.9f));
			label.rectTransform.StretchX(1f, height, -y, 10f, 1f);
			label.AutoSize(size * 0.6f);
			return label;
		}

		/// <summary>Adds a button to the bottom row; buttons share the width evenly.</summary>
		public ChunkyButton AddButton(string label, Color color, Action onClick, bool closes = true)
		{
			var button = ChunkyButton.Create(_buttons, _ctx, $"Button{_buttonCount}", new Vector2(400f, 140f), color, label, null, 60f);
			_buttonCount++;
			button.Clicked = () =>
			{
				if (_closing)
					return;
				onClick();
				if (closes)
					Close();
			};
			LayoutButtons();
			return button;
		}

		private void LayoutButtons()
		{
			int count = _buttons.childCount;
			float width = _panel.sizeDelta.x - 96f;
			float spacing = 28f;
			float each = (width - spacing * (count - 1)) / count;
			for (int i = 0; i < count; i++)
			{
				var child = (RectTransform)_buttons.GetChild(i);
				child.Place(new Vector2(0f, 0.5f), new Vector2(each, 140f), new Vector2(each * 0.5f + i * (each + spacing), 0f));
			}
		}

		private void Open()
		{
			_ctx.Audio.Play(GameSound.PopupOpen);
			_group.alpha = 0f;
			_group.FadeTo(1f, 0.18f);

			if (_ctx.ReducedMotion)
				return;

			_panel.localScale = Vector3.one * 0.55f;
			_panel.ScaleTo(1f, 0.45f, Ease.OutBackStrong);
		}

		public void Close()
		{
			if (_closing)
				return;

			_closing = true;
			_group.blocksRaycasts = false;
			_ctx.Audio.Play(GameSound.PopupClose);
			_panel.ScaleTo(0.85f, 0.16f, Ease.InBack);
			_group.FadeTo(0f, 0.18f).OnComplete(() =>
			{
				Closed?.Invoke();
				Destroy(gameObject);
			});
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (CloseOnBackdrop && eventData.pointerCurrentRaycast.gameObject == gameObject)
				Close();
		}
	}
}
