using System;
using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Popups;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Flow
{
	/// <summary>Full-collection party: the whole roster of the collection orbiting a trophy banner, bonus count-up, fireworks, fanfare.</summary>
	public static class CompletionCelebration
	{
		public static IEnumerator Play(UiContext ctx, int bonusPoints, long durationSeconds, Action showResults)
		{
			var palette = ctx.Palette;
			bool reduced = ctx.ReducedMotion;
			var colors = new[] { palette.Gold, palette.Candy, palette.Slime, palette.Pumpkin, palette.PurpleLight, Color.white };

			ctx.Audio.SetMusicDucked(true);
			ctx.Audio.Play(GameSound.CompletionFanfare);
			ctx.Haptics.Play(HapticStrength.Epic);

			var root = UiBuilder.Rect("Completion", ctx.PopupLayer);
			root.Stretch(-300f, -300f, -300f, -300f);
			var group = root.Group();

			var backdrop = UiBuilder.Image(root, "Backdrop", (Sprite?)null, palette.NightDeep.WithAlpha(0f), raycast: true);
			backdrop.rectTransform.Stretch();
			backdrop.FadeTo(0.9f, 0.4f);

			var stage = UiBuilder.Rect("Stage", root);
			stage.Place(new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f));

			var rays = UiBuilder.Image(stage, "Rays", ctx, UiSprite.Rays, palette.Gold.WithAlpha(0.45f));
			rays.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(2200f, 2200f), new Vector2(0f, 200f));
			if (!reduced)
			{
				var raysRect = rays.rectTransform;
				Tweens.Run(raysRect, 30f, k => raysRect.localRotation = Quaternion.Euler(0f, 0f, k * 360f), Ease.Linear, TweenChannel.Rotation).SetLoops(-1);
			}

			// Roster orbit
			var orbit = UiBuilder.Rect("Orbit", stage);
			orbit.Place(new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), new Vector2(0f, 200f));
			var characters = ctx.Pack.Characters;
			var icons = new List<RectTransform>();
			for (int i = 0; i < characters.Count; i++)
			{
				var character = characters[i];
				var bubble = UiBuilder.Image(orbit, character.Id, ctx, UiSprite.Circle, ctx.Tier(character).PrimaryColor);
				float startAngle = i * Mathf.PI * 2f / characters.Count;
				bubble.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(170f, 170f), new Vector2(Mathf.Cos(startAngle) * 370f, Mathf.Sin(startAngle) * 300f));
				var art = UiBuilder.Image(bubble.rectTransform, "Art", character.Art, Color.white);
				art.preserveAspect = true;
				art.rectTransform.Stretch(10f, 10f, 10f, 10f);
				bubble.rectTransform.localScale = Vector3.zero;
				bubble.rectTransform.ScaleTo(1f, 0.5f, Ease.OutBackStrong).SetDelay(0.3f + i * 0.08f);
				icons.Add(bubble.rectTransform);
			}

			var trophy = UiBuilder.Image(stage, "Trophy", ctx, UiSprite.IconTrophy, Color.white);
			trophy.preserveAspect = true;
			trophy.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(300f, 300f), new Vector2(0f, 200f));
			trophy.rectTransform.ScaleFromTo(0f, 1f, 0.7f, Ease.OutElastic);

			var banner = UiBuilder.Image(stage, "Banner", ctx, UiSprite.Banner, palette.Candy);
			banner.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(980f, 250f), new Vector2(0f, 720f));
			var title = UiBuilder.Text(banner.rectTransform, "Text", ctx, "COLLECTION COMPLETE!", 88f, TextStyle.Title, palette.Cream);
			title.rectTransform.Stretch(70f, 70f, 20f, 40f);
			title.AutoSize(50f);
			banner.rectTransform.localScale = Vector3.one * 3f;
			banner.rectTransform.ScaleTo(1f, 0.5f, Ease.OutBackStrong);
			Tweens.Run(banner, 2.4f, k => banner.color = Color.Lerp(palette.Candy, Color.HSVToRGB(k, 0.55f, 1f), 0.45f), Ease.Linear, TweenChannel.Color).SetLoops(-1);

			var subtitle = UiBuilder.Text(stage, "Subtitle", ctx, $"You found all {characters.Count} {ctx.Pack.Texts.Friends}!", 56f, TextStyle.BodyOutline, palette.Cream);
			subtitle.rectTransform.StretchX(0.5f, 90f, -260f, 60f);

			var bonus = UiBuilder.Text(stage, "Bonus", ctx, "+0", 150f, TextStyle.Title, palette.Gold);
			bonus.rectTransform.StretchX(0.5f, 190f, -400f, 60f);
			var bonusCounter = CountUpText.Attach(bonus, ctx, v => $"+{v} BONUS");

			var time = UiBuilder.Text(stage, "Time", ctx, $"Hunt time: {TimeFormat.Duration(durationSeconds)}", 58f, TextStyle.TitleThin, palette.Slime);
			time.rectTransform.StretchX(0.5f, 90f, -540f, 60f);
			time.alpha = 0f;

			bool done = false;
			var button = ChunkyButton.Create(stage, ctx, "Results", new Vector2(700f, 170f), palette.Pumpkin, "See my results!", UiSprite.IconResults, 64f);
			button.Root().Place(new Vector2(0.5f, 0.5f), new Vector2(700f, 170f), new Vector2(0f, -720f));
			button.Clicked = () => done = true;
			button.Root().localScale = Vector3.zero;

			// Party!
			var center = trophy.rectTransform.position;
			ctx.ScreenFx.Flash(Color.white, 0.9f, 0.6f);
			ctx.ScreenFx.Shake(26f, 0.7f);
			ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Confetti(colors), 160);
			ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Candy(colors, ctx.Pack.Theme.Bats), 50);
			ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Shockwave(palette.Gold), 30);
			ctx.CoroutineHost.StartCoroutine(RevealSequence.Fireworks(ctx, reduced ? 4 : 12, 4f));

			yield return RevealSequence.Wait(0.6f);
			bonusCounter.AnimateTo(bonusPoints, 1f);
			yield return RevealSequence.Wait(0.9f);
			time.FadeTo(1f, 0.4f);
			time.transform.ScaleFromTo(0.6f, 1f, 0.4f, Ease.OutBackStrong);
			button.Root().ScaleTo(1f, 0.45f, Ease.OutBackStrong).OnComplete(() => button.StartIdlePulse());

			float elapsed = 0f;
			float nextConfetti = 1.5f;
			while (!done && elapsed < 40f)
			{
				elapsed += Time.unscaledDeltaTime;

				if (!reduced)
				{
					float baseAngle = elapsed * 0.45f;
					for (int i = 0; i < icons.Count; i++)
					{
						float angle = baseAngle + i * Mathf.PI * 2f / icons.Count;
						icons[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * 370f, Mathf.Sin(angle) * 300f);
						icons[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 2f + i) * 8f);
					}
				}
				else
				{
					for (int i = 0; i < icons.Count; i++)
					{
						float angle = i * Mathf.PI * 2f / icons.Count;
						icons[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * 370f, Mathf.Sin(angle) * 300f);
					}
				}

				if (elapsed >= nextConfetti && !reduced)
				{
					nextConfetti = elapsed + 2.2f;
					var top = new Vector3(center.x, center.y, center.z);
					ctx.FxParticles.EmitAtWorld(top, ParticlePresets.Confetti(colors), 40);
				}

				yield return null;
			}

			group.FadeTo(0f, 0.3f);
			yield return RevealSequence.Wait(0.3f);
			UnityEngine.Object.Destroy(root.gameObject);
			ctx.Audio.SetMusicDucked(false);
			showResults();
		}
	}
}
