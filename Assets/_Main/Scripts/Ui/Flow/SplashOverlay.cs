using System;
using System.Collections;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Flow
{
	/// <summary>Intro: the logo drops in with a bounce, confetti and sparkles burst, "tap to start" breathes. Tap (or wait) to enter the lobby.</summary>
	public sealed class SplashOverlay : MonoBehaviour, IPointerClickHandler
	{
		private bool _tapped;

		public static IEnumerator Play(UiContext ctx, Transform parent, Action onDone)
		{
			var palette = ctx.Palette;
			var root = UiBuilder.Rect("Splash", parent);
			root.Stretch();
			var group = root.Group();
			var backdrop = root.gameObject.AddComponent<Image>();
			backdrop.color = palette.NightDeep.WithAlpha(0.6f);
			var splash = root.gameObject.AddComponent<SplashOverlay>();

			var glow = UiBuilder.Image(root, "Glow", ctx, UiSprite.SoftGlow, palette.Pumpkin.WithAlpha(0.4f));
			glow.rectTransform.Place(new Vector2(0.5f, 0.58f), new Vector2(1400f, 1000f));

			var logo = UiBuilder.Image(root, "Logo", ctx, UiSprite.Logo, Color.white);
			logo.preserveAspect = true;
			logo.rectTransform.Place(new Vector2(0.5f, 0.58f), new Vector2(940f, 330f), new Vector2(0f, 900f));

			var tagline = UiBuilder.Text(root, "Tagline", ctx, "Catch the pals hiding at every party", 54f, TextStyle.BodyOutline, palette.Cream);
			tagline.rectTransform.StretchX(0.58f, 80f, -240f, 60f);
			tagline.alpha = 0f;

			var tap = UiBuilder.Text(root, "Tap", ctx, "TAP TO START", 70f, TextStyle.Title, palette.Slime);
			tap.rectTransform.StretchX(0.25f, 110f, 0f, 60f);
			tap.alpha = 0f;

			yield return null;

			logo.rectTransform.MoveTo(Vector2.zero, ctx.ReducedMotion ? 0.2f : 0.8f, Ease.OutBounce);
			yield return RevealSequence.Wait(0.55f);
			ctx.Audio.Play(GameSound.CardAppear, 0.8f);
			ctx.Haptics.Play(HapticStrength.Light);
			logo.rectTransform.PunchScale(0.15f, 0.6f);
			ctx.FxParticles.EmitAtWorld(logo.rectTransform.position, ParticlePresets.Confetti(new[] { palette.Candy, palette.Gold, palette.Slime, palette.PurpleLight }), 40);
			ctx.FxParticles.EmitAtWorld(logo.rectTransform.position, ParticlePresets.Sparkles(palette.Gold), 24);
			tagline.FadeTo(1f, 0.5f);

			yield return RevealSequence.Wait(0.5f);
			tap.FadeTo(1f, 0.4f);
			if (!ctx.ReducedMotion)
				Tweens.Run(tap.transform, 0.7f, k => tap.transform.localScale = Vector3.one * (1f + 0.06f * k), Ease.InOutSine).SetLoops(-1, true);

			float waited = 0f;
			while (!splash._tapped && waited < 6f)
			{
				waited += Time.unscaledDeltaTime;
				if (!ctx.ReducedMotion)
					logo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.6f) * 2.2f);
				yield return null;
			}

			ctx.Audio.Play(GameSound.ButtonPress);
			ctx.Audio.StartMusic();
			_Main.Scripts.Services.Web.WebPlatform.KeepAwake();
			logo.rectTransform.ScaleTo(1.2f, 0.3f, Ease.InBack);
			group.FadeTo(0f, 0.35f);
			yield return RevealSequence.Wait(0.35f);
			Destroy(root.gameObject);
			onDone();
		}

		public void OnPointerClick(PointerEventData eventData) => _tapped = true;
	}
}
