using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Widgets
{
	/// <summary>
	/// The living night behind every screen: gradient sky, glowing moon, hills, twinkling stars and drifting fog, dressed
	/// by the collection on screen (<see cref="ApplyTheme"/>): its own sky/scenery art, and embers, snow, confetti or hearts
	/// drifting by, plus the occasional bat for Halloween. Calmer (but not frozen) in reduced-motion mode.
	/// </summary>
	public sealed class AmbientBackground : MonoBehaviour
	{
		private UiContext _ctx = null!;
		private UiParticleGraphic _particles = null!;
		private RectTransform _moon = null!;
		private Image _moonGlow = null!;
		private Image _sky = null!;
		private Image _hills = null!;
		private Sprite? _defaultSky;
		private Sprite? _defaultHills;
		private PackTheme? _theme;
		private Color[] _confettiColors = { Color.white };
		private float _nextTwinkle;
		private float _nextFog;
		private float _nextEmber;
		private float _nextBat;

		public static AmbientBackground Create(Transform parent, UiContext ctx)
		{
			var root = UiBuilder.Rect("Background", parent);
			root.Stretch();
			var background = root.gameObject.AddComponent<AmbientBackground>();
			background.Build(ctx);
			return background;
		}

		private void Build(UiContext ctx)
		{
			_ctx = ctx;
			var palette = ctx.Palette;
			var rect = (RectTransform)transform;

			_sky = UiBuilder.Image(rect, "Sky", ctx, UiSprite.Background, Color.white);
			_sky.rectTransform.Stretch();
			_defaultSky = _sky.sprite;
			if (_sky.sprite == null)
				_sky.color = palette.Night;

			_moonGlow = UiBuilder.Image(rect, "MoonGlow", ctx, UiSprite.SoftGlow, palette.Gold.WithAlpha(0.35f));
			_moonGlow.rectTransform.Place(new Vector2(1f, 1f), new Vector2(760f, 760f), new Vector2(-170f, -330f));

			var moon = UiBuilder.Image(rect, "Moon", ctx, UiSprite.Moon, Color.white);
			moon.preserveAspect = true;
			_moon = moon.rectTransform;
			_moon.Place(new Vector2(1f, 1f), new Vector2(300f, 300f), new Vector2(-170f, -330f));

			_particles = UiBuilder.Rect("Particles", rect).gameObject.AddComponent<UiParticleGraphic>();
			_particles.rectTransform.Stretch();
			_particles.Setup(ctx.Theme.ParticleAtlas, ctx.Theme.ParticleAtlasColumns, ctx.Theme.ParticleAtlasRows, 220);

			_hills = UiBuilder.Image(rect, "Hills", ctx, UiSprite.Hills, Color.white);
			_hills.rectTransform.anchorMin = Vector2.zero;
			_hills.rectTransform.anchorMax = new Vector2(1f, 0f);
			_hills.rectTransform.pivot = new Vector2(0.5f, 0f);
			_hills.rectTransform.sizeDelta = new Vector2(0f, 560f);
			_hills.rectTransform.anchoredPosition = Vector2.zero;
			_defaultHills = _hills.sprite;
			if (_hills.sprite == null)
				_hills.gameObject.SetActive(false);

			var vignette = UiBuilder.Image(rect, "Vignette", ctx, UiSprite.Vignette, Color.white.WithAlpha(0.85f));
			vignette.rectTransform.Stretch();
			if (vignette.sprite == null)
				vignette.gameObject.SetActive(false);

			// Pre-warm so the sky isn't empty on the first frame.
			for (int i = 0; i < 26; i++)
				EmitTwinkle();
			for (int i = 0; i < 3; i++)
				EmitFog(Random.Range(-0.5f, 0.3f));
		}

		/// <summary>Dresses the sky for a collection. Missing art falls back to the app's own night.</summary>
		public void ApplyTheme(PackTheme theme)
		{
			_theme = theme;
			var palette = _ctx.Palette;
			_sky.sprite = theme.Background != null ? theme.Background : _defaultSky;
			_sky.color = _sky.sprite != null ? Color.white : palette.Night;

			_hills.sprite = theme.Scenery != null ? theme.Scenery : _defaultHills;
			_hills.gameObject.SetActive(_hills.sprite != null);

			_moon.gameObject.SetActive(theme.Moon);
			_moonGlow.gameObject.SetActive(theme.Moon);
			_confettiColors = new[] { theme.Accent, theme.Accent2, palette.Candy, palette.Gold, palette.PurpleLight };
			_nextEmber = 0f;

			// Falling styles start from the top; pre-warm so the sky isn't empty for the first seconds.
			var area = _particles.rectTransform.rect;
			for (int i = 0; i < 18 && (theme.Ambient == AmbientStyle.Snow || theme.Ambient == AmbientStyle.Confetti); i++)
			{
				var position = new Vector2(Random.Range(-0.55f, 0.55f) * area.width, Random.Range(-0.3f, 0.5f) * area.height);
				_particles.Emit(position, theme.Ambient == AmbientStyle.Snow ? ParticlePresets.Snowfall(Color.white) : ParticlePresets.DriftingConfetti(_confettiColors), 1);
			}
		}

		private void Update()
		{
			float time = Time.unscaledTime;
			bool reduced = _ctx.ReducedMotion;
			_particles.CountScale = reduced ? 0.4f : 1f;

			_moonGlow.color = _ctx.Palette.Gold.WithAlpha(0.28f + 0.08f * Mathf.Sin(time * 1.1f));
			if (!reduced)
				_moon.anchoredPosition = new Vector2(-170f, -330f + Mathf.Sin(time * 0.5f) * 10f);

			if (time >= _nextTwinkle)
			{
				_nextTwinkle = time + Random.Range(0.08f, 0.2f);
				EmitTwinkle();
			}

			if (time >= _nextFog)
			{
				_nextFog = time + Random.Range(2.5f, 4.5f);
				EmitFog(-0.62f);
			}

			if (time >= _nextEmber)
				EmitAmbient(time);

			if (time >= _nextBat && !reduced && _theme is { Bats: true })
			{
				_nextBat = time + Random.Range(5f, 11f);
				bool leftToRight = Random.value < 0.5f;
				var area = _particles.rectTransform.rect;
				var start = new Vector2((leftToRight ? -0.6f : 0.6f) * area.width, Random.Range(0.05f, 0.35f) * area.height);
				_particles.Emit(start, ParticlePresets.FlyingBat(_ctx.Palette.Ink.Lighten(0.08f), leftToRight), Random.Range(1, 4));
			}
		}

		private void EmitAmbient(float time)
		{
			var area = _particles.rectTransform.rect;
			var style = _theme?.Ambient ?? AmbientStyle.Sparkles;
			var accent = _theme?.Accent ?? _ctx.Palette.Pumpkin;
			var accent2 = _theme?.Accent2 ?? _ctx.Palette.Slime;
			var bottom = new Vector2(Random.Range(-0.5f, 0.5f) * area.width, -area.height * 0.5f + Random.Range(0f, 220f));
			var top = new Vector2(Random.Range(-0.55f, 0.55f) * area.width, area.height * 0.5f + 20f);

			switch (style)
			{
				case AmbientStyle.Embers:
					_nextEmber = time + Random.Range(0.25f, 0.6f);
					_particles.Emit(bottom, ParticlePresets.Embers(Random.value < 0.6f ? accent : accent2), 1);
					break;
				case AmbientStyle.Snow:
					_nextEmber = time + Random.Range(0.12f, 0.3f);
					_particles.Emit(top, ParticlePresets.Snowfall(Color.white), 1);
					break;
				case AmbientStyle.Confetti:
					_nextEmber = time + Random.Range(0.3f, 0.7f);
					_particles.Emit(top, ParticlePresets.DriftingConfetti(_confettiColors), 1);
					break;
				case AmbientStyle.Hearts:
					_nextEmber = time + Random.Range(0.5f, 1f);
					_particles.Emit(bottom, ParticlePresets.RisingHeart(Random.value < 0.6f ? _ctx.Palette.Candy : accent), 1);
					break;
				case AmbientStyle.Sparkles:
					_nextEmber = time + Random.Range(0.6f, 1.2f);
					_particles.Emit(new Vector2(Random.Range(-0.5f, 0.5f) * area.width, Random.Range(-0.3f, 0.45f) * area.height), ParticlePresets.Glints(accent2.Lighten(0.3f)), 1);
					break;
				default:
					_nextEmber = time + 1f;
					break;
			}
		}

		private void EmitTwinkle()
		{
			var area = _particles.rectTransform.rect;
			var position = new Vector2(Random.Range(-0.5f, 0.5f) * area.width, Random.Range(-0.05f, 0.5f) * area.height);
			_particles.Emit(position, ParticlePresets.Twinkle(), 1);
		}

		private void EmitFog(float xNormalized)
		{
			var area = _particles.rectTransform.rect;
			var position = new Vector2(xNormalized * area.width, -area.height * 0.5f + Random.Range(80f, 420f));
			var fog = ParticlePresets.Fog(_ctx.Palette.PurpleLight.WithAlpha(0.16f));
			_particles.Emit(position, fog, 1);
		}
	}
}
