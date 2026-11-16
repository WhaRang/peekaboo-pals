using System;
using System.Collections;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Screens;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Flow
{
	/// <summary>
	/// The "new character!" moment. Everything scales with the tier's <see cref="RevealStyle"/> and numbers from the
	/// config (build-up length, particle count, shake, haptic, stinger): a quick bouncy pop for commons up to a full
	/// cinematic with darkness, a charge-up, an explosion and fireworks for the legendary. Ends with the card flying
	/// into its slot on the collection wall.
	/// </summary>
	public static class RevealSequence
	{
		// Dark enough that nothing behind the reveal (lobby text, camera) competes with it.
		private static readonly float[] DimByLevel = { 0.84f, 0.87f, 0.9f, 0.96f };
		private const float CardY = 280f;

		/// <summary>What was caught: the float, its points, and for upgrades the float it beats.</summary>
		public readonly struct Catch
		{
			public float Float { get; }
			public int TierPoints { get; }
			public int FloatBonus { get; }
			/// <summary>Set for a re-catch that beat the previous best float.</summary>
			public float? PreviousBest { get; }
			public bool IsUpgrade => PreviousBest.HasValue;
			/// <summary>Score this catch adds: tier + bonus for a first catch, the bonus difference for an upgrade.</summary>
			public int Points { get; }

			public Catch(float value, int tierPoints, int floatBonus, int points, float? previousBest)
			{
				Float = value;
				TierPoints = tierPoints;
				FloatBonus = floatBonus;
				Points = points;
				PreviousBest = previousBest;
			}
		}
		private static readonly float[] RaysByLevel = { 0.16f, 0.26f, 0.4f, 0.6f };

		/// <param name="getSlot">Switches to the collection and returns the slot to fly into.</param>
		/// <param name="onLanded">Called when the card lands (fill the slot, update the header).</param>
		public static IEnumerator Play(UiContext ctx, CharacterDefinition character, Catch caught, Func<CardView?> getSlot, Action onLanded)
		{
			var tier = ctx.Tier(character);
			var palette = ctx.Palette;
			int level = (int)tier.RevealStyle;
			bool reduced = ctx.ReducedMotion;
			bool robot = character.Signature == CharacterSignature.Robot;
			var burstColors = new[] { tier.PrimaryColor, tier.SecondaryColor, tier.GlowColor, palette.Gold, palette.Candy, Color.white };

			ctx.Audio.SetMusicDucked(true);

			// ---------- Stage ----------
			var root = UiBuilder.Rect($"Reveal {character.Id}", ctx.PopupLayer);
			root.Stretch(-300f, -300f, -300f, -300f);

			var backdrop = UiBuilder.Image(root, "Backdrop", (Sprite?)null, palette.NightDeep.WithAlpha(0f), raycast: true);
			backdrop.rectTransform.Stretch();
			backdrop.FadeTo(DimByLevel[level], level == 3 ? 0.6f : 0.3f);

			var stage = UiBuilder.Rect("Stage", root);
			stage.Place(new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f));

			var rays = UiBuilder.Image(stage, "Rays", ctx, UiSprite.Rays, tier.GlowColor.WithAlpha(0f));
			rays.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(2000f, 2000f), new Vector2(0f, CardY));
			if (!reduced)
			{
				var raysRect = rays.rectTransform;
				Tweens.Run(raysRect, 24f, k => raysRect.localRotation = Quaternion.Euler(0f, 0f, -k * 360f), Ease.Linear, TweenChannel.Rotation).SetLoops(-1);
			}

			var aura = UiBuilder.Image(stage, "Aura", ctx, UiSprite.SoftGlow, tier.GlowColor.WithAlpha(0f));
			aura.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(1100f, 1300f), new Vector2(0f, CardY));

			var cardSize = new Vector2(420f, 571f);
			var card = CardView.Create(stage, ctx, character, cardSize);
			card.Root.Place(new Vector2(0.5f, 0.5f), cardSize, new Vector2(0f, CardY));
			// The quality is revealed after the character: neutral art first (or the old float for an upgrade).
			card.SetFloat(caught.PreviousBest);
			card.SetCollected(true);
			card.ShowBack(true);
			card.Glow.gameObject.SetActive(false);

			TextMeshProUGUI? teaser = null;
			if (level == 3 && !caught.IsUpgrade)
			{
				string line = string.IsNullOrEmpty(character.RevealTeaser) ? "Something LEGENDARY stirs…" : character.RevealTeaser;
				teaser = UiBuilder.Text(stage, "Teaser", ctx, line, 64f, TextStyle.Title, robot ? tier.GlowColor : palette.Gold);
				teaser.rectTransform.StretchX(0.5f, 120f, -330f, 60f);
				if (robot)
				{
					ctx.CoroutineHost.StartCoroutine(Typewriter(ctx, teaser, 0.04f));
				}
				else
				{
					teaser.alpha = 0f;
					teaser.FadeTo(1f, 0.6f);
				}
			}

			// ---------- Card appears ----------
			yield return Wait(level == 3 ? 0.5f : 0.1f);
			ctx.Audio.Play(GameSound.CardAppear);
			ctx.Haptics.Play(HapticStrength.Light);
			card.Root.localScale = Vector3.zero;
			card.Root.localRotation = Quaternion.Euler(0f, 0f, -18f);
			card.Root.ScaleTo(1f, 0.45f, Ease.OutBackStrong);
			card.Root.RotateTo(0f, 0.5f, Ease.OutBack);
			rays.FadeTo(RaysByLevel[level] * 0.5f, 0.5f);
			aura.FadeTo(0.25f, 0.5f);
			yield return Wait(0.45f);

			// ---------- Build-up ----------
			float buildUp = tier.BuildUpSeconds * (reduced ? 0.4f : 1f) * (caught.IsUpgrade ? 0.5f : 1f);
			if (level >= 1)
				ctx.Audio.Play(GameSound.BuildUp, 1f, level >= 2 ? 1f : 0.7f);
			if (level >= 2)
				ctx.Audio.Play(GameSound.Rumble, 0.8f, 0.9f);

			float elapsed = 0f;
			float nextEmit = 0f;
			float nextHaptic = 0f;
			float nextBeep = 0.1f;
			while (elapsed < buildUp)
			{
				elapsed += Time.unscaledDeltaTime;
				float k = Mathf.Clamp01(elapsed / buildUp);

				if (!reduced)
				{
					float shake = (2f + level * 2.5f) * k;
					card.Body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 55f) * shake);
					float grow = 1f + 0.08f * k * (level + 1) * 0.5f;
					card.Body.localScale = new Vector3(grow, grow, 1f);
				}

				aura.color = tier.GlowColor.WithAlpha(0.25f + 0.6f * k);
				rays.color = tier.GlowColor.WithAlpha(RaysByLevel[level] * (0.5f + 0.5f * k));

				if (elapsed >= nextEmit)
				{
					nextEmit = elapsed + 0.07f;
					var center = card.Root.position;
					if (level >= 2)
						ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Implode(tier.GlowColor, 520f), 2 + level);
					else if (level == 1)
						ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Glints(tier.GlowColor), 1);

					if (robot)
					{
						// Sparks crackle off the card's edges while it "boots up".
						var edge = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f) * cardSize.x, UnityEngine.Random.Range(-0.5f, 0.5f) * cardSize.y, 0f);
						if (UnityEngine.Random.value < 0.5f)
							edge.x = Mathf.Sign(edge.x) * cardSize.x * 0.5f;
						else
							edge.y = Mathf.Sign(edge.y) * cardSize.y * 0.5f;
						ctx.FxParticles.EmitAtWorld(card.Root.TransformPoint(edge), ParticlePresets.Electric(tier.GlowColor), 2 + (int)(3 * k));
						if (UnityEngine.Random.value < 0.4f)
							ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Pixels(tier.GlowColor), 3);
					}
				}

				if (robot && elapsed >= nextBeep)
				{
					// A rising "power-up" chirp sequence.
					nextBeep = elapsed + Mathf.Lerp(0.3f, 0.1f, k);
					ctx.Audio.Play(GameSound.ScanBeep, 1.1f + 0.9f * k, 0.35f);
				}

				if (robot && !reduced)
				{
					// Digital glitch: the card jumps a few pixels now and then.
					card.Body.anchoredPosition = UnityEngine.Random.value < 0.1f + 0.15f * k
						? new Vector2(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(-6f, 6f)) * k
						: Vector2.zero;
				}

				if (level >= 2 && elapsed >= nextHaptic)
				{
					nextHaptic = elapsed + Mathf.Lerp(0.35f, 0.12f, k);
					ctx.Haptics.Play(k > 0.6f ? HapticStrength.Medium : HapticStrength.Light);
				}

				if (level == 3)
					ctx.ScreenFx.Shake(4f + 14f * k, 0.12f);

				yield return null;
			}

			card.Body.localRotation = Quaternion.identity;
			card.Body.anchoredPosition = Vector2.zero;
			if (teaser != null)
				teaser.FadeTo(0f, 0.2f);

			// ---------- Flip ----------
			ctx.Audio.Play(GameSound.CardFlip);
			// Flip the whole card (frame, shadow and glow), not just its face.
			float flipOut = level >= 2 ? 0.14f : 0.1f;
			card.Body.localScale = Vector3.one;
			var flipped = card.Root;
			Tweens.Run(flipped, flipOut, k => flipped.localScale = new Vector3(1f - k, 1f, 1f), Ease.InQuad, TweenChannel.Scale);
			yield return Wait(flipOut);
			card.ShowBack(false);
			card.ArtRect.localScale = Vector3.zero;
			Tweens.Run(flipped, 0.28f, k => flipped.localScale = new Vector3(Mathf.LerpUnclamped(0f, 1f, k), 1f, 1f), Ease.OutBackStrong, TweenChannel.Scale);

			// ---------- POP! ----------
			var popCenter = card.Root.position;
			if (level >= 2)
				ctx.ScreenFx.Flash(Color.white, level == 3 ? 1f : 0.8f, level == 3 ? 0.7f : 0.45f);
			else if (level == 1)
				ctx.ScreenFx.Flash(tier.GlowColor, 0.35f, 0.3f);

			ctx.ScreenFx.Shake(tier.ScreenShake, 0.35f + 0.15f * level);
			ctx.Haptics.Play(tier.RevealHaptic);
			ctx.Audio.PlayClip(tier.RevealStinger);
			ctx.Audio.Play(GameSound.Sparkle, 1f + level * 0.05f, 0.8f);

			int burst = tier.BurstParticles;
			ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Confetti(burstColors), burst);
			ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Puff(tier.GlowColor.WithAlpha(0.6f)), 6 + level * 3);
			if (level >= 1)
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Sparkles(tier.GlowColor), burst / 2);
			if (level >= 2)
			{
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Swirl(tier.GlowColor, 90f), burst / 3);
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Shockwave(tier.GlowColor), 24);
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Ring(tier.GlowColor), 1);
				if (ctx.Pack.Theme.Bats)
					ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Bats(palette.Ink.Lighten(0.2f)), 6 + level * 2);
				else
					ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Hearts(burstColors), 6 + level * 2);
			}
			if (level == 3)
			{
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Candy(burstColors, ctx.Pack.Theme.Bats), 40);
				ctx.CoroutineHost.StartCoroutine(Fireworks(ctx, reduced ? 3 : 7, 2.4f));
			}
			if (robot)
			{
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Electric(tier.GlowColor), 70);
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Pixels(tier.GlowColor), 40);
				ctx.FxParticles.EmitAtWorld(popCenter, ParticlePresets.Hearts(new[] { palette.Candy, tier.GlowColor, Color.white }), reduced ? 8 : 18);
			}

			card.ArtRect.ScaleTo(1f, 0.8f, Ease.OutElastic);
			card.Glow.gameObject.SetActive(true);
			rays.FadeTo(RaysByLevel[level], 0.3f);
			aura.FadeTo(0.55f, 0.4f);
			Tweens.Delay(0.3f, () => ctx.Audio.PlayClip(character.Voice));

			// ---------- Banner ----------
			yield return Wait(0.2f);
			var banner = UiBuilder.Image(stage, "Banner", ctx, UiSprite.Banner, tier.PrimaryColor);
			banner.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(820f, 210f), new Vector2(0f, 750f));
			string bannerTitle = caught.IsUpgrade ? "UPGRADE!" : $"{tier.TierName.ToUpperInvariant()}!";
			var bannerText = UiBuilder.Text(banner.rectTransform, "Text", ctx, bannerTitle, 104f, TextStyle.Title, palette.Cream);
			bannerText.rectTransform.Stretch(60f, 60f, 10f, 36f);
			bannerText.AutoSize(60f);
			var bannerSub = UiBuilder.Text(banner.rectTransform, "Sub", ctx, caught.IsUpgrade ? "NEW BEST FLOAT" : tier.RarityLabel.ToUpperInvariant(), 42f, TextStyle.TitleThin, palette.Gold);
			bannerSub.rectTransform.StretchX(0f, 60f, 6f, 0f, 0f);
			banner.rectTransform.localScale = Vector3.one * 2.6f;
			banner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);
			banner.rectTransform.ScaleTo(1f, 0.4f, Ease.OutBackStrong);
			if (tier.RainbowShimmer)
			{
				Tweens.Run(banner, 2f, k => banner.color = Color.Lerp(palette.Gold, Color.HSVToRGB(k, 0.6f, 1f), 0.5f), Ease.Linear, TweenChannel.Color).SetLoops(-1);
			}

			// ---------- NEW! + points ----------
			yield return Wait(0.25f);
			card.ShowNewBadge(true, true, caught.IsUpgrade ? "BEST!" : "NEW!");
			ctx.Audio.Play(GameSound.NewBadge);

			var pointsText = UiBuilder.Text(stage, "Points", ctx, "+0", 110f, TextStyle.Title, palette.Gold);
			pointsText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(760f, 130f), new Vector2(0f, CardY));
			var pointsCounter = CountUpText.Attach(pointsText, ctx, v => $"+{v}");
			if (!caught.IsUpgrade)
			{
				pointsText.rectTransform.MoveTo(new Vector2(0f, -70f), 0.45f, Ease.OutBackStrong);
				pointsText.transform.ScaleFromTo(0.3f, 1f, 0.45f, Ease.OutBackStrong);
				yield return Wait(0.2f);
				pointsCounter.AnimateTo(caught.TierPoints, 0.4f + 0.1f * level);
			}
			else
			{
				pointsText.alpha = 0f;
				pointsText.rectTransform.anchoredPosition = new Vector2(0f, -70f);
			}

			// ---------- Float ----------
			yield return Wait(0.45f);
			var badge = FloatReveal.Create(ctx, stage, new Vector2(0f, -205f));
			yield return FloatReveal.Play(ctx, badge, caught.Float, card.Root);
			card.SetFloat(caught.Float);
			card.Body.PunchScale(0.12f, 0.4f, 2);

			TextMeshProUGUI? bonusLabel = null;
			if (caught.IsUpgrade)
			{
				pointsText.alpha = 1f;
				pointsText.transform.ScaleFromTo(0.3f, 1f, 0.4f, Ease.OutBackStrong);
				if (caught.Points > 0)
					pointsCounter.AnimateTo(caught.Points, 0.6f);
				else
					pointsText.text = "NEW BEST!";
				bonusLabel = AddSmallLabel(ctx, stage, $"was {FloatStyle.Short(caught.PreviousBest!.Value)}", -380f);
			}
			else if (caught.FloatBonus > 0)
			{
				pointsCounter.AnimateTo(caught.TierPoints + caught.FloatBonus, 0.6f);
				bonusLabel = AddSmallLabel(ctx, stage, $"incl. +{caught.FloatBonus} float bonus", -380f);
			}

			// ---------- Info + collect (on its own dark panel so it never fights with what's behind) ----------
			yield return Wait(0.45f);
			var info = UiBuilder.Image(stage, "Info", ctx, UiSprite.RoundedRect, palette.NightDeep.WithAlpha(0.92f)).rectTransform;
			info.Place(new Vector2(0.5f, 0.5f), new Vector2(1000f, 540f), new Vector2(0f, -682f));
			var infoGroup = info.Group();
			infoGroup.alpha = 0f;
			infoGroup.FadeTo(1f, 0.3f);
			var infoRim = UiBuilder.Image(info, "Rim", ctx, UiSprite.Pill, tier.PrimaryColor);
			infoRim.rectTransform.StretchX(1f, 10f, -10f, 80f, 1f);

			var name = UiBuilder.Text(info, "Name", ctx, character.DisplayName, 84f, TextStyle.Title, palette.Cream);
			name.rectTransform.StretchX(1f, 100f, -26f, 30f, 1f);
			name.textWrappingMode = TextWrappingModes.NoWrap;
			name.AutoSize(50f);

			var grade = ctx.Pack.GetFloatGrade(caught.Float);
			var stats = UiBuilder.Text(info, "Stats", ctx, $"Power {character.Power}  ·  {tier.RarityLabel}  ·  {grade.Name}", 44f, TextStyle.BodyOutline, tier.PrimaryColor.Lighten(0.3f));
			stats.rectTransform.StretchX(1f, 64f, -126f, 30f, 1f);
			stats.AutoSize(30f);

			var description = UiBuilder.Text(info, "Description", ctx, character.Description, 40f, TextStyle.Body, palette.Cream.WithAlpha(0.9f));
			description.rectTransform.StretchX(1f, 110f, -192f, 50f, 1f);
			description.AutoSize(28f);

			string addedText = caught.IsUpgrade ? "Your best float just got better!" : "Added to your collection!";
			var added = UiBuilder.Text(info, "Added", ctx, addedText, 50f, TextStyle.TitleThin, palette.Slime);
			added.rectTransform.StretchX(1f, 70f, -308f, 30f, 1f);
			added.AutoSize(32f);
			added.transform.ScaleFromTo(0.6f, 1f, 0.4f, Ease.OutBackStrong);

			bool collected = false;
			var collect = ChunkyButton.Create(info, ctx, "Collect", new Vector2(540f, 150f), palette.Slime.Darken(0.25f), "Collect!", null, 70f);
			collect.Root().Place(new Vector2(0.5f, 1f), new Vector2(540f, 150f), new Vector2(0f, -452f));
			collect.ClickSound = GameSound.PopupClose;
			collect.Clicked = () => collected = true;
			collect.StartIdlePulse(0.05f, 1.1f);

			float waited = 0f;
			while (!collected && waited < 25f)
			{
				waited += Time.unscaledDeltaTime;
				yield return null;
			}

			badge.Root.ScaleTo(0f, 0.2f, Ease.InBack);
			if (bonusLabel != null)
				bonusLabel.FadeTo(0f, 0.2f);

			// ---------- Fly into the slot ----------
			infoGroup.FadeTo(0f, 0.2f);
			banner.rectTransform.ScaleTo(0f, 0.25f, Ease.InBack);
			pointsText.FadeTo(0f, 0.25f);
			card.ShowNewBadge(false, false);

			var slot = getSlot();
			yield return null;
			yield return null;

			backdrop.FadeTo(0f, 0.5f);
			rays.FadeTo(0f, 0.3f);
			aura.FadeTo(0f, 0.3f);
			backdrop.raycastTarget = false;
			ctx.Audio.Play(GameSound.CardWhoosh);

			if (slot != null)
			{
				Vector3 from = card.Root.position;
				Vector3 to = slot.Root.position;
				Vector3 control = Vector3.Lerp(from, to, 0.5f) + (Vector3)(Vector2.up * Mathf.Abs(to.y - from.y) * 0.4f) + Vector3.right * (to.x - from.x) * 0.3f;
				float targetScale = slot.Root.rect.width / cardSize.x;
				float duration = reduced ? 0.35f : 0.75f;
				float t = 0f;
				float nextTrail = 0f;

				while (t < duration)
				{
					t += Time.unscaledDeltaTime;
					float k = Easing.Evaluate(Ease.InOutCubic, Mathf.Clamp01(t / duration));
					Vector3 a = Vector3.Lerp(from, control, k);
					Vector3 b = Vector3.Lerp(control, to, k);
					card.Root.position = Vector3.Lerp(a, b, k);
					float scale = Mathf.Lerp(1f, targetScale, k);
					card.Root.localScale = new Vector3(scale, scale, 1f);
					card.Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * -16f);

					if (t >= nextTrail)
					{
						nextTrail = t + 0.03f;
						ctx.FxParticles.EmitAtWorld(card.Root.position, ParticlePresets.Glints(tier.GlowColor), 2);
					}

					yield return null;
				}

				ctx.Audio.Play(GameSound.SlotFill);
				ctx.Haptics.Play(HapticStrength.Light);
				ctx.FxParticles.EmitAtWorld(to, ParticlePresets.Sparkles(tier.GlowColor), 16 + level * 8);
				ctx.FxParticles.EmitAtWorld(to, ParticlePresets.Ring(tier.GlowColor), 1);
			}

			UnityEngine.Object.Destroy(root.gameObject);
			onLanded();
			slot?.PlayFill();
			ctx.Audio.SetMusicDucked(false);
		}

		/// <summary>Types the label out one character at a time with a soft blip, like a terminal.</summary>
		private static IEnumerator Typewriter(UiContext ctx, TextMeshProUGUI label, float perCharacter)
		{
			label.ForceMeshUpdate();
			int total = label.textInfo.characterCount;
			for (int i = 0; i <= total; i++)
			{
				if (label == null)
					yield break;
				label.maxVisibleCharacters = i;
				if (i % 2 == 1)
					ctx.Audio.Play(GameSound.PointsTick, 1.6f, 0.6f);
				yield return Wait(perCharacter);
			}
		}

		private static TextMeshProUGUI AddSmallLabel(UiContext ctx, RectTransform stage, string text, float y)
		{
			var label = UiBuilder.Text(stage, "Small", ctx, text, 36f, TextStyle.BodyOutline, ctx.Palette.Gold.Lighten(0.3f));
			label.rectTransform.StretchX(0.5f, 52f, y, 60f);
			label.transform.ScaleFromTo(0.5f, 1f, 0.35f, Ease.OutBackStrong);
			return label;
		}

		public static IEnumerator Fireworks(UiContext ctx, int count, float duration)
		{
			var palette = ctx.Palette;
			var colors = new[] { palette.Gold, palette.Candy, palette.Slime, palette.Pumpkin, palette.PurpleLight, Color.white };
			var area = ctx.FxLayer.rect;

			for (int i = 0; i < count; i++)
			{
				var position = new Vector2(UnityEngine.Random.Range(-0.38f, 0.38f) * area.width, UnityEngine.Random.Range(0.05f, 0.4f) * area.height);
				var color = colors[UnityEngine.Random.Range(0, colors.Length)];
				ctx.FxParticles.Emit(position, ParticlePresets.Firework(color), 46);
				ctx.FxParticles.Emit(position, ParticlePresets.Ring(color.WithAlpha(0.7f)), 1);
				ctx.Audio.Play(GameSound.Firework, UnityEngine.Random.Range(0.85f, 1.2f), 0.7f);
				yield return Wait(duration / count * UnityEngine.Random.Range(0.6f, 1.4f));
			}
		}

		public static WaitForSecondsRealtime Wait(float seconds) => new(seconds);
	}
}
