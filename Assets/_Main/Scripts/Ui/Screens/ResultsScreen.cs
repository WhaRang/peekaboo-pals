using System.Collections.Generic;
using System.Linq;
using _Main.Scripts._ProjectAgnostic.Utils;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Ecs.Collection.Sys;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Popups;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Screens
{
	/// <summary>
	/// "Show this to your friends" scoreboard: big total, rank title, hunt times and a row per character.
	/// Built for comparing two phones side by side: large type, high contrast, same layout on every device.
	/// </summary>
	public sealed class ResultsScreen : ScreenBase
	{
		private const float RowHeight = 150f;

		private RectTransform _content = null!;
		private ScrollRect _scroll = null!;
		private CountUpText _points = null!;
		private TextMeshProUGUI _rank = null!;
		private Image _rankPill = null!;
		private TextMeshProUGUI _found = null!;
		private TextMeshProUGUI _started = null!;
		private TextMeshProUGUI _completed = null!;
		private TextMeshProUGUI _duration = null!;
		private TextMeshProUGUI _durationLabel = null!;
		private TextMeshProUGUI _nickname = null!;
		private readonly List<Row> _rows = new();
		private float _nextClockRefresh;

		private sealed class Row
		{
			public CharacterDefinition Character = null!;
			public Image Background = null!;
			public Image Icon = null!;
			public TextMeshProUGUI Name = null!;
			public TextMeshProUGUI Rarity = null!;
			public TextMeshProUGUI Power = null!;
			public TextMeshProUGUI Points = null!;
			public Image Check = null!;
		}

		protected override void Build()
		{
			var palette = Ctx.Palette;

			var viewport = UiBuilder.Rect("Viewport", Rect);
			viewport.Stretch();
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

			float y = -30f;

			var packTitle = UiBuilder.Text(_content, "Pack", Ctx, Ctx.Pack.Title.ToUpperInvariant(), 44f, TextStyle.TitleThin, Ctx.Pack.Theme.Accent2.Lighten(0.1f));
			packTitle.rectTransform.StretchX(1f, 60f, y, 40f, 1f);
			packTitle.AutoSize(30f);
			y -= 58f;

			var title = UiBuilder.Text(_content, "Title", Ctx, Ctx.Pack.Texts.HaulTitle, 100f, TextStyle.Title, Ctx.Pack.Theme.Accent);
			title.rectTransform.StretchX(1f, 130f, y, 30f, 1f);
			title.AutoSize(60f);
			y -= 132f;

			_nickname = UiBuilder.Text(_content, "Nickname", Ctx, "", 60f, TextStyle.TitleThin, palette.Cream);
			_nickname.rectTransform.StretchX(1f, 80f, y, 40f, 1f);
			_nickname.textWrappingMode = TextWrappingModes.NoWrap;
			_nickname.AutoSize(36f);
			y -= 92f;

			_rankPill = UiBuilder.Image(_content, "RankPill", Ctx, UiSprite.Pill, palette.Purple);
			_rankPill.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(700f, 96f), new Vector2(0f, y), new Vector2(0.5f, 1f));
			_rank = UiBuilder.Text(_rankPill.rectTransform, "Rank", Ctx, "", 54f, TextStyle.TitleThin, palette.Cream);
			_rank.rectTransform.Stretch(20f, 20f, 0f, 4f);
			_rank.AutoSize(34f);
			y -= 120f;

			// Big score card
			var scoreCard = UiBuilder.Image(_content, "ScoreCard", Ctx, UiSprite.RoundedRect, palette.Ink.WithAlpha(0.85f));
			scoreCard.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(980f, 330f), new Vector2(0f, y), new Vector2(0.5f, 1f));
			var candy = UiBuilder.Image(scoreCard.rectTransform, "Candy", Ctx, UiSprite.IconCandy, Color.white);
			candy.preserveAspect = true;
			candy.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(190f, 190f), new Vector2(140f, 10f));
			candy.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
			var points = UiBuilder.Text(scoreCard.rectTransform, "Points", Ctx, "0", 200f, TextStyle.Title, palette.Gold);
			points.rectTransform.Stretch(240f, 30f, 10f, 60f);
			points.textWrappingMode = TextWrappingModes.NoWrap;
			points.AutoSize(110f);
			_points = CountUpText.Attach(points, Ctx);
			var pointsLabel = UiBuilder.Text(scoreCard.rectTransform, "PointsLabel", Ctx, "COLLECTION POINTS", 48f, TextStyle.TitleThin, palette.Muted);
			pointsLabel.rectTransform.StretchX(0f, 60f, 40f, 240f, 0f);
			y -= 360f;

			// Stats grid 2x2
			_found = AddStatTile("Found", UiSprite.IconPal, new Vector2(-245f, y), palette.Slime);
			_duration = AddStatTile("Total time", UiSprite.IconClock, new Vector2(245f, y), palette.Candy);
			_durationLabel = _duration.transform.parent.Find("Label").GetComponent<TextMeshProUGUI>();
			y -= 230f;
			_started = AddStatTile("Hunt started", UiSprite.IconStar, new Vector2(-245f, y), palette.Pumpkin);
			_completed = AddStatTile("Completed", UiSprite.IconTrophy, new Vector2(245f, y), palette.Gold);
			y -= 260f;

			var listTitle = UiBuilder.Text(_content, "ListTitle", Ctx, "THE ROSTER", 64f, TextStyle.Title, palette.Cream);
			listTitle.rectTransform.StretchX(1f, 90f, y, 30f, 1f);
			y -= 110f;

			var ordered = Ctx.Pack.Characters
				.Select((c, index) => (c, index))
				.OrderByDescending(p => p.c.Rarity)
				.ThenBy(p => p.index)
				.Select(p => p.c);

			foreach (var character in ordered)
			{
				_rows.Add(AddRow(character, y));
				y -= RowHeight + 16f;
			}

			y -= 20f;
			var footer = UiBuilder.Text(_content, "Footer", Ctx, "Show this screen to your friends!\nMost points wins. Finishing first wins bragging rights.", 40f, TextStyle.Body, palette.Muted);
			footer.rectTransform.StretchX(1f, 140f, y, 40f, 1f);
			y -= 320f;

			_content.sizeDelta = new Vector2(0f, -y);
		}

		private TextMeshProUGUI AddStatTile(string label, UiSprite icon, Vector2 position, Color accent)
		{
			var palette = Ctx.Palette;
			var tile = UiBuilder.Image(_content, label, Ctx, UiSprite.RoundedRect, palette.Night.Lighten(0.06f));
			tile.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(470f, 210f), position, new Vector2(0.5f, 1f));
			var stripe = UiBuilder.Image(tile.rectTransform, "Stripe", Ctx, UiSprite.Pill, accent);
			stripe.rectTransform.StretchX(1f, 12f, -14f, 60f, 1f);

			var iconImage = UiBuilder.Image(tile.rectTransform, "Icon", Ctx, icon, Color.white);
			iconImage.preserveAspect = true;
			iconImage.rectTransform.Place(new Vector2(0f, 1f), new Vector2(64f, 64f), new Vector2(60f, -70f));

			var title = UiBuilder.Text(tile.rectTransform, "Label", Ctx, label.ToUpperInvariant(), 36f, TextStyle.TitleThin, palette.Muted, TextAlignmentOptions.Left);
			title.rectTransform.StretchX(1f, 60f, -42f, 0f, 1f);
			title.rectTransform.offsetMin = new Vector2(104f, title.rectTransform.offsetMin.y);
			title.rectTransform.offsetMax = new Vector2(-20f, title.rectTransform.offsetMax.y);
			title.AutoSize(24f);

			var value = UiBuilder.Text(tile.rectTransform, "Value", Ctx, "—", 76f, TextStyle.Title, palette.Cream);
			value.rectTransform.StretchX(0f, 100f, 18f, 20f, 0f);
			value.textWrappingMode = TextWrappingModes.NoWrap;
			value.AutoSize(40f);
			return value;
		}

		private Row AddRow(CharacterDefinition character, float y)
		{
			var palette = Ctx.Palette;
			var tier = Ctx.Tier(character);
			var row = new Row { Character = character };

			row.Background = UiBuilder.Image(_content, character.Id, Ctx, UiSprite.RoundedRect, palette.Night.Lighten(0.05f));
			var rect = row.Background.rectTransform;
			rect.Place(new Vector2(0.5f, 1f), new Vector2(980f, RowHeight), new Vector2(0f, y), new Vector2(0.5f, 1f));

			var edge = UiBuilder.Image(rect, "Edge", Ctx, UiSprite.Pill, tier.PrimaryColor);
			edge.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(16f, RowHeight - 30f), new Vector2(22f, 0f));

			var iconBack = UiBuilder.Image(rect, "IconBack", Ctx, UiSprite.Circle, tier.PrimaryColor.Darken(0.55f));
			iconBack.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(120f, 120f), new Vector2(110f, 0f));
			row.Icon = UiBuilder.Image(iconBack.rectTransform, "Icon", character.Art, Color.white);
			row.Icon.preserveAspect = true;
			row.Icon.rectTransform.Stretch(6f, 6f, 6f, 6f);

			row.Name = UiBuilder.Text(rect, "Name", Ctx, character.DisplayName, 50f, TextStyle.TitleThin, palette.Cream, TextAlignmentOptions.BottomLeft);
			row.Name.rectTransform.Stretch(190f, 345f, 18f, RowHeight * 0.48f);
			row.Name.textWrappingMode = TextWrappingModes.NoWrap;
			row.Name.AutoSize(30f);

			var rarity = UiBuilder.Text(rect, "Rarity", Ctx, $"{tier.TierName} · {tier.RarityLabel}", 36f, TextStyle.Body, tier.PrimaryColor.Lighten(0.15f), TextAlignmentOptions.TopLeft);
			rarity.rectTransform.Stretch(190f, 345f, RowHeight * 0.55f, 14f);
			rarity.textWrappingMode = TextWrappingModes.NoWrap;
			rarity.AutoSize(24f);
			row.Rarity = rarity;

			row.Power = UiBuilder.Text(rect, "Power", Ctx, "", 34f, TextStyle.TitleThin, palette.Muted, TextAlignmentOptions.Center);
			row.Power.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(150f, 120f), new Vector2(-265f, 0f));
			row.Power.textWrappingMode = TextWrappingModes.NoWrap;

			row.Points = UiBuilder.Text(rect, "Points", Ctx, "", 56f, TextStyle.Title, palette.Gold, TextAlignmentOptions.Right);
			row.Points.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(170f, 100f), new Vector2(-115f, 0f));
			row.Points.textWrappingMode = TextWrappingModes.NoWrap;

			// "Got it" star sits on the portrait's corner.
			row.Check = UiBuilder.Image(iconBack.rectTransform, "Check", Ctx, UiSprite.IconStar, palette.Gold);
			row.Check.preserveAspect = true;
			row.Check.rectTransform.Place(new Vector2(1f, 1f), new Vector2(52f, 52f), new Vector2(-6f, -6f));
			return row;
		}

		protected override void OnShow()
		{
			Refresh(Ctx.Snapshot, animate: true);
			_scroll.verticalNormalizedPosition = 1f;
		}

		public void Refresh(CollectionSnapshot snapshot, bool animate)
		{
			var palette = Ctx.Palette;

			if (animate)
			{
				_points.SetImmediate(0);
				_points.AnimateTo(snapshot.TotalPoints, 1.2f);
			}
			else
			{
				_points.SetImmediate(snapshot.TotalPoints);
			}

			_nickname.text = $"Hunter: {Ctx.Settings.Nickname}";

			var (rankTitle, rankColor) = Rank(snapshot);
			_rank.text = rankTitle;
			_rankPill.color = rankColor;
			if (animate && !Ctx.ReducedMotion)
				_rankPill.transform.ScaleFromTo(0.6f, 1f, 0.5f, Ease.OutBackStrong);

			_found.text = $"{snapshot.CollectedCount}/{snapshot.TotalCharacters}";
			_started.text = snapshot.FirstScanUnixSeconds.HasValue ? TimeFormat.ShortTime(snapshot.FirstScanUnixSeconds.Value) : "—";
			_completed.text = snapshot.CompletedUnixSeconds.HasValue ? TimeFormat.ShortTime(snapshot.CompletedUnixSeconds.Value) : "Not yet";
			RefreshDuration(snapshot);

			foreach (var row in _rows)
			{
				bool collected = snapshot.IsCollected(row.Character.Id);
				var tier = Ctx.Tier(row.Character);
				row.Icon.sprite = collected ? row.Character.Art : row.Character.Silhouette ?? row.Character.Art;
				row.Icon.color = collected ? Color.white : palette.NightDeep.Lighten(0.12f);
				row.Name.text = collected ? row.Character.DisplayName : "???";
				row.Name.color = collected ? palette.Cream : palette.Muted.WithAlpha(0.7f);
				if (collected && snapshot.TryGet(row.Character.Id, out var caught))
				{
					var grade = Ctx.Pack.GetFloatGrade(caught.BestFloat);
					string gradeColor = ColorUtility.ToHtmlStringRGB(grade.Color.Lighten(0.2f));
					row.Power.text = $"PWR {row.Character.Power}\n<color=#{gradeColor}>{FloatStyle.Short(caught.BestFloat)}</color>";
					row.Points.text = $"+{CollectionRules.CharacterPoints(Ctx.Pack, row.Character, caught.BestFloat)}";
					row.Rarity.text = $"{tier.RarityLabel} · <color=#{gradeColor}>{grade.Name}</color>";
				}
				else
				{
					row.Power.text = "PWR ?\n?.????";
					row.Points.text = "—";
					row.Rarity.text = $"{tier.TierName} · {tier.RarityLabel}";
				}
				row.Points.color = collected ? palette.Gold : palette.Muted.WithAlpha(0.5f);
				row.Check.gameObject.SetActive(collected);
				row.Background.color = collected ? Color.Lerp(palette.Night, tier.PrimaryColor, 0.16f) : palette.Night.WithAlpha(0.75f);
			}
		}

		private void RefreshDuration(CollectionSnapshot snapshot)
		{
			if (!snapshot.FirstScanUnixSeconds.HasValue)
			{
				_duration.text = "—";
				_durationLabel.text = "TOTAL TIME";
				return;
			}

			long end = snapshot.CompletedUnixSeconds ?? System.DateTime.UtcNow.ToUnixTimeSeconds();
			_duration.text = TimeFormat.Duration(end - snapshot.FirstScanUnixSeconds.Value);
			_durationLabel.text = snapshot.IsComplete ? "TOTAL TIME" : "HUNTING FOR";
		}

		private void Update()
		{
			if (!IsShown || Ctx.Snapshot.IsComplete || Time.unscaledTime < _nextClockRefresh)
				return;

			_nextClockRefresh = Time.unscaledTime + 1f;
			RefreshDuration(Ctx.Snapshot);
		}

		private (string, Color) Rank(CollectionSnapshot snapshot)
		{
			var palette = Ctx.Palette;
			var texts = Ctx.Pack.Texts;
			if (snapshot.IsComplete)
				return (texts.Rank(5), palette.Gold.Darken(0.2f));

			float ratio = snapshot.TotalCharacters > 0 ? snapshot.CollectedCount / (float)snapshot.TotalCharacters : 0f;
			if (ratio <= 0f) return (texts.Rank(0), palette.Pumpkin.Darken(0.25f));
			if (ratio < 0.3f) return (texts.Rank(1), palette.PurpleLight.Darken(0.3f));
			if (ratio < 0.6f) return (texts.Rank(2), palette.Purple);
			if (ratio < 0.9f) return (texts.Rank(3), palette.Candy.Darken(0.25f));
			return (texts.Rank(4), palette.Slime.Darken(0.45f));
		}
	}
}
