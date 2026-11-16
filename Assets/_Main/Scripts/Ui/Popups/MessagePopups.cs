using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Qr;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Flow;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using System.Collections;
using UnityEngine;

namespace _Main.Scripts.Ui.Popups
{
	/// <summary>The calmer popups of the scan flow plus generic confirm dialogs.</summary>
	public static class MessagePopups
	{
		/// <summary>Caught again too soon: calm popup with the card, its best float and a countdown. No points, no confetti.</summary>
		public static Popup Resting(UiContext ctx, CharacterDefinition character, float bestFloat, int secondsLeft, Action onClosed)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(880f, 1160f), palette.PurpleLight);

			var card = AddCard(ctx, popup, character, bestFloat);
			Tweens.Delay(0.2f, () =>
			{
				if (card != null)
					card.Body.Wobble(7f, 0.9f, 3);
			});

			popup.AddTitle($"{character.DisplayName} is resting!", 470f, 70f);
			var body = popup.AddBody(Countdown(secondsLeft), 640f, 190f);
			var host = popup.gameObject.AddComponent<CountdownText>();
			host.Setup(body, secondsLeft, Countdown);
			popup.AddButton("Keep hunting", palette.Purple, () => { });
			popup.Closed += onClosed;

			ctx.Audio.Play(GameSound.Duplicate);
			ctx.Haptics.Play(HapticStrength.Light);
			return popup;
		}

		private static string Countdown(int seconds)
		{
			var span = TimeSpan.FromSeconds(Mathf.Max(0, seconds));
			return seconds > 0
				? $"Catch it again in {(int)span.TotalMinutes}:{span.Seconds:00} for a shot at a rarer float."
				: "Ready! Scan it again for a new float roll.";
		}

		/// <summary>Caught again after the cooldown, but the new float doesn't beat the best one: roll it anyway, it's fun.</summary>
		public static Popup NoImprovement(UiContext ctx, CharacterDefinition character, float rolled, float bestFloat, Action onClosed)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(900f, 1260f), palette.PurpleLight);
			AddCard(ctx, popup, character, bestFloat);
			popup.AddTitle($"Another {character.DisplayName}!", 440f, 70f);

			var badge = FloatReveal.Create(ctx, popup.Content, new Vector2(0f, 0f), 480f);
			badge.Root.anchorMin = badge.Root.anchorMax = new Vector2(0.5f, 1f);
			badge.Root.anchoredPosition = new Vector2(0f, -680f);

			var grade = ctx.Pack.GetFloatGrade(bestFloat);
			var verdict = popup.AddBody($"Not rarer than your best: {FloatStyle.Short(bestFloat)} · {grade.Name}. Keep hunting!", 830f, 130f, 40f);
			verdict.alpha = 0f;

			ctx.Audio.Play(GameSound.CardAppear);
			ctx.CoroutineHost.StartCoroutine(RollThenVerdict(ctx, badge, rolled, verdict));
			popup.AddButton("Keep hunting", palette.Purple, () => { });
			popup.Closed += onClosed;
			return popup;
		}

		private static IEnumerator RollThenVerdict(UiContext ctx, FloatReveal.Badge badge, float rolled, TextMeshProUGUI verdict)
		{
			yield return FloatReveal.Play(ctx, badge, rolled);
			if (verdict != null)
				verdict.FadeTo(1f, 0.3f);
		}

		private static CardView AddCard(UiContext ctx, Popup popup, CharacterDefinition character, float bestFloat)
		{
			var card = CardView.Create(popup.Content, ctx, character, new Vector2(290f, 394f));
			card.Root.Place(new Vector2(0.5f, 1f), new Vector2(290f, 394f), new Vector2(0f, -230f));
			card.SetFloat(bestFloat);
			card.SetCollected(true);
			card.Glow.gameObject.SetActive(false);
			card.Body.localRotation = Quaternion.Euler(0f, 0f, -4f);
			return card;
		}

		/// <param name="offline">For <see cref="QrRejectionReason.UnknownPack"/>: the website couldn't be reached to fetch it.</param>
		public static Popup Invalid(UiContext ctx, QrRejectionReason reason, Action onClosed, bool offline = false)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(880f, 960f), palette.Candy);

			var ghost = UiBuilder.Image(popup.Content, "Ghost", ctx, UiSprite.IconPal, Color.white);
			ghost.preserveAspect = true;
			ghost.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(240f, 240f), new Vector2(0f, -140f));
			if (!ctx.ReducedMotion)
			{
				var ghostRect = ghost.rectTransform;
				Tweens.Run(ghostRect, 0.9f, k => ghostRect.anchoredPosition = new Vector2(Mathf.Sin(k * Mathf.PI * 2f) * 18f, -140f + Mathf.Sin(k * Mathf.PI * 4f) * 10f), Ease.Linear, TweenChannel.Position)
					.SetLoops(-1);
				ghostRect.Wobble(14f, 0.8f, 3);
			}

			if (reason == QrRejectionReason.UnknownPack && offline)
			{
				popup.AddTitle("A pal from a new collection!", 300f, 66f);
				popup.AddBody("Your phone needs the internet once to download this collection. Connect, then scan it again!", 470f, 190f);
			}
			else if (reason == QrRejectionReason.UnknownPack)
			{
				popup.AddTitle("A pal from the future!", 300f, 74f);
				popup.AddBody("This code belongs to a collection that hasn't been released yet. Try again a bit later!", 470f, 190f);
			}
			else
			{
				popup.AddTitle("That's not one of our codes!", 300f, 74f);
				popup.AddBody("Only the codes hidden for the party count. Try another one!", 470f, 160f);
			}
			popup.AddButton("Try again", palette.Candy.Darken(0.15f), () => { });
			popup.Closed += onClosed;

			ctx.Audio.Play(GameSound.Error);
			ctx.Haptics.Play(HapticStrength.Medium);
			return popup;
		}

		public static Popup Confirm(UiContext ctx, string title, string body, string confirmLabel, Color confirmColor, Action onConfirm)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(900f, 820f), confirmColor);
			popup.AddTitle(title, 60f, 76f);
			popup.AddBody(body, 230f, 260f);
			popup.AddButton("Cancel", palette.Purple, () => { });
			popup.AddButton(confirmLabel, confirmColor, onConfirm);
			popup.CloseOnBackdrop = true;
			return popup;
		}
	}

	/// <summary>Ticks a "back in m:ss" label down once per second.</summary>
	public sealed class CountdownText : MonoBehaviour
	{
		private TextMeshProUGUI _label = null!;
		private Func<int, string> _format = null!;
		private float _endTime;
		private int _shown = -1;

		public void Setup(TextMeshProUGUI label, int seconds, Func<int, string> format)
		{
			_label = label;
			_format = format;
			_endTime = Time.unscaledTime + seconds;
		}

		private void Update()
		{
			int left = Mathf.CeilToInt(_endTime - Time.unscaledTime);
			if (left == _shown || _label == null)
				return;
			_shown = left;
			_label.text = _format(left);
		}
	}
}
