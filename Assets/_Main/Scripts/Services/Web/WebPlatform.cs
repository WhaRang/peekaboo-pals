using System.Runtime.InteropServices;

namespace _Main.Scripts.Services.Web
{
	/// <summary>Browser features used by the WebGL build (Assets/Plugins/WebGL/SpookyWeb.jslib). No-ops elsewhere.</summary>
	public static class WebPlatform
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")] private static extern int SpookyWeb_PrefersReducedMotion();
		[DllImport("__Internal")] private static extern void SpookyWeb_KeepAwake();
		[DllImport("__Internal")] private static extern void SpookyWeb_Vibrate(int milliseconds);
		[DllImport("__Internal")] private static extern void SpookyWeb_ClearLaunchCode();

		public static bool PrefersReducedMotion => SpookyWeb_PrefersReducedMotion() != 0;

		/// <summary>Screen Wake Lock, so the phone doesn't dim while the player walks around hunting.</summary>
		public static void KeepAwake() => SpookyWeb_KeepAwake();

		/// <summary>navigator.vibrate (Android browsers; iOS Safari ignores it).</summary>
		public static void Vibrate(int milliseconds) => SpookyWeb_Vibrate(milliseconds);

		/// <summary>The page address, which carries a tag's code when the page was opened by scanning it with the phone's camera.</summary>
		public static string LaunchUrl => UnityEngine.Application.absoluteURL;

		/// <summary>Removes the scanned code from the address bar once it was caught.</summary>
		public static void ClearLaunchCode() => SpookyWeb_ClearLaunchCode();
#else
		public static bool PrefersReducedMotion => false;
		public static void KeepAwake() { }
		public static void Vibrate(int milliseconds) { }
		public static string LaunchUrl => string.Empty;
		public static void ClearLaunchCode() { }
#endif
	}
}
