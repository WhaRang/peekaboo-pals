using System;
using System.Globalization;
using System.Linq;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Screens
{
	/// <summary>
	/// The album shelf: every collection ever brought to a party as a "book" (cover, date, progress, points, a gold
	/// badge once complete), the lifetime score, and teaser books for parties announced in index.json. Collections
	/// that need a newer app show as locked books, with a banner when the website announces a new app version.
	/// Tap a book to look at that collection. Rebuilt every time it opens, so it always shows the latest progress.
	/// </summary>
	public sealed class AlbumScreen : ScreenBase
	{
		private const float BookWidth = 980f;
		private const float BookHeight = 330f;
		private const float BookSpacing = 36f;

		private RectTransform _content = null!;
		private ScrollRect _scroll = null!;
		private TextMeshProUGUI _total = null!;
		private bool _animate = true;

		public Action? CloseRequested;
		public Action<CollectionPack>? PackSelected;

		protected override void Build()
		{
			var palette = Ctx.Palette;

			var background = UiBuilder.Image(Rect, "Background", (Sprite?)null, palette.NightDeep, raycast: true);
			background.rectTransform.Stretch(-300f, -300f, -300f, -300f);
			var sheen = UiBuilder.Image(Rect, "Sheen", Ctx, UiSprite.GradientVertical, palette.Purple.WithAlpha(0.45f));
			sheen.rectTransform.StretchX(1f, 900f, 300f, -300f, 1f);

			var logo = UiBuilder.Image(Rect, "Logo", Ctx, UiSprite.Logo, Color.white);
			logo.preserveAspect = true;
			logo.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(620f, 225f), new Vector2(0f, -24f), new Vector2(0.5f, 1f));

			var close = ChunkyButton.Create(Rect, Ctx, "Close", new Vector2(124f, 124f), palette.Danger, null, UiSprite.IconClose);
			close.Root().Place(new Vector2(1f, 1f), new Vector2(124f, 124f), new Vector2(-30f, -40f), new Vector2(1f, 1f));
			close.Clicked = () => CloseRequested?.Invoke();

			var totalPill = UiBuilder.Image(Rect, "TotalPill", Ctx, UiSprite.Pill, palette.Ink.WithAlpha(0.85f));
			totalPill.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(860f, 100f), new Vector2(0f, -268f), new Vector2(0.5f, 1f));
			var candy = UiBuilder.Image(totalPill.rectTransform, "Candy", Ctx, UiSprite.IconCandy, Color.white);
			candy.preserveAspect = true;
			candy.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(104f, 104f), new Vector2(46f, 4f));
			candy.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
			_total = UiBuilder.Text(totalPill.rectTransform, "Total", Ctx, "", 46f, TextStyle.TitleThin, palette.Gold);
			_total.rectTransform.Stretch(110f, 40f, 0f, 2f);
			_total.textWrappingMode = TextWrappingModes.NoWrap;
			_total.AutoSize(28f);

			var title = UiBuilder.Text(Rect, "ShelfTitle", Ctx, "MY ALBUMS", 70f, TextStyle.Title, palette.Cream);
			title.rectTransform.StretchX(1f, 90f, -390f, 40f, 1f);

			var viewport = UiBuilder.Rect("Viewport", Rect);
			viewport.Stretch(0f, 0f, 500f, 0f);
			viewport.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 30);
			viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

			_content = UiBuilder.Rect("Content", viewport);
			_content.anchorMin = new Vector2(0f, 1f);
			_content.anchorMax = new Vector2(1f, 1f);
			_content.pivot = new Vector2(0.5f, 1f);

			_scroll = viewport.gameObject.AddComponent<ScrollRect>();
			_scroll.viewport = viewport;
			_scroll.content = _content;
			_scroll.horizontal = false;
			_scroll.scrollSensitivity = 30f;
		}

		protected override void OnShow()
		{
			Refresh();
			_scroll.verticalNormalizedPosition = 1f;
		}

		/// <summary>Rebuilds the shelf from the library and the latest album snapshot.</summary>
		public void Refresh(bool animate = true)
		{
			_animate = animate;
			for (int i = _content.childCount - 1; i >= 0; i--)
				Destroy(_content.GetChild(i).gameObject);

			var packs = Ctx.Library.Packs;
			int complete = packs.Count(p => Ctx.Album.Get(p.Id, p.Characters.Count).IsComplete);
			_total.text = $"{Ctx.Album.TotalScore} PTS  ·  {complete}/{packs.Count} COMPLETE";

			float y = -16f;
			int index = 0;
			var release = Ctx.Library.NewerApp;
			if (release != null)
			{
				AddUpdateBanner(release, y);
				y -= 150f + BookSpacing;
			}

			foreach (var pack in packs)
			{
				AddBook(pack, y, index++);
				y -= BookHeight + BookSpacing;
			}

			foreach (var locked in Ctx.Library.Locked)
			{
				AddLocked(locked, release, y, index++);
				y -= BookHeight + BookSpacing;
			}

			foreach (var upcoming in Ctx.Library.Upcoming)
			{
				AddUpcoming(upcoming, y, index++);
				y -= BookHeight + BookSpacing;
			}

			var footer = UiBuilder.Text(_content, "Footer", Ctx, FooterText(), 38f, TextStyle.Body, Ctx.Palette.Muted);
			footer.rectTransform.StretchX(1f, 120f, y - 10f, 60f, 1f);
			y -= 220f;
			_content.sizeDelta = new Vector2(0f, -y);
		}

		private void AddBook(CollectionPack pack, float y, int index)
		{
			var palette = Ctx.Palette;
			var theme = pack.Theme;
			var snapshot = Ctx.Album.Get(pack.Id, pack.Characters.Count);
			bool viewing = pack == Ctx.Pack;

			var root = UiBuilder.Rect(pack.Id, _content);
			root.Place(new Vector2(0.5f, 1f), new Vector2(BookWidth, BookHeight), new Vector2(0f, y), new Vector2(0.5f, 1f));

			if (viewing)
			{
				var rim = UiBuilder.Image(root, "Rim", Ctx, UiSprite.RoundedRect, theme.Accent2);
				rim.rectTransform.Stretch(-10f, -10f, -10f, -10f);
			}

			var shadow = UiBuilder.Image(root, "Shadow", Ctx, UiSprite.RoundedRect, palette.Ink.WithAlpha(0.6f));
			shadow.rectTransform.Stretch(0f, 0f, 14f, -14f);
			var body = UiBuilder.Image(root, "Body", Ctx, UiSprite.RoundedRect, Color.Lerp(palette.Night, theme.Accent, 0.3f), raycast: true);
			body.rectTransform.Stretch();
			var spine = UiBuilder.Image(body.rectTransform, "Spine", Ctx, UiSprite.Pill, theme.Accent);
			spine.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(22f, BookHeight - 50f), new Vector2(26f, 0f));

			var coverBack = UiBuilder.Image(body.rectTransform, "CoverBack", Ctx, UiSprite.Circle, theme.Accent.Darken(0.55f));
			coverBack.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(250f, 250f), new Vector2(190f, 0f));
			var coverSprite = theme.Cover != null ? theme.Cover : pack.Characters.Count > 0 ? pack.Characters[0].Art : null;
			var cover = UiBuilder.Image(coverBack.rectTransform, "Cover", coverSprite, Color.white);
			cover.preserveAspect = true;
			cover.rectTransform.Stretch(14f, 14f, 14f, 14f);

			var title = UiBuilder.Text(body.rectTransform, "Title", Ctx, pack.Title.ToUpperInvariant(), 60f, TextStyle.Title, theme.Accent.Lighten(0.15f), TextAlignmentOptions.Left);
			title.rectTransform.Stretch(340f, 40f, 34f, BookHeight - 120f);
			title.textWrappingMode = TextWrappingModes.NoWrap;
			title.AutoSize(36f);

			var date = UiBuilder.Text(body.rectTransform, "Date", Ctx, FormatDate(pack.Date), 34f, TextStyle.Body, palette.Muted, TextAlignmentOptions.Left);
			date.rectTransform.Stretch(342f, 40f, 112f, BookHeight - 160f);

			float progress = snapshot.TotalCharacters > 0 ? snapshot.CollectedCount / (float)snapshot.TotalCharacters : 0f;
			var bar = ProgressBar.Create(body.rectTransform, Ctx, new Vector2(590f, 36f), theme.Accent2);
			((RectTransform)bar.transform).Place(new Vector2(0f, 0f), new Vector2(590f, 36f), new Vector2(340f + 295f, 118f));
			bar.SetImmediate(_animate ? 0f : progress);
			if (_animate)
				bar.AnimateTo(progress, 0.8f + index * 0.1f);

			var status = UiBuilder.Text(body.rectTransform, "Status", Ctx,
				$"{snapshot.CollectedCount}/{snapshot.TotalCharacters} found  ·  {snapshot.TotalPoints} pts", 38f, TextStyle.TitleThin, palette.Cream, TextAlignmentOptions.Left);
			status.rectTransform.Stretch(342f, 40f, BookHeight - 92f, 26f);
			status.AutoSize(26f);

			// Badges hang off the bottom of the cover, so they never collide with the title or a neighbouring book.
			if (snapshot.IsComplete)
				AddChip(coverBack.rectTransform, "COMPLETE", palette.Gold.Darken(0.05f), palette.Ink, true, -5f);
			else if (viewing)
				AddChip(coverBack.rectTransform, "VIEWING", theme.Accent2.Darken(0.25f), palette.Cream, false, 0f);

			body.gameObject.AddComponent<TapArea>().Tapped = () =>
			{
				Ctx.Audio.Play(GameSound.ButtonPress);
				root.PunchScale(0.05f, 0.25f, 2);
				PackSelected?.Invoke(pack);
			};

			Appear(root, index);
		}

		private void AddUpcoming(UpcomingPack upcoming, float y, int index)
		{
			var palette = Ctx.Palette;
			var root = UiBuilder.Rect("Upcoming", _content);
			root.Place(new Vector2(0.5f, 1f), new Vector2(BookWidth, BookHeight), new Vector2(0f, y), new Vector2(0.5f, 1f));

			var body = UiBuilder.Image(root, "Body", Ctx, UiSprite.RoundedRect, palette.Night.WithAlpha(0.7f), raycast: true);
			body.rectTransform.Stretch();
			var coverBack = UiBuilder.Image(body.rectTransform, "CoverBack", Ctx, UiSprite.Circle, palette.Ink.WithAlpha(0.8f));
			coverBack.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(250f, 250f), new Vector2(190f, 0f));
			var question = UiBuilder.Text(coverBack.rectTransform, "Question", Ctx, "?", 170f, TextStyle.Title, palette.Muted.WithAlpha(0.8f));
			question.rectTransform.Stretch(0f, 0f, 0f, 16f);

			var title = UiBuilder.Text(body.rectTransform, "Title", Ctx, upcoming.Title.ToUpperInvariant(), 58f, TextStyle.Title, palette.Muted, TextAlignmentOptions.Left);
			title.rectTransform.Stretch(340f, 40f, 60f, BookHeight - 150f);
			title.textWrappingMode = TextWrappingModes.NoWrap;
			title.AutoSize(34f);

			string when = upcoming.Date.HasValue ? $"Coming {upcoming.Date.Value.ToString("MMM d", CultureInfo.InvariantCulture)}" : "Coming soon";
			var subtitle = UiBuilder.Text(body.rectTransform, "When", Ctx, $"{when}\nNew pals are getting ready to hide…", 36f, TextStyle.Body, palette.Muted.WithAlpha(0.85f), TextAlignmentOptions.TopLeft);
			subtitle.rectTransform.Stretch(342f, 40f, 160f, 30f);
			subtitle.AutoSize(26f);

			body.gameObject.AddComponent<TapArea>().Tapped = () =>
			{
				Ctx.Audio.Play(GameSound.Toggle, 0.8f);
				root.Wobble(6f, 0.45f);
			};

			Appear(root, index);
		}

		private void AddLocked(LockedPack locked, AppRelease? release, float y, int index)
		{
			var palette = Ctx.Palette;
			var root = UiBuilder.Rect("Locked " + locked.Id, _content);
			root.Place(new Vector2(0.5f, 1f), new Vector2(BookWidth, BookHeight), new Vector2(0f, y), new Vector2(0.5f, 1f));

			var body = UiBuilder.Image(root, "Body", Ctx, UiSprite.RoundedRect, Color.Lerp(palette.Night, palette.Gold, 0.12f), raycast: true);
			body.rectTransform.Stretch();
			var coverBack = UiBuilder.Image(body.rectTransform, "CoverBack", Ctx, UiSprite.Circle, palette.Ink.WithAlpha(0.8f));
			coverBack.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(250f, 250f), new Vector2(190f, 0f));
			var pal = UiBuilder.Image(coverBack.rectTransform, "Pal", Ctx, UiSprite.IconPal, palette.Muted.WithAlpha(0.7f));
			pal.preserveAspect = true;
			pal.rectTransform.Stretch(40f, 40f, 40f, 40f);

			var title = UiBuilder.Text(body.rectTransform, "Title", Ctx, locked.Title.ToUpperInvariant(), 58f, TextStyle.Title, palette.Gold.Darken(0.1f), TextAlignmentOptions.Left);
			title.rectTransform.Stretch(340f, 40f, 60f, BookHeight - 150f);
			title.textWrappingMode = TextWrappingModes.NoWrap;
			title.AutoSize(34f);

			bool canGet = !string.IsNullOrEmpty(release?.DownloadUrl);
			string hint = canGet ? "Tap to get the update" : $"Needs app version {locked.MinAppVersion}";
			var subtitle = UiBuilder.Text(body.rectTransform, "When", Ctx, $"Ready! Update the app to open it.\n{hint}", 36f, TextStyle.Body, palette.Cream.WithAlpha(0.85f), TextAlignmentOptions.TopLeft);
			subtitle.rectTransform.Stretch(342f, 40f, 160f, 30f);
			subtitle.AutoSize(26f);
			AddChip(coverBack.rectTransform, "UPDATE", palette.Gold.Darken(0.05f), palette.Ink, false, -4f);

			body.gameObject.AddComponent<TapArea>().Tapped = () =>
			{
				Ctx.Audio.Play(GameSound.ButtonPress);
				root.PunchScale(0.05f, 0.25f, 2);
				if (canGet)
					Application.OpenURL(release!.DownloadUrl);
			};

			Appear(root, index);
		}

		private void AddUpdateBanner(AppRelease release, float y)
		{
			var palette = Ctx.Palette;
			var root = UiBuilder.Rect("UpdateBanner", _content);
			root.Place(new Vector2(0.5f, 1f), new Vector2(BookWidth, 150f), new Vector2(0f, y), new Vector2(0.5f, 1f));
			var body = UiBuilder.Image(root, "Body", Ctx, UiSprite.Pill, palette.Slime.Darken(0.35f), raycast: true);
			body.rectTransform.Stretch();
			var icon = UiBuilder.Image(body.rectTransform, "Star", Ctx, UiSprite.IconStar, Color.white);
			icon.preserveAspect = true;
			icon.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(96f, 96f), new Vector2(80f, 2f));
			bool canGet = !string.IsNullOrEmpty(release.DownloadUrl);
			var label = UiBuilder.Text(body.rectTransform, "Text", Ctx,
				canGet ? $"NEW APP VERSION {release.Version}\n<size=70%>Tap to get it. Your pals stay safe.</size>" : $"NEW APP VERSION {release.Version} IS OUT", 46f, TextStyle.TitleThin, palette.Cream, TextAlignmentOptions.Left);
			label.rectTransform.Stretch(150f, 40f, 10f, 10f);
			label.AutoSize(26f);

			body.gameObject.AddComponent<TapArea>().Tapped = () =>
			{
				Ctx.Audio.Play(GameSound.ButtonPress);
				root.PunchScale(0.04f, 0.25f, 2);
				if (canGet)
					Application.OpenURL(release.DownloadUrl);
			};

			Appear(root, 0);
		}

		private string FooterText()
		{
			var library = Ctx.Library;
			if (library.CheckStatus == PackCheckStatus.Checking)
				return "Looking for new collections…";
			if (library.UpdatesWaiting)
#if UNITY_WEBGL && !UNITY_EDITOR
				return "Collection updates are out!\nReload the page to see them.";
#else
				return "Collection updates downloaded!\nThey appear next time you open the app.";
#endif
			if (library.CheckStatus == PackCheckStatus.Offline)
				return "Offline: showing the collections on this phone.\nNew ones download when you're online.";
			return "A new collection arrives for every party.\nSee you at the next one!";
		}

		private void AddChip(RectTransform cover, string text, Color color, Color textColor, bool star, float angle)
		{
			var chip = UiBuilder.Image(cover, "Chip", Ctx, UiSprite.Pill, color);
			chip.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(250f, 70f), new Vector2(0f, 8f));
			chip.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
			var label = UiBuilder.Text(chip.rectTransform, "Text", Ctx, text, 36f, TextStyle.TitleThin, textColor);
			label.rectTransform.Stretch(star ? 62f : 16f, 16f, 0f, 3f);
			label.AutoSize(22f);
			if (!star)
				return;

			var icon = UiBuilder.Image(chip.rectTransform, "Star", Ctx, UiSprite.IconStar, Color.white);
			icon.preserveAspect = true;
			icon.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(64f, 64f), new Vector2(40f, 2f));
		}

		private void Appear(RectTransform book, int index)
		{
			if (Ctx.ReducedMotion || !_animate)
				return;
			book.localScale = Vector3.one * 0.85f;
			book.ScaleTo(1f, 0.4f, Ease.OutBackStrong).SetDelay(0.05f + index * 0.07f);
		}

		private static string FormatDate(DateTime? date)
			=> date.HasValue ? date.Value.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) : string.Empty;
	}
}
