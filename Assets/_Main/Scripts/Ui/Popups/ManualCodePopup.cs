using System;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.Ui.Popups
{
	/// <summary>
	/// Editor/debug-build fallback for testing without a camera: type a payload, or fire one of the real codes /
	/// a fake one with a single tap. Never shown in release builds.
	/// </summary>
	public static class ManualCodePopup
	{
		public static Popup Show(UiContext ctx, Action<string> submit)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(960f, 1640f), palette.PurpleLight);
			popup.CloseOnBackdrop = true;
			popup.AddTitle("Debug: type a code", 0f, 64f);

			var input = UiInputField.Create(ctx, popup.Content, $"{ctx.Pack.Id}.id.SECRET");
			((RectTransform)input.transform).StretchX(1f, 130f, -150f, 10f, 1f);

			var send = ChunkyButton.Create(popup.Content, ctx, "Send", new Vector2(420f, 130f), palette.Slime.Darken(0.3f), "Scan it", null, 54f);
			send.Root().Place(new Vector2(0.5f, 1f), new Vector2(420f, 130f), new Vector2(0f, -380f));
			send.Clicked = () =>
			{
				var text = input.text;
				popup.Close();
				submit(text);
			};

			var hint = UiBuilder.Text(popup.Content, "Hint", ctx, "…or simulate a scan:", 42f, TextStyle.Body, palette.Muted);
			hint.rectTransform.StretchX(1f, 60f, -470f, 0f, 1f);

			var characters = ctx.Pack.Characters;
			int columns = 2;
			float cellW = 410f;
			float cellH = 104f;
			for (int i = 0; i <= characters.Count; i++)
			{
				bool isFake = i == characters.Count;
				var character = isFake ? null : characters[i];
				var color = isFake ? palette.Danger : ctx.Tier(character!).PrimaryColor.Darken(0.2f);
				var button = ChunkyButton.Create(popup.Content, ctx, $"Quick{i}", new Vector2(cellW, cellH), color,
					isFake ? "Fake code" : character!.DisplayName, null, 38f);
				int column = i % columns;
				int row = i / columns;
				button.Root().Place(new Vector2(0.5f, 1f), new Vector2(cellW, cellH),
					new Vector2((column - 0.5f) * (cellW + 24f), -580f - row * (cellH + 16f)));

				string payload = isFake ? "https://example.com/not-ours" : DevCodeBook.TryGetPayload(ctx.Pack.Id, character!.Id, out var real) ? real : $"{ctx.Pack.Id}.{character.Id}.NOCODE";
				button.Clicked = () =>
				{
					popup.Close();
					submit(payload);
				};
			}

			return popup;
		}
	}
}
