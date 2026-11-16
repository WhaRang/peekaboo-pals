using System.Collections;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Flow
{
	/// <summary>
	/// The float moment: a chip whose number rolls down like a slot machine and lands on the caught float, then the
	/// grade name slams in and the chip catches fire. The rarer the grade (config: flame intensity), the longer the
	/// roll, the bigger the fire, the louder the sound and haptic; the top grade adds a flash and a ring of fire.
	/// </summary>
	public static class FloatReveal
	{
		public sealed class Badge
		{
			public RectTransform Root = null!;
			public TextMeshProUGUI Value = null!;
			public TextMeshProUGUI Grade = null!;
			public Image Fill = null!;
			/// <summary>Own particle layer drawn behind the chip, so the fire never hides the number.</summary>
			public UiParticleGraphic Flames = null!;
		}

		public static Badge Create(UiContext ctx, RectTransform parent, Vector2 position, float width = 520f)
		{
			var palette = ctx.Palette;
			var flames = UiBuilder.Rect("FloatFlames", parent).gameObject.AddComponent<UiParticleGraphic>();
			flames.rectTransform.Stretch(-400f, -400f, -400f, -400f);
			flames.Setup(ctx.Theme.ParticleAtlas, ctx.Theme.ParticleAtlasColumns, ctx.Theme.ParticleAtlasRows, 700);
			flames.CountScale = ctx.ReducedMotion ? 0.35f : 1f;

			var root = UiBuilder.Rect("FloatBadge", parent);
			root.Place(new Vector2(0.5f, 0.5f), new Vector2(width, 128f), position);

			var shadow = UiBuilder.Image(root, "Shadow", ctx, UiSprite.Pill, palette.Ink.WithAlpha(0.8f));
			shadow.rectTransform.Stretch(-8f, -8f, -4f, -14f);
			var fill = UiBuilder.Image(root, "Fill", ctx, UiSprite.ChipMetal, palette.Muted);
			fill.rectTransform.Stretch();

			var label = UiBuilder.Text(root, "Label", ctx, "FLOAT", 34f, TextStyle.TitleThin, palette.Cream, TextAlignmentOptions.Left);
			label.rectTransform.Stretch(34f, 0f, 0f, 0f);
			label.rectTransform.anchorMax = new Vector2(0.4f, 1f);

			var value = UiBuilder.Text(root, "Value", ctx, "0.9999", 74f, TextStyle.Title, palette.Cream, TextAlignmentOptions.Right);
			value.rectTransform.Stretch(0f, 34f, 0f, 6f);
			value.rectTransform.anchorMin = new Vector2(0.32f, 0f);
			value.textWrappingMode = TextWrappingModes.NoWrap;

			var grade = UiBuilder.Text(root, "Grade", ctx, "", 46f, TextStyle.Title, palette.Cream);
			grade.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(width + 200f, 64f), new Vector2(0f, -44f));
			grade.textWrappingMode = TextWrappingModes.NoWrap;
			grade.alpha = 0f;

			return new Badge { Root = root, Value = value, Grade = grade, Fill = fill, Flames = flames };
		}

		/// <param name="fireAround">Optional rect (the card) that gets a ring of fire for the top grades.</param>
		public static IEnumerator Play(UiContext ctx, Badge badge, float value, RectTransform? fireAround = null)
		{
			var grade = ctx.Pack.GetFloatGrade(value);
			float intensity = grade.FlameIntensity;
			bool reduced = ctx.ReducedMotion;

			badge.Root.localScale = Vector3.zero;
			badge.Root.ScaleTo(1f, 0.35f, Ease.OutBackStrong);
			ctx.Audio.Play(GameSound.CardAppear, 1.2f, 0.6f);
			yield return RevealSequence.Wait(0.3f);

			// Slot-machine roll: fast at first, crawling near the end. Rarer floats roll longer for suspense.
			float duration = (reduced ? 0.5f : 1f) * (0.9f + 1.3f * intensity);
			float start = Mathf.Max(value + 0.3f, 0.9999f);
			float t = 0f;
			float nextTick = 0f;
			while (t < duration)
			{
				t += Time.unscaledDeltaTime;
				float k = Easing.Evaluate(Ease.OutCubic, Mathf.Clamp01(t / duration));
				float shown = Mathf.Lerp(start, value, k);
				badge.Value.text = FloatStyle.Short(shown);
				var rolling = ctx.Pack.GetFloatGrade(shown);
				badge.Fill.color = Color.Lerp(badge.Fill.color, rolling.Color, 0.25f);

				if (t >= nextTick)
				{
					nextTick = t + Mathf.Lerp(0.04f, 0.16f, k);
					ctx.Audio.Play(GameSound.PointsTick, 1.6f - 0.8f * k, 0.5f);
				}

				if (!reduced && intensity > 0.4f)
					badge.Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 60f) * 2.5f * k * intensity);

				yield return null;
			}

			badge.Root.localRotation = Quaternion.identity;
			badge.Value.text = FloatStyle.Short(value);
			badge.Fill.color = grade.Color;
			badge.Fill.sprite = ctx.Sprite(grade.Metallic >= 0.5f ? UiSprite.ChipMetal : UiSprite.Pill);
			badge.Value.color = ctx.Palette.Cream;
			badge.Root.PunchScale(0.25f + 0.25f * intensity, 0.5f, 2);

			badge.Grade.text = grade.Name.ToUpperInvariant() + (intensity >= 0.95f ? "!!" : intensity >= 0.45f ? "!" : "");
			badge.Grade.color = grade.Color.Lighten(0.2f);
			badge.Grade.alpha = 1f;
			badge.Grade.transform.ScaleFromTo(2.4f, 1f, 0.35f, Ease.OutBackStrong);

			// Landing feedback scales with rarity.
			var center = badge.Root.position;
			if (intensity <= 0.01f)
			{
				ctx.Audio.Play(GameSound.Duplicate, 1.1f, 0.5f);
				ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Smoke(grade.FlameTipColor.WithAlpha(0.6f)), 8);
			}
			else
			{
				ctx.Audio.Play(GameSound.FloatFire, 0.9f + 0.3f * intensity, 0.4f + 0.6f * intensity);
				ctx.Haptics.Play(intensity >= 0.95f ? HapticStrength.Epic : intensity >= 0.7f ? HapticStrength.Heavy : intensity >= 0.4f ? HapticStrength.Medium : HapticStrength.Light);
				ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Sparkles(grade.FlameTipColor), (int)(8 + 30 * intensity));
			}

			if (intensity >= 0.7f)
				ctx.Audio.Play(GameSound.FloatJackpot);
			if (intensity >= 0.95f)
			{
				ctx.ScreenFx.Flash(grade.Color, 0.8f, 0.6f);
				ctx.ScreenFx.Shake(28f, 0.7f);
				ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Ring(grade.FlameTipColor), 1);
				ctx.FxParticles.EmitAtWorld(center, ParticlePresets.Shockwave(grade.FlameColor), 26);
			}
			else if (intensity >= 0.45f)
			{
				ctx.ScreenFx.Shake(6f + 14f * intensity, 0.4f);
			}

			if (intensity > 0.01f)
				ctx.CoroutineHost.StartCoroutine(Burn(ctx, badge, grade, fireAround));

			yield return RevealSequence.Wait(0.35f);
		}

		/// <summary>Keeps the chip (and for top grades the card) on fire for a while after landing.</summary>
		private static IEnumerator Burn(UiContext ctx, Badge badge, FloatGradeDefinition grade, RectTransform? fireAround)
		{
			float intensity = grade.FlameIntensity;
			float duration = 0.8f + 2.2f * intensity;
			float rate = 8f + 90f * intensity;
			var flames = ParticlePresets.Flames(grade.FlameColor, grade.FlameTipColor, intensity);
			flames.SpawnBox = new Vector2(badge.Root.rect.width * 0.9f, 30f);
			var ring = ParticlePresets.Flames(grade.FlameColor, grade.FlameTipColor, intensity * 0.7f);
			float carry = 0f;
			float t = 0f;

			while (t < duration && badge.Root != null && badge.Flames != null)
			{
				float dt = Time.unscaledDeltaTime;
				t += dt;
				float fade = 1f - Mathf.Clamp01((t - duration * 0.6f) / (duration * 0.4f));
				carry += rate * fade * dt;
				int count = (int)carry;
				carry -= count;

				if (count > 0)
				{
					var bottom = badge.Root.TransformPoint(new Vector3(0f, -badge.Root.rect.height * 0.3f, 0f));
					badge.Flames.EmitAtWorld(bottom, flames, count);

					if (fireAround != null && intensity >= 0.7f)
					{
						var size = fireAround.rect.size;
						ring.SpawnBox = new Vector2(size.x * 1.05f, 20f);
						var cardBottom = fireAround.TransformPoint(new Vector3(0f, -size.y * 0.5f, 0f));
						badge.Flames.EmitAtWorld(cardBottom, ring, Mathf.Max(1, count / 2));
					}
				}

				if (grade.FlameIntensity >= 0.95f && badge.Fill != null)
					badge.Fill.color = FloatStyle.AnimatedColor(grade, Time.unscaledTime);

				yield return null;
			}
		}
	}
}
