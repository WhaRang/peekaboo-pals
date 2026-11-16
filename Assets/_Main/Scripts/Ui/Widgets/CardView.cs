using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Ui.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Widgets
{
	/// <summary>
	/// A collectible card. Collected: full art, rarity frame and glow, name, power and stars, idle bob and shine.
	/// Locked: dark silhouette with "?" and only the frame color hinting at the rarity. Everything comes from the
	/// character + tier definitions; nothing is character-specific here.
	/// </summary>
	public sealed class CardView : MonoBehaviour
	{
		private UiContext _ctx = null!;
		private RarityTierDefinition _tier = null!;
		private Vector2 _size;

		private Image _glow = null!;
		private Image _frame = null!;
		private Image _inner = null!;
		private Image _art = null!;
		private Image? _rays;
		private Image _shine = null!;
		private Image _back = null!;
		private TextMeshProUGUI _question = null!;
		private TextMeshProUGUI _name = null!;
		private Image _namePlate = null!;
		private RectTransform _stars = null!;
		private RectTransform _powerBadge = null!;
		private RectTransform _newBadge = null!;
		private TextMeshProUGUI _newBadgeText = null!;
		private Image _metal = null!;
		private Image _scratches = null!;
		private Image _chip = null!;
		private TextMeshProUGUI _chipText = null!;
		private Material? _artMaterial;
		private FloatGradeDefinition? _grade;

		private bool _idle;
		private float _phase;
		private float _nextShineTime;
		private bool _shining;

		public CharacterDefinition Character { get; private set; } = null!;
		public RectTransform Body { get; private set; } = null!;
		public RectTransform Root => (RectTransform)transform;
		public bool IsCollected { get; private set; }
		public Image ArtImage => _art;
		public Image Glow => _glow;
		public RectTransform ArtRect => _art.rectTransform;
		public RectTransform Chip => _chip.rectTransform;
		public float? Float { get; private set; }

		public static CardView Create(Transform parent, UiContext ctx, CharacterDefinition character, Vector2 size)
		{
			var root = UiBuilder.Rect($"Card {character.Id}", parent);
			root.sizeDelta = size;
			var view = root.gameObject.AddComponent<CardView>();
			view.Build(ctx, character, size);
			return view;
		}

		private void Build(UiContext ctx, CharacterDefinition character, Vector2 size)
		{
			_ctx = ctx;
			_size = size;
			Character = character;
			_tier = ctx.Tier(character);
			float w = size.x;
			var palette = ctx.Palette;

			_glow = UiBuilder.Image(transform, "Glow", ctx, UiSprite.SoftGlow, _tier.GlowColor.WithAlpha(0.7f));
			_glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(w * 1.9f, size.y * 1.6f));

			var shadow = UiBuilder.Image(transform, "Shadow", ctx, UiSprite.CardFrame, palette.Ink.WithAlpha(0.55f));
			shadow.rectTransform.Stretch(0f, 0f, w * 0.05f, -w * 0.05f);

			Body = UiBuilder.Rect("Body", transform);
			Body.Stretch();

			_frame = UiBuilder.Image(Body, "Frame", ctx, UiSprite.CardFrame, _tier.PrimaryColor);
			_frame.rectTransform.Stretch();
			_metal = UiBuilder.Image(Body, "Metal", ctx, UiSprite.CardFrameMetal, Color.clear);
			_metal.rectTransform.Stretch();

			var innerMask = UiBuilder.Rect("InnerMask", Body);
			float inset = w * 0.065f;
			innerMask.Stretch(inset, inset, inset, inset);
			innerMask.gameObject.AddComponent<RectMask2D>();

			_inner = UiBuilder.Image(innerMask, "Inner", ctx, UiSprite.CardInner, Color.white);
			_inner.rectTransform.Stretch();

			if (_tier.RainbowShimmer)
			{
				// Kept faint so a pale character (the white robot) still stands out against it.
				_rays = UiBuilder.Image(innerMask, "Rays", ctx, UiSprite.Rays, _tier.GlowColor.WithAlpha(0.2f));
				_rays.rectTransform.Place(new Vector2(0.5f, 0.58f), new Vector2(w * 1.8f, w * 1.8f));
			}

			_art = UiBuilder.Image(innerMask, "Art", character.Art, Color.white);
			_art.preserveAspect = true;
			if (ctx.Theme.WearShader != null)
			{
				_artMaterial = new Material(ctx.Theme.WearShader) { name = $"Wear {character.Id}" };
				_art.material = _artMaterial;
			}
			_art.rectTransform.anchorMin = new Vector2(0.02f, 0.2f);
			_art.rectTransform.anchorMax = new Vector2(0.98f, 0.98f);
			_art.rectTransform.offsetMin = Vector2.zero;
			_art.rectTransform.offsetMax = Vector2.zero;

			_question = UiBuilder.Text(innerMask, "Question", ctx, "?", w * 0.42f, TextStyle.Title, _tier.PrimaryColor.Lighten(0.2f));
			_question.rectTransform.anchorMin = new Vector2(0f, 0.2f);
			_question.rectTransform.anchorMax = new Vector2(1f, 0.98f);
			_question.rectTransform.offsetMin = Vector2.zero;
			_question.rectTransform.offsetMax = Vector2.zero;

			_scratches = UiBuilder.Image(innerMask, "Scratches", ctx, UiSprite.Scratches, Color.clear);
			_scratches.rectTransform.Stretch();

			_shine = UiBuilder.Image(innerMask, "Shine", ctx, UiSprite.Shine, new Color(1f, 1f, 1f, 0.55f));
			_shine.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(w * 0.45f, size.y * 2f), new Vector2(-w * 1.5f, 0f));
			_shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -22f);

			_namePlate = UiBuilder.Image(Body, "NamePlate", ctx, UiSprite.Pill, _tier.PrimaryColor.Darken(0.35f));
			_namePlate.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(w * 0.94f, w * 0.22f), new Vector2(0f, w * 0.12f), new Vector2(0.5f, 0f));
			_name = UiBuilder.Text(_namePlate.rectTransform, "Name", ctx, character.DisplayName, w * 0.1f, TextStyle.TitleThin, palette.Cream);
			_name.rectTransform.Stretch(w * 0.05f, w * 0.05f, 0f, 0f);
			_name.textWrappingMode = TextWrappingModes.NoWrap;
			_name.AutoSize(w * 0.055f);

			_stars = UiBuilder.Rect("Stars", Body);
			_stars.Place(new Vector2(0.5f, 0f), new Vector2(w, w * 0.12f), new Vector2(0f, w * 0.36f), new Vector2(0.5f, 0f));
			float starSize = w * 0.11f;
			for (int i = 0; i < _tier.Stars; i++)
			{
				var star = UiBuilder.Image(_stars, $"Star{i}", ctx, UiSprite.IconStar, palette.Gold);
				star.preserveAspect = true;
				float x = (i - (_tier.Stars - 1) * 0.5f) * starSize * 0.95f;
				star.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(starSize, starSize), new Vector2(x, 0f));
			}

			var badge = UiBuilder.Image(Body, "Power", ctx, UiSprite.Circle, palette.Ink);
			_powerBadge = badge.rectTransform;
			_powerBadge.Place(new Vector2(1f, 1f), new Vector2(w * 0.3f, w * 0.3f), new Vector2(-w * 0.04f, -w * 0.04f), new Vector2(0.78f, 0.78f));
			var ring = UiBuilder.Image(_powerBadge, "Ring", ctx, UiSprite.Circle, _tier.PrimaryColor);
			ring.rectTransform.Stretch(w * 0.018f, w * 0.018f, w * 0.018f, w * 0.018f);
			var core = UiBuilder.Image(_powerBadge, "Core", ctx, UiSprite.Circle, palette.Ink);
			core.rectTransform.Stretch(w * 0.035f, w * 0.035f, w * 0.035f, w * 0.035f);
			var power = UiBuilder.Text(_powerBadge, "Value", ctx, character.Power.ToString(), w * 0.12f, TextStyle.TitleThin, palette.Gold);
			power.rectTransform.Stretch(0f, 0f, 0f, w * 0.01f);

			_chip = UiBuilder.Image(Body, "FloatChip", ctx, UiSprite.Pill, palette.Muted);
			_chip.rectTransform.Place(new Vector2(0f, 1f), new Vector2(w * 0.5f, w * 0.17f), new Vector2(w * 0.035f, -w * 0.035f), new Vector2(0f, 1f));
			_chipText = UiBuilder.Text(_chip.rectTransform, "Value", ctx, "", w * 0.085f, TextStyle.Body, palette.Ink);
			_chipText.rectTransform.Stretch(w * 0.03f, w * 0.03f, 0f, w * 0.005f);
			_chipText.textWrappingMode = TextWrappingModes.NoWrap;
			_chipText.AutoSize(w * 0.05f);
			_chip.gameObject.SetActive(false);

			var newBadge = UiBuilder.Image(Body, "New", ctx, UiSprite.Badge, palette.Candy);
			_newBadge = newBadge.rectTransform;
			_newBadge.Place(new Vector2(0.5f, 1f), new Vector2(w * 0.56f, w * 0.22f), new Vector2(0f, w * 0.04f), new Vector2(0.5f, 0.5f));
			_newBadge.localRotation = Quaternion.Euler(0f, 0f, -6f);
			_newBadgeText = UiBuilder.Text(_newBadge, "Text", ctx, "NEW!", w * 0.12f, TextStyle.Title, palette.Cream);
			_newBadgeText.rectTransform.Stretch(0f, 0f, 0f, 0f);
			_newBadgeText.textWrappingMode = TextWrappingModes.NoWrap;
			_newBadge.gameObject.SetActive(false);

			_back = UiBuilder.Image(Body, "Back", ctx, UiSprite.CardBack, _tier.PrimaryColor);
			_back.rectTransform.Stretch();
			_back.gameObject.SetActive(false);

			_phase = Random.value * 10f;
			SetCollected(false);
		}

		public void SetCollected(bool collected)
		{
			IsCollected = collected;
			var palette = _ctx.Palette;

			_art.sprite = collected ? Character.Art : Character.Silhouette != null ? Character.Silhouette : Character.Art;
			_art.color = collected ? Color.white : Color.Lerp(palette.NightDeep, _tier.PrimaryColor, 0.18f).WithAlpha(0.95f);
			_question.gameObject.SetActive(!collected);
			_inner.color = collected
				? Color.Lerp(_tier.SecondaryColor, Character.ThemeColor, 0.35f).Darken(0.35f)
				: palette.NightDeep.Lighten(0.04f);
			_frame.color = collected ? _tier.PrimaryColor : _tier.PrimaryColor.Darken(0.45f);
			_glow.gameObject.SetActive(collected);
			_stars.gameObject.SetActive(collected);
			_powerBadge.gameObject.SetActive(collected);
			_namePlate.color = collected ? _tier.PrimaryColor.Darken(0.35f) : palette.NightDeep.Lighten(0.08f);
			_name.text = collected ? Character.DisplayName : "???";
			_name.color = collected ? palette.Cream : _tier.PrimaryColor.Darken(0.1f);
			if (_rays != null)
				_rays.gameObject.SetActive(collected);

			ApplyFloat(collected ? Float : null);
		}

		/// <summary>The catch quality: rare floats look metallic and contrasty, common ones pale and scratched.</summary>
		public void SetFloat(float? value)
		{
			Float = value;
			ApplyFloat(IsCollected ? value : null);
		}

		private void ApplyFloat(float? value)
		{
			var palette = _ctx.Palette;
			_grade = value.HasValue && _ctx.Pack.FloatGrades.Count > 0 ? _ctx.Pack.GetFloatGrade(value.Value) : null;

			if (_grade == null)
			{
				SetArtWear(1f, 1f, 0f);
				_metal.color = Color.clear;
				_scratches.color = Color.clear;
				_chip.gameObject.SetActive(false);
				return;
			}

			var grade = _grade;
			SetArtWear(grade.Saturation, grade.Contrast, grade.Wash);
			_metal.color = Color.Lerp(Color.white, grade.Color, 0.35f).WithAlpha(grade.Metallic * 0.95f);
			_scratches.color = palette.Cream.WithAlpha(grade.Scratches * 0.75f);
			_frame.color = Color.Lerp(_tier.PrimaryColor, Color.Lerp(_tier.PrimaryColor, new Color(0.86f, 0.84f, 0.8f), 0.7f), grade.Wash * 2f);
			var baseInner = Color.Lerp(_tier.SecondaryColor, Character.ThemeColor, 0.35f).Darken(0.35f);
			_inner.color = Color.Lerp(baseInner, new Color(0.55f, 0.52f, 0.5f), grade.Wash);

			_chip.gameObject.SetActive(true);
			_chip.sprite = _ctx.Sprite(grade.Metallic >= 0.5f ? UiSprite.ChipMetal : UiSprite.Pill);
			_chip.color = grade.Metallic >= 0.5f ? Color.Lerp(Color.white, grade.Color, 0.55f) : Color.Lerp(grade.Color, Color.white, 0.25f);
			_chipText.text = FloatStyle.Short(value!.Value);
			_chipText.color = palette.Ink;
		}

		private void SetArtWear(float saturation, float contrast, float wash)
		{
			if (_artMaterial == null)
				return;
			_artMaterial.SetFloat(SaturationId, saturation);
			_artMaterial.SetFloat(ContrastId, contrast);
			_artMaterial.SetFloat(WashId, wash);
		}

		private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
		private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
		private static readonly int WashId = Shader.PropertyToID("_Wash");

		private void OnDestroy()
		{
			if (_artMaterial != null)
				Destroy(_artMaterial);
		}

		public void ShowBack(bool showBack) => _back.gameObject.SetActive(showBack);

		public void SetIdle(bool idle)
		{
			_idle = idle && !_ctx.ReducedMotion;
			_nextShineTime = Time.unscaledTime + Random.Range(1.5f, 6f);
			if (!_idle)
			{
				Body.anchoredPosition = Vector2.zero;
				Body.localRotation = Quaternion.identity;
			}
		}

		public void ShowNewBadge(bool show, bool animate = true, string text = "NEW!")
		{
			_newBadgeText.text = text;
			_newBadge.gameObject.SetActive(show);
			if (!show || !animate)
				return;

			_newBadge.localScale = Vector3.one * 2.4f;
			_newBadge.ScaleTo(1f, 0.35f, Ease.OutBackStrong);
		}

		/// <summary>A diagonal glint sweeping across the card.</summary>
		public void PlayShine(float duration = 0.6f)
		{
			if (_shining)
				return;

			_shining = true;
			var rect = _shine.rectTransform;
			float from = -_size.x * 1.1f;
			float to = _size.x * 1.1f;
			Tweens.Run(rect, duration, k => rect.anchoredPosition = new Vector2(Mathf.Lerp(from, to, k), 0f), Ease.InOutSine, TweenChannel.Position)
				.OnComplete(() =>
				{
					rect.anchoredPosition = new Vector2(-_size.x * 1.5f, 0f);
					_shining = false;
				});
		}

		/// <summary>The slot "fills" when a revealed card lands in it.</summary>
		public void PlayFill()
		{
			Body.localScale = new Vector3(1.25f, 0.8f, 1f);
			Body.ScaleTo(Vector3.one, 0.6f, Ease.OutElastic);
			_glow.color = _tier.GlowColor.WithAlpha(1f);
			_glow.rectTransform.localScale = Vector3.one * 1.4f;
			_glow.rectTransform.ScaleTo(1f, 0.6f, Ease.OutCubic);
			PlayShine(0.5f);
		}

		private void Update()
		{
			if (!IsCollected)
				return;

			float time = Time.unscaledTime + _phase;

			if (_tier.RainbowShimmer)
			{
				// Gold first, with a rainbow shimmer rolling through it.
				var rainbow = Color.HSVToRGB(Mathf.Repeat(time * 0.18f, 1f), 0.5f, 1f);
				_frame.color = Color.Lerp(_tier.PrimaryColor, rainbow, 0.16f + 0.1f * Mathf.Sin(time * 2.4f));
				_glow.color = Color.Lerp(_tier.GlowColor, rainbow, 0.35f).WithAlpha(0.75f);
				if (_rays != null && !_ctx.ReducedMotion)
					_rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, time * 12f);
			}
			else if (!_ctx.ReducedMotion)
			{
				float pulse = 0.55f + 0.2f * Mathf.Sin(time * 2.2f);
				_glow.color = _tier.GlowColor.WithAlpha(pulse);
			}

			if (_grade != null && _grade.FlameIntensity >= 0.95f)
				_chip.color = FloatStyle.AnimatedColor(_grade, time);

			if (!_idle)
				return;

			Body.anchoredPosition = new Vector2(0f, Mathf.Sin(time * 1.9f) * _size.x * 0.025f);
			Body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.3f) * 1.6f);

			if (Time.unscaledTime >= _nextShineTime)
			{
				float metallic = _grade?.Metallic ?? 0f;
				_nextShineTime = Time.unscaledTime + Random.Range(4f, 9f) * (1f - 0.7f * metallic);
				PlayShine();
			}
		}
	}
}
