using System;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;

namespace _Main.Scripts.Ui.Popups
{
	/// <summary>Popups about collections arriving from the website and app updates.</summary>
	public static class PackPopups
	{
		/// <summary>A collection was downloaded while the app is open.</summary>
		public static Popup NewCollection(UiContext ctx, CollectionPack pack, Action onOpen, Action onClosed)
		{
			var palette = ctx.Palette;
			var accent = pack.Theme.Accent;
			var popup = Popup.Create(ctx, new Vector2(880f, 1100f), accent);

			var coverBack = UiBuilder.Image(popup.Content, "CoverBack", ctx, UiSprite.Circle, accent.Darken(0.5f));
			coverBack.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(330f, 330f), new Vector2(0f, -220f));
			var coverSprite = pack.Theme.Cover != null ? pack.Theme.Cover : pack.Characters.Count > 0 ? pack.Characters[0].Art : null;
			var cover = UiBuilder.Image(coverBack.rectTransform, "Cover", coverSprite, Color.white);
			cover.preserveAspect = true;
			cover.rectTransform.Stretch(18f, 18f, 18f, 18f);
			if (!ctx.ReducedMotion)
			{
				coverBack.rectTransform.localScale = Vector3.one * 0.4f;
				coverBack.rectTransform.ScaleTo(1f, 0.6f, Ease.OutBackStrong).SetDelay(0.15f);
				Tweens.Delay(0.8f, () =>
				{
					if (coverBack != null)
						coverBack.rectTransform.Wobble(8f, 0.8f, 3);
				});
			}

			popup.AddTitle("A new collection!", 420f, 74f, accent.Lighten(0.2f));
			popup.AddBody($"{pack.Title} just arrived on your phone.\n{pack.Characters.Count} new {pack.Texts.Friends} to find!", 540f, 210f);
			popup.AddButton("Later", palette.Purple, () => { });
			popup.AddButton("Open it", accent.Darken(0.2f), onOpen);
			popup.Closed += onClosed;

			ctx.Audio.Play(GameSound.NewBadge);
			ctx.Haptics.Play(HapticStrength.Medium);
			return popup;
		}

		/// <summary>The website announces a newer app (Android): once per version.</summary>
		public static Popup AppUpdate(UiContext ctx, AppRelease release, Action onClosed)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(880f, 960f), palette.Slime.Darken(0.15f));
			AddFloatingPal(ctx, popup);

			popup.AddTitle("A new version is out!", 300f, 70f);
			popup.AddBody($"{Application.productName} {release.Version} is ready. Get it so the next collections work on your phone. Your pals stay safe.", 450f, 240f, 42f);
			popup.AddButton("Later", palette.Purple, () => { });
			if (!string.IsNullOrEmpty(release.DownloadUrl))
				popup.AddButton("Get it", palette.Slime.Darken(0.3f), () => Application.OpenURL(release.DownloadUrl));
			popup.Closed += onClosed;

			ctx.Audio.Play(GameSound.Sparkle);
			return popup;
		}

		/// <summary>A code (or a book on the shelf) from a collection that needs a newer app.</summary>
		public static Popup NeedsAppUpdate(UiContext ctx, LockedPack locked, AppRelease? release, Action onClosed)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(880f, 960f), palette.Gold.Darken(0.1f));
			AddFloatingPal(ctx, popup);

			popup.AddTitle("Update the app!", 300f, 74f);
#if UNITY_WEBGL && !UNITY_EDITOR
			string how = "Reload this page, then scan the code again.";
#else
			string how = "Get the update, then scan the code again. Your pals stay safe.";
#endif
			popup.AddBody($"{locked.Title} needs {Application.productName} {locked.MinAppVersion} or newer. {how}", 450f, 240f, 42f);
			popup.AddButton("OK", palette.Purple, () => { });
			if (!string.IsNullOrEmpty(release?.DownloadUrl))
				popup.AddButton("Get it", palette.Slime.Darken(0.3f), () => Application.OpenURL(release!.DownloadUrl));
			popup.Closed += onClosed;

			ctx.Audio.Play(GameSound.Error);
			ctx.Haptics.Play(HapticStrength.Light);
			return popup;
		}

		/// <summary>A code from a collection this phone doesn't have: shown while the website is asked for it.</summary>
		public static Popup Fetching(UiContext ctx, Action onCancel)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(880f, 900f), palette.PurpleLight);
			AddFloatingPal(ctx, popup);

			popup.AddTitle("A pal from a new collection!", 300f, 66f);
			var body = popup.AddBody("Downloading it", 450f, 150f);
			popup.gameObject.AddComponent<DotsText>().Setup(body, "Downloading it");
			popup.AddButton("Cancel", palette.Purple, onCancel);

			ctx.Audio.Play(GameSound.Sparkle);
			ctx.Haptics.Play(HapticStrength.Light);
			return popup;
		}

		private static void AddFloatingPal(UiContext ctx, Popup popup)
		{
			var pal = UiBuilder.Image(popup.Content, "Pal", ctx, UiSprite.IconPal, Color.white);
			pal.preserveAspect = true;
			pal.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(240f, 240f), new Vector2(0f, -140f));
			if (ctx.ReducedMotion)
				return;

			var rect = pal.rectTransform;
			Tweens.Run(rect, 1.6f, k => rect.anchoredPosition = new Vector2(0f, -140f + Mathf.Sin(k * Mathf.PI * 2f) * 14f), Ease.Linear, TweenChannel.Position)
				.SetLoops(-1);
		}

		/// <summary>"Downloading it." → ".." → "..." while the popup is open.</summary>
		private sealed class DotsText : MonoBehaviour
		{
			private TextMeshProUGUI _label = null!;
			private string _text = string.Empty;

			public void Setup(TextMeshProUGUI label, string text)
			{
				_label = label;
				_text = text;
			}

			private void Update()
			{
				if (_label != null)
					_label.text = _text + new string('.', 1 + (int)(Time.unscaledTime * 2.5f) % 3);
			}
		}
	}
}
