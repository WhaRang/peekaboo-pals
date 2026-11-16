using System;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Settings;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Widgets;
using UnityEngine;

namespace _Main.Scripts.Ui.Popups
{
	/// <summary>Edit the player's nickname, or roll a random spooky one.</summary>
	public static class NicknamePopup
	{
		public static Popup Show(UiContext ctx, Action onSaved)
		{
			var palette = ctx.Palette;
			var popup = Popup.Create(ctx, new Vector2(920f, 860f), palette.Pumpkin);
			popup.CloseOnBackdrop = true;
			popup.AddTitle("Your hunter name", 20f, 76f);
			popup.AddBody("This is how your friends will know you on the Results screen.", 170f, 110f, 40f);

			var input = UiInputField.Create(ctx, popup.Content, "Type a nickname");
			((RectTransform)input.transform).Place(new Vector2(0.5f, 1f), new Vector2(620f, 130f), new Vector2(-80f, -380f));
			input.characterLimit = NicknameGenerator.MaxLength;
			input.text = ctx.Settings.Nickname;
			input.pointSize = 54f;

			var dice = ChunkyButton.Create(popup.Content, ctx, "Dice", new Vector2(140f, 140f), palette.Purple, null, UiSprite.IconDice);
			dice.Root().Place(new Vector2(0.5f, 1f), new Vector2(140f, 140f), new Vector2(330f, -376f));
			dice.Clicked = () =>
			{
				input.text = NicknameGenerator.Generate();
				dice.Face.Wobble(18f, 0.45f, 3);
			};

			popup.AddButton("Cancel", palette.Purple, () => { });
			popup.AddButton("Save", palette.Slime.Darken(0.25f), () =>
			{
				ctx.Settings.Nickname = input.text;
				onSaved();
			});
			return popup;
		}
	}
}
