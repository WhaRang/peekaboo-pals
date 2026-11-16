using System;
using System.Collections.Generic;
using System.Linq;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Popups;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Screens
{
	/// <summary>
	/// Hidden host tools (long-press the collection logo): what's in the collection on screen, player settings,
	/// "Reset this collection" and "Erase everything", each with a confirmation. The QR secrets never ship in the app,
	/// so printable tags come from the computer (Party > Packs > Export QR Sheets, see PACKS.md).
	/// </summary>
	public sealed class HostScreen : ScreenBase
	{
		private readonly List<ToggleSwitch> _toggles = new();
		private ScrollRect _scroll = null!;

		public Action? CloseRequested;
		public Action? ResetCollectionConfirmed;
		public Action? EraseEverythingConfirmed;

		protected override void Build()
		{
			var palette = Ctx.Palette;
			var pack = Ctx.Pack;

			var background = UiBuilder.Image(Rect, "Background", (Sprite?)null, palette.NightDeep, raycast: true);
			background.rectTransform.Stretch(-300f, -300f, -300f, -300f);
			var sheen = UiBuilder.Image(Rect, "Sheen", Ctx, UiSprite.GradientVertical, palette.Purple.WithAlpha(0.35f));
			sheen.rectTransform.StretchX(1f, 700f, 300f, -300f, 1f);

			var title = UiBuilder.Text(Rect, "Title", Ctx, "HOST TOOLS", 92f, TextStyle.Title, palette.Gold);
			title.rectTransform.StretchX(1f, 130f, -50f, 160f, 1f);

			var close = ChunkyButton.Create(Rect, Ctx, "Close", new Vector2(124f, 124f), palette.Danger, null, UiSprite.IconClose);
			close.Root().Place(new Vector2(1f, 1f), new Vector2(124f, 124f), new Vector2(-36f, -40f), new Vector2(1f, 1f));
			close.Clicked = () => CloseRequested?.Invoke();

			var viewport = UiBuilder.Rect("Viewport", Rect);
			viewport.Stretch(0f, 0f, 200f, 0f);
			viewport.gameObject.AddComponent<RectMask2D>();
			viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

			var content = UiBuilder.Rect("Content", viewport);
			content.anchorMin = new Vector2(0f, 1f);
			content.anchorMax = new Vector2(1f, 1f);
			content.pivot = new Vector2(0.5f, 1f);

			_scroll = viewport.gameObject.AddComponent<ScrollRect>();
			_scroll.viewport = viewport;
			_scroll.content = content;
			_scroll.horizontal = false;
			_scroll.scrollSensitivity = 30f;

			float y = -10f;

			// What's in this collection
			var card = UiBuilder.Image(content, "PackCard", Ctx, UiSprite.RoundedRect, Color.Lerp(palette.Night, pack.Theme.Accent, 0.22f));
			card.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(980f, 360f), new Vector2(0f, y), new Vector2(0.5f, 1f));
			var packTitle = UiBuilder.Text(card.rectTransform, "Title", Ctx, pack.Title.ToUpperInvariant(), 64f, TextStyle.Title, pack.Theme.Accent, TextAlignmentOptions.Left);
			packTitle.rectTransform.StretchX(1f, 90f, -30f, 50f, 1f);
			packTitle.AutoSize(40f);
			var counts = pack.Characters.GroupBy(c => c.Rarity).OrderBy(g => g.Key)
				.Select(g => $"{g.Count()} {pack.GetTier(g.Key).RarityLabel.ToLowerInvariant()}");
			var summary = UiBuilder.Text(card.rectTransform, "Summary", Ctx,
				$"{pack.Characters.Count} {pack.Texts.Friends}: {string.Join(", ", counts)}\nCollection id: <b>{pack.Id}</b>",
				40f, TextStyle.Body, palette.Cream, TextAlignmentOptions.TopLeft);
			summary.rectTransform.Stretch(50f, 50f, 130f, 30f);
			summary.AutoSize(28f);
			y -= 400f;

			var intro = UiBuilder.Text(content, "Intro", Ctx,
				"Hide one tag per character around the party.\nPrint the tags from your computer: in Unity, Party > Packs > Export QR Sheets (see PACKS.md). The codes are never stored in the app.",
				38f, TextStyle.Body, palette.Cream.WithAlpha(0.85f));
			intro.rectTransform.StretchX(1f, 220f, y, 50f, 1f);
			intro.AutoSize(26f);
			y -= 260f;

			var settingsTitle = UiBuilder.Text(content, "SettingsTitle", Ctx, "SETTINGS", 64f, TextStyle.Title, palette.Cream);
			settingsTitle.rectTransform.StretchX(1f, 90f, y, 40f, 1f);
			y -= 120f;

			AddToggle(content, "Sound", () => !Ctx.Settings.IsMuted, on => Ctx.Settings.IsMuted = !on, ref y);
			AddToggle(content, "Vibration", () => Ctx.Settings.IsHapticsEnabled, on => Ctx.Settings.IsHapticsEnabled = on, ref y);
			AddToggle(content, "Reduced motion", () => Ctx.Settings.ForceReducedMotion, on => Ctx.Settings.ForceReducedMotion = on, ref y);

			y -= 60f;
			var reset = ChunkyButton.Create(content, Ctx, "Reset", new Vector2(860f, 150f), palette.Danger, "Reset this collection", null, 56f);
			reset.Root().Place(new Vector2(0.5f, 1f), new Vector2(860f, 150f), new Vector2(0f, y), new Vector2(0.5f, 1f));
			reset.Clicked = () => MessagePopups.Confirm(Ctx, $"Reset {pack.Title}?",
				"Every character, float and point of THIS collection on this phone will be wiped. Other collections stay. Printed codes keep working.",
				"Reset", palette.Danger, () => ResetCollectionConfirmed?.Invoke());
			y -= 190f;

			var erase = ChunkyButton.Create(content, Ctx, "Erase", new Vector2(860f, 150f), palette.Danger.Darken(0.35f), "Erase everything", null, 56f);
			erase.Root().Place(new Vector2(0.5f, 1f), new Vector2(860f, 150f), new Vector2(0f, y), new Vector2(0.5f, 1f));
			erase.Clicked = () => MessagePopups.Confirm(Ctx, "Erase everything?",
				"ALL collections, points and times on this phone will be wiped. Your nickname and settings stay.",
				"Erase", palette.Danger.Darken(0.2f), () => EraseEverythingConfirmed?.Invoke());
			y -= 210f;

			var footer = UiBuilder.Text(content, "Footer", Ctx, "Progress lives on this phone only. On iPhone, add the game to your Home Screen so Safari keeps it.", 34f, TextStyle.Body, palette.Muted);
			footer.rectTransform.StretchX(1f, 100f, y, 60f, 1f);
			footer.AutoSize(24f);
			y -= 200f;

			content.sizeDelta = new Vector2(0f, -y);
		}

		private void AddToggle(RectTransform content, string label, Func<bool> get, Action<bool> set, ref float y)
		{
			var toggle = ToggleSwitch.Create(content, Ctx, label, get, set);
			toggle.Root().Place(new Vector2(0.5f, 1f), new Vector2(960f, 130f), new Vector2(0f, y), new Vector2(0.5f, 1f));
			_toggles.Add(toggle);
			y -= 150f;
		}

		protected override void OnShow()
		{
			_scroll.verticalNormalizedPosition = 1f;
			foreach (var toggle in _toggles)
				toggle.Apply(false);
		}
	}
}
