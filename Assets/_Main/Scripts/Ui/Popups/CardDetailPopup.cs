using System;
using System.Collections.Generic;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.Collection.Sys;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Screens;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Popups
{
	/// <summary>
	/// Card-first inspect view: the catch big and alive in the middle, its rarity and flavour text under it, nothing else.
	/// The details live on the card's back: the "i" button flips the card over.
	/// </summary>
	public static class CardDetailPopup
	{
		private static readonly Vector2 CardSize = new(600f, 816f);
		private const float CardY = 120f;
		private const float BackInset = 36f;

		public static Popup Show(UiContext ctx, CharacterDefinition character, CollectionSnapshot snapshot)
		{
			var tier = ctx.Tier(character);
			var palette = ctx.Palette;
			bool owned = snapshot.TryGet(character.Id, out var caught);
			var grade = owned && ctx.Pack.FloatGrades.Count > 0 ? ctx.Pack.GetFloatGrade(caught.BestFloat) : null;

			var popup = Popup.Create(ctx, new Vector2(1000f, 1700f), tier.PrimaryColor, frameless: true);
			popup.CloseOnBackdrop = true;
			var content = popup.Content;

			var rays = UiBuilder.Image(content, "Rays", ctx, UiSprite.Rays, tier.GlowColor.WithAlpha(0.12f));
			rays.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(1500f, 1500f), new Vector2(0f, CardY));
			if (!ctx.ReducedMotion)
			{
				var raysRect = rays.rectTransform;
				Tweens.Run(raysRect, 40f, k => raysRect.localRotation = Quaternion.Euler(0f, 0f, k * 360f), Ease.Linear, TweenChannel.Rotation).SetLoops(-1);
			}

			// Ambient fire/glints for good floats, behind the card.
			var fx = UiBuilder.Rect("Fx", content).gameObject.AddComponent<UiParticleGraphic>();
			fx.rectTransform.Stretch();
			fx.Setup(ctx.Theme.ParticleAtlas, ctx.Theme.ParticleAtlasColumns, ctx.Theme.ParticleAtlasRows, 300);
			fx.CountScale = ctx.ReducedMotion ? 0.35f : 1f;

			// Front + back share one flipping root.
			var flip = UiBuilder.Rect("Flip", content);
			flip.Place(new Vector2(0.5f, 0.5f), CardSize, new Vector2(0f, CardY));

			var card = CardView.Create(flip, ctx, character, CardSize);
			card.Root.Stretch();
			card.SetFloat(owned ? caught.BestFloat : null);
			card.SetCollected(true);
			card.SetIdle(true);
			card.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
			card.gameObject.AddComponent<TapArea>().Tapped = () =>
			{
				ctx.Audio.PlayClip(character.Voice);
				card.Body.PunchScale(0.1f, 0.45f);
				ctx.Haptics.Play(HapticStrength.Light);
				ctx.FxParticles.EmitAtWorld(card.Root.position, ParticlePresets.Sparkles(tier.GlowColor), 16);
			};

			var back = BuildBack(ctx, flip, character, tier, owned, caught, grade);
			back.gameObject.SetActive(false);

			// Under the card: one compact rarity line, then the creature's flavour text (the name is on the card's patch).
			float underCard = CardY - CardSize.y * 0.5f;
			var pill = UiBuilder.Image(content, "Rarity", ctx, UiSprite.Pill, tier.PrimaryColor.Darken(0.15f));
			pill.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(620f, 74f), new Vector2(0f, underCard - 66f));
			string gradeText = grade != null ? $"  ·  <color=#{ColorUtility.ToHtmlStringRGB(grade.Color.Lighten(0.3f))}>{grade.Name}</color>" : "";
			var rarity = UiBuilder.Text(pill.rectTransform, "Text", ctx, $"{tier.RarityLabel.ToUpperInvariant()}{gradeText}", 40f, TextStyle.TitleThin, palette.Cream);
			rarity.rectTransform.Stretch(20f, 20f, 0f, 2f);
			rarity.textWrappingMode = TextWrappingModes.NoWrap;
			rarity.AutoSize(26f);

			// A soft dark pool keeps the flavour text readable over the busy lobby behind the popup.
			var shade = UiBuilder.Image(content, "DescriptionShade", ctx, UiSprite.SoftGlow, palette.NightDeep.WithAlpha(0.75f));
			shade.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(1100f, 330f), new Vector2(0f, underCard - 200f));
			shade.transform.SetSiblingIndex(pill.transform.GetSiblingIndex());

			var description = UiBuilder.Text(content, "Description", ctx, character.Description, 48f, TextStyle.BodyOutline, palette.Cream.WithAlpha(0.95f), TextAlignmentOptions.Top);
			description.rectTransform.StretchX(0.5f, 150f, underCard - 130f, 100f, 1f);
			description.fontStyle = FontStyles.Italic;
			description.lineSpacing = -6f;
			description.AutoSize(32f);

			// Corner buttons: close (left) and info (right).
			var close = ChunkyButton.Create(content, ctx, "Close", new Vector2(112f, 112f), palette.Purple, null, UiSprite.IconClose);
			close.Root().Place(new Vector2(0f, 1f), new Vector2(112f, 112f), new Vector2(70f, -70f));
			close.Clicked = popup.Close;

			bool showingBack = false;
			var info = ChunkyButton.Create(content, ctx, "Info", new Vector2(112f, 112f), tier.PrimaryColor.Darken(0.1f), null, UiSprite.IconInfo);
			info.Root().Place(new Vector2(1f, 1f), new Vector2(112f, 112f), new Vector2(-70f, -70f));
			info.ClickSound = GameSound.CardFlip;
			info.Clicked = () =>
			{
				showingBack = !showingBack;
				Flip(ctx, flip, card.gameObject, back.gameObject, showingBack);
			};

			popup.gameObject.AddComponent<InspectFx>().Setup(fx, card, grade, character.Signature, tier.GlowColor, palette.Candy);

			ctx.Audio.PlayClip(character.Voice, 0.9f);
			if (!ctx.ReducedMotion)
			{
				flip.localScale = Vector3.one * 0.6f;
				flip.ScaleTo(1f, 0.5f, Ease.OutBackStrong);
				card.Body.Wobble(6f, 0.6f, 3);
			}

			return popup;
		}

		private static void Flip(UiContext ctx, RectTransform flip, GameObject front, GameObject back, bool toBack)
		{
			if (ctx.ReducedMotion)
			{
				front.SetActive(!toBack);
				back.SetActive(toBack);
				return;
			}

			Tweens.Run(flip, 0.13f, k => flip.localScale = new Vector3(1f - k, 1f, 1f), Ease.InQuad, TweenChannel.Scale).OnComplete(() =>
			{
				front.SetActive(!toBack);
				back.SetActive(toBack);
				Tweens.Run(flip, 0.25f, k => flip.localScale = new Vector3(k, 1f, 1f), Ease.OutBackStrong, TweenChannel.Scale);
			});
		}

		/// <summary>The card's back: the small print as label/value rows, vertically centred on the card.</summary>
		private static RectTransform BuildBack(UiContext ctx, RectTransform parent, CharacterDefinition character, RarityTierDefinition tier,
			bool owned, CaughtCreature caught, FloatGradeDefinition? grade)
		{
			var palette = ctx.Palette;
			var frame = UiBuilder.Image(parent, "Back", ctx, UiSprite.CardFrame, tier.PrimaryColor);
			var rect = frame.rectTransform;
			rect.Stretch();
			var face = UiBuilder.Image(rect, "Face", ctx, UiSprite.RoundedRect, palette.NightDeep.WithAlpha(0.96f));
			face.rectTransform.Stretch(BackInset, BackInset, BackInset, BackInset);
			var body = face.rectTransform;

			var rows = new List<(RectTransform Rect, float Height)>();
			RectTransform AddRow(string name, float height)
			{
				var row = UiBuilder.Rect(name, body);
				rows.Add((row, height));
				return row;
			}

			void Divider()
			{
				var line = UiBuilder.Image(AddRow("Divider", 44f), "Line", ctx, UiSprite.RoundedRectSmall, palette.Cream.WithAlpha(0.18f));
				line.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(380f, 4f));
			}

			RectTransform Stat(string label, string? value, Color valueColor)
			{
				var row = AddRow(label, 66f);
				var key = UiBuilder.Text(row, "Label", ctx, label, 36f, TextStyle.Body, palette.Muted, TextAlignmentOptions.Left);
				key.rectTransform.Stretch(52f, 52f, 0f, 0f);
				if (value != null)
				{
					var text = UiBuilder.Text(row, "Value", ctx, value, 40f, TextStyle.TitleThin, valueColor, TextAlignmentOptions.Right);
					text.rectTransform.Stretch(180f, 52f, 0f, 0f);
					text.textWrappingMode = TextWrappingModes.NoWrap;
					text.AutoSize(26f);
				}
				return row;
			}

			var title = UiBuilder.Text(AddRow("Name", 84f), "Text", ctx, character.DisplayName, 62f, TextStyle.Title, palette.Cream);
			title.rectTransform.Stretch(30f, 30f, 0f, 0f);
			title.textWrappingMode = TextWrappingModes.NoWrap;
			title.AutoSize(36f);

			var subtitle = UiBuilder.Text(AddRow("Tier", 52f), "Text", ctx, $"{tier.TierName} · {tier.RarityLabel}", 38f, TextStyle.TitleThin, tier.GlowColor.Lighten(0.2f));
			subtitle.rectTransform.Stretch(30f, 30f, 0f, 0f);
			subtitle.textWrappingMode = TextWrappingModes.NoWrap;
			subtitle.AutoSize(26f);

			Divider();

			var powerRow = Stat("Power", null, palette.Gold);
			var power = UiBuilder.Text(powerRow, "Value", ctx, character.Power.ToString(), 40f, TextStyle.TitleThin, palette.Gold, TextAlignmentOptions.Left);
			power.rectTransform.Stretch(176f, 0f, 0f, 0f);
			var bar = ProgressBar.Create(powerRow, ctx, new Vector2(190f, 30f), tier.PrimaryColor);
			((RectTransform)bar.transform).Place(new Vector2(1f, 0.5f), new Vector2(190f, 30f), new Vector2(-52f - 95f, 0f));
			bar.SetImmediate(character.Power / 99f);

			if (owned)
			{
				if (grade != null)
					Stat("Float", grade.Name, grade.Color.Lighten(0.3f));

				int points = CollectionRules.CharacterPoints(ctx.Pack, character, caught.BestFloat);
				int bonus = CollectionRules.FloatBonus(ctx.Pack, caught.BestFloat);
				Stat("Points", bonus > 0 ? $"+{points}  <size=70%>(+{bonus} float)</size>" : $"+{points}", palette.Slime);
				Stat("Caught", $"{caught.CatchCount}x", palette.Cream);
				Stat("Found", TimeFormat.ShortTime(caught.FirstCaughtAtUnixSeconds), palette.Cream);

				Divider();

				long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				long ready = caught.LastCaughtAtUnixSeconds + ctx.Pack.RecatchCooldownSeconds;
				bool canRecatch = now >= ready;
				var recatch = UiBuilder.Text(AddRow("Recatch", 56f), "Text", ctx,
					canRecatch ? "Ready to re-catch!" : $"Re-catch in {TimeFormat.Duration(ready - now)}",
					36f, TextStyle.TitleThin, canRecatch ? palette.Slime : palette.Muted);
				recatch.rectTransform.Stretch(30f, 30f, 0f, 0f);
				recatch.textWrappingMode = TextWrappingModes.NoWrap;
				recatch.AutoSize(26f);
			}

			// Centre the stack vertically on the card face.
			float total = 0f;
			foreach (var row in rows)
				total += row.Height;
			float y = -Mathf.Max(24f, (CardSize.y - BackInset * 2f - total) * 0.5f);
			foreach (var (row, height) in rows)
			{
				row.StretchX(1f, height, y, 0f, 1f);
				y -= height;
			}

			return rect;
		}

		/// <summary>
		/// Keeps the inspected card feeling alive: gentle flames for great floats, glints for metallic ones, and the
		/// character's signature flourish (the robot crackles with sparks and the odd heart).
		/// </summary>
		private sealed class InspectFx : MonoBehaviour
		{
			private UiParticleGraphic _fx = null!;
			private CardView _card = null!;
			private FloatGradeDefinition? _grade;
			private float _carry;
			private float _nextGlint;
			private CharacterSignature _signature;
			private Color _accent;
			private Color _heart;
			private float _nextSignature = 0.4f;

			public void Setup(UiParticleGraphic fx, CardView card, FloatGradeDefinition? grade, CharacterSignature signature, Color accent, Color heart)
			{
				_fx = fx;
				_card = card;
				_grade = grade;
				_signature = signature;
				_accent = accent;
				_heart = heart;
			}

			private void Update()
			{
				if (_card == null || !_card.gameObject.activeInHierarchy)
					return;

				var size = _card.Root.rect.size;
				if (_signature == CharacterSignature.Robot && Time.unscaledTime >= _nextSignature)
				{
					_nextSignature = Time.unscaledTime + UnityEngine.Random.Range(0.25f, 0.7f);
					var edge = new Vector3(Mathf.Sign(UnityEngine.Random.value - 0.5f) * size.x * 0.48f, UnityEngine.Random.Range(-0.45f, 0.45f) * size.y, 0f);
					_fx.EmitAtWorld(_card.Root.TransformPoint(edge), ParticlePresets.Electric(_accent), 3);
					if (UnityEngine.Random.value < 0.3f)
						_fx.EmitAtWorld(_card.Root.TransformPoint(new Vector3(0f, size.y * 0.3f, 0f)), ParticlePresets.Hearts(new[] { _heart, _accent }), 1);
				}

				if (_grade == null)
					return;

				if (_grade.FlameIntensity >= 0.4f)
				{
					_carry += (4f + 26f * _grade.FlameIntensity) * Time.unscaledDeltaTime;
					int count = (int)_carry;
					_carry -= count;
					if (count > 0)
					{
						var flames = ParticlePresets.Flames(_grade.FlameColor, _grade.FlameTipColor, _grade.FlameIntensity * 0.55f);
						flames.SpawnBox = new Vector2(size.x * 0.95f, 10f);
						_fx.EmitAtWorld(_card.Root.TransformPoint(new Vector3(0f, -size.y * 0.46f, 0f)), flames, count);
					}
				}

				if (_grade.Metallic >= 0.5f && Time.unscaledTime >= _nextGlint)
				{
					_nextGlint = Time.unscaledTime + Mathf.Lerp(1.6f, 0.5f, _grade.Metallic);
					var offset = new Vector3(UnityEngine.Random.Range(-0.45f, 0.45f) * size.x, UnityEngine.Random.Range(-0.45f, 0.45f) * size.y, 0f);
					_fx.EmitAtWorld(_card.Root.TransformPoint(offset), ParticlePresets.Glints(_grade.Color.Lighten(0.4f)), 1);
				}
			}
		}
	}

	public static class TimeFormat
	{
		public static string ShortTime(long unixSeconds)
		{
			var local = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime();
			bool today = local.Date == DateTimeOffset.Now.Date;
			return today ? local.ToString("HH:mm") : local.ToString("ddd HH:mm");
		}

		public static string Duration(long seconds)
		{
			if (seconds < 0)
				seconds = 0;
			var span = TimeSpan.FromSeconds(seconds);
			if (span.TotalHours >= 1)
				return $"{(int)span.TotalHours}h {span.Minutes:00}m";
			if (span.TotalMinutes >= 1)
				return $"{span.Minutes}m {span.Seconds:00}s";
			return $"{span.Seconds}s";
		}
	}
}
