using _Main.Scripts.Ui.Core;
using UnityEngine;

namespace _Main.Scripts.Ui.Screens
{
	public abstract class ScreenBase : MonoBehaviour
	{
		protected UiContext Ctx { get; private set; } = null!;
		public RectTransform Rect => (RectTransform)transform;
		public CanvasGroup Group { get; private set; } = null!;
		public bool IsShown { get; private set; }

		public void Initialize(UiContext ctx)
		{
			Ctx = ctx;
			Group = this.Group();
			Build();
		}

		protected abstract void Build();

		public void SetShown(bool shown)
		{
			if (IsShown == shown)
				return;

			IsShown = shown;
			if (shown)
				OnShow();
			else
				OnHide();
		}

		protected virtual void OnShow() { }
		protected virtual void OnHide() { }
	}
}
