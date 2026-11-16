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
	/// <summary>Labelled on/off switch with a sliding knob.</summary>
	public sealed class ToggleSwitch : MonoBehaviour, IPointerClickHandler
	{
		private UiContext _ctx = null!;
		private Image _track = null!;
		private RectTransform _knob = null!;
		private Func<bool> _get = null!;
		private Action<bool> _set = null!;

		public static ToggleSwitch Create(Transform parent, UiContext ctx, string label, Func<bool> get, Action<bool> set)
		{
			var palette = ctx.Palette;
			var root = UiBuilder.Rect($"Toggle {label}", parent);
			var hit = root.gameObject.AddComponent<Image>();
			hit.sprite = ctx.Sprite(UiSprite.RoundedRectSmall);
			hit.type = Image.Type.Sliced;
			hit.color = palette.Night.Lighten(0.06f);
			hit.raycastTarget = true;

			var text = UiBuilder.Text(root, "Label", ctx, label, 50f, TextStyle.TitleThin, palette.Cream, TextAlignmentOptions.Left);
			text.rectTransform.Stretch(40f, 240f, 0f, 0f);
			text.AutoSize(32f);

			var track = UiBuilder.Image(root, "Track", ctx, UiSprite.Pill, palette.Ink);
			track.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(170f, 84f), new Vector2(-120f, 0f));
			var knob = UiBuilder.Image(track.rectTransform, "Knob", ctx, UiSprite.Circle, palette.Cream);
			knob.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(68f, 68f));

			var toggle = root.gameObject.AddComponent<ToggleSwitch>();
			toggle._ctx = ctx;
			toggle._track = track;
			toggle._knob = knob.rectTransform;
			toggle._get = get;
			toggle._set = set;
			toggle.Apply(false);
			return toggle;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			_set(!_get());
			_ctx.Audio.Play(GameSound.Toggle);
			_ctx.Haptics.Play(HapticStrength.Light);
			Apply(true);
		}

		public void Apply(bool animate)
		{
			bool on = _get();
			var target = new Vector2(on ? 43f : -43f, 0f);
			var color = on ? _ctx.Palette.Slime.Darken(0.2f) : _ctx.Palette.Ink;
			if (animate)
			{
				_knob.MoveTo(target, 0.25f, Ease.OutBackStrong);
				_track.ColorTo(color, 0.2f);
				_knob.PunchScale(0.15f, 0.3f, 2);
			}
			else
			{
				_knob.anchoredPosition = target;
				_track.color = color;
			}
		}
	}
}
