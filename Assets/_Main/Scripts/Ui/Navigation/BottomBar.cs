using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;

namespace _Main.Scripts.Ui.Navigation
{
	public enum MainTab
	{
		Results = 0,
		Collection = 1,
		Scanner = 2
	}

	/// <summary>Three chunky slots: Results (left), Collection (center, home) and Scanner (right). The active one pops up.</summary>
	public sealed class BottomBar : MonoBehaviour
	{
		public const float Height = 260f;

		private UiContext _ctx = null!;
		private readonly ChunkyButton[] _buttons = new ChunkyButton[3];
		private readonly Color[] _colors = new Color[3];
		private readonly Vector2[] _restPositions = new Vector2[3];

		public Action<MainTab>? TabSelected;

		public static BottomBar Create(Transform parent, UiContext ctx)
		{
			var root = UiBuilder.Rect("BottomBar", parent);
			root.StretchX(0f, Height, 0f, 0f, 0f);

			var bar = root.gameObject.AddComponent<BottomBar>();
			bar._ctx = ctx;
			bar.Build();
			return bar;
		}

		private void Build()
		{
			var palette = _ctx.Palette;
			var rect = (RectTransform)transform;

			var glow = UiBuilder.Image(rect, "Glow", _ctx, UiSprite.SoftGlow, palette.Purple.WithAlpha(0.5f));
			glow.rectTransform.StretchX(1f, 220f, 0f, -200f);

			var panel = UiBuilder.Image(rect, "Panel", _ctx, UiSprite.RoundedRect, palette.Ink.WithAlpha(0.94f), raycast: true);
			panel.rectTransform.Stretch(20f, 20f, 40f, -60f);
			var rim = UiBuilder.Image(panel.rectTransform, "Rim", _ctx, UiSprite.Pill, palette.Purple.WithAlpha(0.8f));
			rim.rectTransform.StretchX(1f, 10f, -6f, 60f, 1f);

			_colors[(int)MainTab.Results] = palette.Pumpkin;
			_colors[(int)MainTab.Collection] = palette.Purple.Lighten(0.1f);
			_colors[(int)MainTab.Scanner] = palette.Slime.Darken(0.25f);

			CreateSlot(MainTab.Results, "Results", UiSprite.IconResults, new Vector2(-350f, 0f));
			CreateSlot(MainTab.Collection, "Cards", UiSprite.IconCollection, new Vector2(0f, 0f));
			CreateSlot(MainTab.Scanner, "Scan", UiSprite.IconScan, new Vector2(350f, 0f));
		}

		private void CreateSlot(MainTab tab, string label, UiSprite icon, Vector2 position)
		{
			int index = (int)tab;
			var button = ChunkyButton.Create(transform, _ctx, tab.ToString(), new Vector2(250f, 170f), _colors[index], null, icon);
			button.Root().Place(new Vector2(0.5f, 0.5f), new Vector2(250f, 170f), position + new Vector2(0f, 20f));
			button.ClickSound = GameSound.TabSwitch;
			button.Clicked = () => TabSelected?.Invoke(tab);
			if (button.Icon != null)
				button.Icon.rectTransform.sizeDelta = new Vector2(124f, 124f);

			_buttons[index] = button;
			_restPositions[index] = button.Root().anchoredPosition;
		}

		public void SetActive(MainTab active, bool animate)
		{
			for (int i = 0; i < _buttons.Length; i++)
			{
				bool isActive = i == (int)active;
				var rect = _buttons[i].Root();
				var targetPosition = _restPositions[i] + (isActive ? new Vector2(0f, 34f) : Vector2.zero);
				float targetScale = isActive ? 1.14f : 0.9f;
				var color = isActive ? _colors[i].Lighten(0.12f) : _colors[i].Darken(0.35f);

				_buttons[i].SetColor(color);
				if (_buttons[i].Icon != null)
					_buttons[i].Icon!.color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.75f);

				if (animate && !_ctx.ReducedMotion)
				{
					rect.MoveTo(targetPosition, 0.35f, Ease.OutBackStrong);
					rect.ScaleTo(targetScale, 0.35f, Ease.OutBackStrong);
				}
				else
				{
					rect.anchoredPosition = targetPosition;
					rect.localScale = new Vector3(targetScale, targetScale, 1f);
				}
			}
		}

		public void SetInteractable(bool interactable)
		{
			foreach (var button in _buttons)
				button.Interactable = interactable;
		}
	}
}
