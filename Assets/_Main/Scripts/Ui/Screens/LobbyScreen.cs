using System;
using System.Collections.Generic;
using System.Linq;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.Collection.Sys;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Popups;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Screens
{
	/// <summary>
	/// The home screen of one collection: its logo (or title), the album shelf button, points, collected count with a
	/// progress bar and the card wall. The wall is built from the pack: one row per rarity tier (rarest on top), and
	/// small neighbouring tiers share a row with the rarer cards in the middle (1 legendary between 2 epics).
	/// </summary>
	public sealed class LobbyScreen : ScreenBase
	{
		private const float CardAspect = 1.36f;
		private const float MaxRowCards = 3;

		private readonly Dictionary<string, CardView> _cards = new(StringComparer.OrdinalIgnoreCase);
		private CountUpText _points = null!;
		private TextMeshProUGUI _collectedLabel = null!;
		private ProgressBar _progress = null!;
		private ChunkyButton _soundButton = null!;
		private RectTransform _logo = null!;
		private RectTransform _namePill = null!;
		private TextMeshProUGUI _nickname = null!;
		private ScrollRect _scroll = null!;
		private RectTransform _content = null!;
		private CollectionSnapshot _shown = CollectionSnapshot.Empty;

		/// <summary>Raised by a long-press on the logo (hidden host tools).</summary>
		public Action? HostToolsRequested;

		/// <summary>Raised by the album shelf button.</summary>
		public Action? AlbumsRequested;

		protected override void Build()
		{
			BuildHeader();
			BuildWall();
		}

		private const float HeaderHeight = 470f;

		private void BuildHeader()
		{
			var palette = Ctx.Palette;
			var header = UiBuilder.Rect("Header", Rect);
			header.StretchX(1f, HeaderHeight, 0f, 0f, 1f);

			_logo = BuildPackTitle(header);
			var longPress = _logo.gameObject.AddComponent<LongPressArea>();
			longPress.Setup(Ctx, 1.3f, () => HostToolsRequested?.Invoke(), OnLogoTapped);

			var albums = ChunkyButton.Create(header, Ctx, "Albums", new Vector2(124f, 124f), Ctx.Pack.Theme.Accent.Darken(0.25f), null, UiSprite.IconAlbums);
			albums.Root().Place(new Vector2(0f, 1f), new Vector2(124f, 124f), new Vector2(30f, -40f), new Vector2(0f, 1f));
			albums.Clicked = () => AlbumsRequested?.Invoke();

			_soundButton = ChunkyButton.Create(header, Ctx, "Sound", new Vector2(124f, 124f), palette.Purple, null,
				Ctx.Settings.IsMuted ? UiSprite.IconSoundOff : UiSprite.IconSoundOn);
			_soundButton.Root().Place(new Vector2(1f, 1f), new Vector2(124f, 124f), new Vector2(-30f, -40f), new Vector2(1f, 1f));
			_soundButton.ClickSound = GameSound.Toggle;
			_soundButton.Clicked = ToggleSound;

			// Nickname pill (tap to edit)
			var namePill = UiBuilder.Image(header, "Nickname", Ctx, UiSprite.Pill, palette.Purple.Darken(0.25f), raycast: true);
			_namePill = namePill.rectTransform;
			_namePill.Place(new Vector2(0.5f, 1f), new Vector2(640f, 92f), new Vector2(0f, -206f), new Vector2(0.5f, 1f));
			var hunter = UiBuilder.Text(_namePill, "Prefix", Ctx, "HUNTER", 30f, TextStyle.TitleThin, palette.Muted, TextAlignmentOptions.Left);
			hunter.rectTransform.Stretch(36f, 0f, 0f, 0f);
			_nickname = UiBuilder.Text(_namePill, "Name", Ctx, Ctx.Settings.Nickname, 50f, TextStyle.Title, palette.Cream, TextAlignmentOptions.Center);
			_nickname.rectTransform.Stretch(150f, 96f, 0f, 2f);
			_nickname.textWrappingMode = TextWrappingModes.NoWrap;
			_nickname.AutoSize(30f);
			var edit = UiBuilder.Image(_namePill, "Edit", Ctx, UiSprite.IconEdit, Color.white);
			edit.preserveAspect = true;
			edit.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(64f, 64f), new Vector2(-50f, 0f));
			_namePill.gameObject.AddComponent<TapArea>().Tapped = () =>
			{
				Ctx.Audio.Play(GameSound.ButtonPress);
				_namePill.PunchScale(0.08f, 0.3f, 2);
				NicknamePopup.Show(Ctx, RefreshNickname);
			};

			// Points pill
			var pointsPill = UiBuilder.Image(header, "PointsPill", Ctx, UiSprite.Pill, palette.Ink.WithAlpha(0.8f));
			pointsPill.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(470f, 104f), new Vector2(-250f, -312f), new Vector2(0.5f, 1f));
			var candy = UiBuilder.Image(pointsPill.rectTransform, "Candy", Ctx, UiSprite.IconCandy, Color.white);
			candy.preserveAspect = true;
			candy.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(110f, 110f), new Vector2(42f, 4f));
			candy.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
			var pointsText = UiBuilder.Text(pointsPill.rectTransform, "Points", Ctx, "0", 68f, TextStyle.Title, palette.Gold, TextAlignmentOptions.Left);
			pointsText.rectTransform.Stretch(108f, 110f, 0f, 4f);
			pointsText.textWrappingMode = TextWrappingModes.NoWrap;
			var ptsLabel = UiBuilder.Text(pointsPill.rectTransform, "Pts", Ctx, "PTS", 38f, TextStyle.TitleThin, palette.Muted, TextAlignmentOptions.Right);
			ptsLabel.rectTransform.Stretch(0f, 30f, 0f, 0f);
			_points = CountUpText.Attach(pointsText, Ctx);

			// Collected pill
			var countPill = UiBuilder.Image(header, "CountPill", Ctx, UiSprite.Pill, palette.Ink.WithAlpha(0.8f));
			countPill.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(470f, 104f), new Vector2(250f, -312f), new Vector2(0.5f, 1f));
			var ghost = UiBuilder.Image(countPill.rectTransform, "Pal", Ctx, UiSprite.IconPal, Color.white);
			ghost.preserveAspect = true;
			ghost.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(100f, 100f), new Vector2(40f, 6f));
			ghost.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
			_collectedLabel = UiBuilder.Text(countPill.rectTransform, "Count", Ctx, $"0/{Ctx.Pack.Characters.Count}", 68f, TextStyle.Title, Ctx.Pack.Theme.Accent2.Lighten(0.1f), TextAlignmentOptions.Left);
			_collectedLabel.rectTransform.Stretch(108f, 20f, 0f, 4f);
			_collectedLabel.textWrappingMode = TextWrappingModes.NoWrap;
			var foundLabel = UiBuilder.Text(countPill.rectTransform, "Found", Ctx, "FOUND", 38f, TextStyle.TitleThin, palette.Muted, TextAlignmentOptions.Right);
			foundLabel.rectTransform.Stretch(0f, 30f, 0f, 0f);

			_progress = ProgressBar.Create(header, Ctx, new Vector2(970f, 40f), Ctx.Pack.Theme.Accent2);
			((RectTransform)_progress.transform).Place(new Vector2(0.5f, 1f), new Vector2(970f, 40f), new Vector2(0f, -446f), new Vector2(0.5f, 0.5f));
		}

		/// <summary>The pack's logo image, or its title in the display font when the pack has none.</summary>
		private RectTransform BuildPackTitle(RectTransform header)
		{
			var size = new Vector2(600f, 180f);
			var theme = Ctx.Pack.Theme;
			if (theme.Logo != null)
			{
				var logoImage = UiBuilder.Image(header, "Logo", theme.Logo, Color.white, raycast: true);
				logoImage.preserveAspect = true;
				logoImage.rectTransform.Place(new Vector2(0.5f, 1f), size, new Vector2(0f, -18f), new Vector2(0.5f, 1f));
				return logoImage.rectTransform;
			}

			var holder = UiBuilder.Image(header, "Logo", (Sprite?)null, new Color(0f, 0f, 0f, 0f), raycast: true);
			holder.rectTransform.Place(new Vector2(0.5f, 1f), size, new Vector2(0f, -18f), new Vector2(0.5f, 1f));
			var title = UiBuilder.Text(holder.rectTransform, "Title", Ctx, Ctx.Pack.Title.ToUpperInvariant(), 96f, TextStyle.Title, theme.Accent);
			title.rectTransform.Stretch(0f, 0f, 0f, 24f);
			title.AutoSize(48f);
			var tagline = UiBuilder.Text(holder.rectTransform, "Tagline", Ctx, Ctx.Pack.Texts.Tagline, 34f, TextStyle.BodyOutline, Ctx.Palette.Cream);
			tagline.rectTransform.StretchX(0f, 44f, 6f, 0f, 0f);
			tagline.AutoSize(24f);
			return holder.rectTransform;
		}

		public void RefreshNickname()
		{
			_nickname.text = Ctx.Settings.Nickname;
			_nickname.transform.PunchScale(0.15f, 0.35f, 2);
		}

		private void BuildWall()
		{
			var viewport = UiBuilder.Rect("Viewport", Rect);
			viewport.Stretch(0f, 0f, HeaderHeight + 4f, 0f);
			viewport.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, 40);
			var hit = viewport.gameObject.AddComponent<Image>();
			hit.color = new Color(0f, 0f, 0f, 0f);

			_content = UiBuilder.Rect("Content", viewport);
			_content.anchorMin = new Vector2(0f, 1f);
			_content.anchorMax = new Vector2(1f, 1f);
			_content.pivot = new Vector2(0.5f, 1f);

			_scroll = viewport.gameObject.AddComponent<ScrollRect>();
			_scroll.viewport = viewport;
			_scroll.content = _content;
			_scroll.horizontal = false;
			_scroll.movementType = ScrollRect.MovementType.Elastic;
			_scroll.scrollSensitivity = 30f;

			float y = -40f;
			foreach (var row in BuildRows())
			{
				float available = 1000f;
				float spacing = 26f;
				float widthLimit = (available - spacing * (row.Count - 1)) / row.Count;
				var widths = row.Select(c => Mathf.Min(widthLimit, BaseCardWidth(c.Rarity))).ToList();
				float rowHeight = widths.Max() * CardAspect;
				float total = widths.Sum() + spacing * (row.Count - 1);
				float x = -total * 0.5f;

				for (int i = 0; i < row.Count; i++)
				{
					var character = row[i];
					var size = new Vector2(widths[i], widths[i] * CardAspect);
					var card = CardView.Create(_content, Ctx, character, size);
					// Cards sit on the row's baseline, rarer (bigger) cards rise above it.
					card.Root.Place(new Vector2(0.5f, 1f), size, new Vector2(x + widths[i] * 0.5f, y - rowHeight + size.y * 0.5f));
					x += widths[i] + spacing;

					var tap = card.gameObject.AddComponent<Image>();
					tap.color = new Color(0f, 0f, 0f, 0f);
					var clicker = card.gameObject.AddComponent<TapArea>();
					var id = character.Id;
					clicker.Tapped = () => OnCardTapped(id);

					_cards[character.Id] = card;
				}

				y -= rowHeight + 46f;
			}

			_content.sizeDelta = new Vector2(0f, -y + 20f);
		}

		/// <summary>Tiers rarest first; consecutive small tiers merge into one row with the rarest in the middle.</summary>
		private List<List<CharacterDefinition>> BuildRows()
		{
			var tiers = Ctx.Pack.Characters
				.GroupBy(c => c.Rarity)
				.OrderByDescending(g => g.Key)
				.Select(g => g.ToList())
				.ToList();

			var rows = new List<List<CharacterDefinition>>();
			foreach (var tier in tiers)
			{
				var last = rows.Count > 0 ? rows[rows.Count - 1] : null;
				if (last != null && last.Count + tier.Count <= MaxRowCards && last.All(c => c.Rarity > tier[0].Rarity))
				{
					// Flank the rarer cards: split this tier to both sides.
					int left = tier.Count / 2;
					var merged = new List<CharacterDefinition>();
					merged.AddRange(tier.Take(left));
					merged.AddRange(last);
					merged.AddRange(tier.Skip(left));
					rows[rows.Count - 1] = merged;
				}
				else
				{
					rows.Add(tier);
				}
			}

			return rows;
		}

		private static float BaseCardWidth(CharacterRarity rarity) => rarity switch
		{
			CharacterRarity.Legendary => 320f,
			CharacterRarity.Epic => 268f,
			CharacterRarity.Rare => 290f,
			_ => 232f
		};

		/// <summary>
		/// Updates cards and header. <paramref name="pendingId"/> keeps the state it had before (locked, or its previous
		/// float) until its reveal lands in the slot; the header points are computed from what is actually shown.
		/// </summary>
		public void ApplySnapshot(CollectionSnapshot snapshot, bool animate, string? pendingId = null)
		{
			var shown = new Dictionary<string, CaughtCreature>(StringComparer.OrdinalIgnoreCase);
			foreach (var pair in snapshot.Caught)
				shown[pair.Key] = pair.Value;

			if (pendingId != null)
			{
				if (_shown.TryGet(pendingId, out var before))
					shown[pendingId] = before;
				else
					shown.Remove(pendingId);
			}

			foreach (var pair in _cards)
			{
				bool collected = shown.TryGetValue(pair.Key, out var caught);
				pair.Value.SetFloat(collected ? caught.BestFloat : null);
				if (pair.Value.IsCollected != collected)
					pair.Value.SetCollected(collected);
				pair.Value.SetIdle(collected);
			}

			bool complete = snapshot.IsComplete && CollectionRules.IsEveryCharacterCollected(Ctx.Pack, shown);
			_shown = new CollectionSnapshot(Ctx.Pack.Id, shown, snapshot.FirstScanUnixSeconds, complete ? snapshot.CompletedUnixSeconds : null,
				CollectionRules.CalculatePoints(Ctx.Pack, shown, complete), snapshot.TotalCharacters);

			int points = _shown.TotalPoints;
			int collectedCount = _shown.CollectedCount;
			_collectedLabel.text = $"{collectedCount}/{snapshot.TotalCharacters}";
			float progress = snapshot.TotalCharacters > 0 ? collectedCount / (float)snapshot.TotalCharacters : 0f;

			if (animate)
			{
				_points.AnimateTo(points);
				_progress.AnimateTo(progress);
				_collectedLabel.transform.PunchScale(0.3f, 0.45f);
			}
			else
			{
				_points.SetImmediate(points);
				_progress.SetImmediate(progress);
			}
		}

		public CardView? GetCard(string id) => _cards.TryGetValue(id, out var card) ? card : null;

		/// <summary>Scrolls so the card is visible (before a reveal flies into it).</summary>
		public void ScrollToCard(string id)
		{
			if (!_cards.TryGetValue(id, out var card))
				return;

			Canvas.ForceUpdateCanvases();
			float viewportHeight = _scroll.viewport.rect.height;
			float contentHeight = _content.rect.height;
			if (contentHeight <= viewportHeight)
				return;

			float cardY = -card.Root.anchoredPosition.y;
			float target = Mathf.Clamp01((cardY - viewportHeight * 0.5f) / (contentHeight - viewportHeight));
			_scroll.StopMovement();
			_scroll.verticalNormalizedPosition = 1f - target;
		}

		private void OnCardTapped(string id)
		{
			if (!_cards.TryGetValue(id, out var card))
				return;

			if (!card.IsCollected)
			{
				card.Body.Wobble(8f, 0.45f);
				Ctx.Audio.Play(GameSound.Toggle, 0.8f);
				return;
			}

			CardDetailPopup.Show(Ctx, card.Character, _shown);
		}

		private void OnLogoTapped()
		{
			_logo.PunchScale(0.12f, 0.5f);
			if (Ctx.Pack.Theme.Bats)
				Ctx.FxParticles.EmitAtWorld(_logo.position, ParticlePresets.Bats(Ctx.Palette.Ink.Lighten(0.15f)), 6);
			else
				Ctx.FxParticles.EmitAtWorld(_logo.position, ParticlePresets.Sparkles(Ctx.Pack.Theme.Accent), 10);
		}

		private void ToggleSound()
		{
			Ctx.Settings.IsMuted = !Ctx.Settings.IsMuted;
			if (_soundButton.Icon != null)
				_soundButton.Icon.sprite = Ctx.Sprite(Ctx.Settings.IsMuted ? UiSprite.IconSoundOff : UiSprite.IconSoundOn);
		}

		protected override void OnShow()
		{
			_nickname.text = Ctx.Settings.Nickname;
			if (_soundButton.Icon != null)
				_soundButton.Icon.sprite = Ctx.Sprite(Ctx.Settings.IsMuted ? UiSprite.IconSoundOff : UiSprite.IconSoundOn);
		}
	}

	/// <summary>Tap that ignores drags, so scrolling the wall doesn't open cards.</summary>
	public sealed class TapArea : MonoBehaviour, IPointerClickHandler
	{
		public Action? Tapped;

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!eventData.dragging)
				Tapped?.Invoke();
		}
	}

	/// <summary>Hold to trigger (with a growing wobble as feedback); a short tap triggers the tap callback instead.</summary>
	public sealed class LongPressArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
	{
		private UiContext _ctx = null!;
		private float _seconds;
		private Action _onLongPress = null!;
		private Action? _onTap;
		private bool _pressed;
		private float _time;

		public void Setup(UiContext ctx, float seconds, Action onLongPress, Action? onTap)
		{
			_ctx = ctx;
			_seconds = seconds;
			_onLongPress = onLongPress;
			_onTap = onTap;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			_pressed = true;
			_time = 0f;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (_pressed && _time < 0.35f)
				_onTap?.Invoke();
			Cancel();
		}

		public void OnPointerExit(PointerEventData eventData) => Cancel();

		private void Cancel()
		{
			_pressed = false;
			transform.localRotation = Quaternion.identity;
		}

		private void Update()
		{
			if (!_pressed)
				return;

			_time += Time.unscaledDeltaTime;
			float k = Mathf.Clamp01(_time / _seconds);
			if (!_ctx.ReducedMotion)
				transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 40f) * 4f * k);

			if (_time < _seconds)
				return;

			Cancel();
			_ctx.Haptics.Play(HapticStrength.Heavy);
			_onLongPress();
		}
	}
}
