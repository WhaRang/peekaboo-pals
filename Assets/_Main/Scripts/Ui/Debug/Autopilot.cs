#if SPOOKY_AUTOPILOT
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Services.Qr;
using _Main.Scripts.Ui.Flow;
using _Main.Scripts.Ui.Navigation;
using _Main.Scripts.Ui.Popups;
using _Main.Scripts.Ui.Screens;
using _Main.Scripts.Ui.Widgets;
using UnityEngine;

namespace _Main.Scripts.Ui.DebugTools
{
	/// <summary>
	/// Dev-only (SPOOKY_AUTOPILOT define): plays the whole game flow on its own and saves screenshots, so the UI and
	/// the reveal choreography can be checked without a device. Started with "-autopilot &lt;outputDir&gt;".
	/// </summary>
	public sealed class Autopilot : MonoBehaviour
	{
		private string _outDir = "";
		private string _phase = "full";
		private int _shot;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Boot()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			if (Application.absoluteURL.Contains("scantest"))
			{
				PlayerPrefs.DeleteAll();
				var web = new GameObject("[ScanTest]");
				DontDestroyOnLoad(web);
				web.AddComponent<Autopilot>()._phase = "scantest";
			}
			return;
#endif
			var args = Environment.GetCommandLineArgs();
			int index = Array.IndexOf(args, "-autopilot");
			if (index < 0 || index + 1 >= args.Length)
				return;

			int phaseIndex = Array.IndexOf(args, "-autopilotPhase");
			string phase = phaseIndex >= 0 && phaseIndex + 1 < args.Length ? args[phaseIndex + 1] : "full";

			// "persist2" relaunches on top of the save written by "persist1" to prove progress survives restarts.
			if (phase != "persist2" && phase != "online2")
				PlayerPrefs.DeleteAll();
			Application.runInBackground = true;
			var go = new GameObject("[Autopilot]");
			DontDestroyOnLoad(go);
			var autopilot = go.AddComponent<Autopilot>();
			autopilot._outDir = args[index + 1];
			autopilot._phase = phase;
		}

		/// <summary>WebGL: tap through to the scanner, allow the camera and report what the browser decoder sees.</summary>
		private IEnumerator ScanTest()
		{
			yield return new WaitForSecondsRealtime(2.5f);
			FindAnyObjectByType<SplashOverlay>()?.OnPointerClick(null!);
			yield return new WaitForSecondsRealtime(1.2f);
			Bar().TabSelected?.Invoke(MainTab.Scanner);
			yield return new WaitForSecondsRealtime(1.0f);
			FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Allow")?.Clicked?.Invoke();
			Debug.Log("[ScanTest] scanner open, waiting for a code");

			float t = 0f;
			while (t < 40f)
			{
				t += Time.unscaledDeltaTime;
				var reveal = FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(r => r.name.StartsWith("Reveal "));
				if (reveal != null)
				{
					Debug.Log($"[ScanTest] DECODED -> {reveal.name} after {t:0.0}s");
					yield break;
				}

				yield return null;
			}

			var status = FindAnyObjectByType<ScannerScreen>()?.GetComponentsInChildren<TMPro.TextMeshProUGUI>().FirstOrDefault(x => x.name == "Status");
			Debug.Log($"[ScanTest] TIMEOUT, camera running={FindAnyObjectByType<ScannerScreen>() != null} status='{status?.text}'");
		}

		private IEnumerator Start()
		{
			if (_phase == "scantest")
			{
				yield return ScanTest();
				yield break;
			}

			Directory.CreateDirectory(_outDir);
			yield return new WaitForSecondsRealtime(1.6f);
			yield return Shot("splash");

			FindAnyObjectByType<SplashOverlay>()?.OnPointerClick(null!);
			yield return new WaitForSecondsRealtime(1.2f);
			yield return Shot("lobby_empty");

			var config = FindConfig();
			const string halloween = "halloween-2026";
			string Payload(string id) => DevCodeBook.TryGetPayload(halloween, id, out var p) ? p : id;
			string LegacyPayload(string id) => DevCodeBook.TryGetSecret(halloween, id, out var secret) ? QrPayloadCodec.EncodeLegacy("SPOOKY", id, secret) : id;

			if (_phase == "persist1")
			{
				foreach (var id in new[] { "pip", "wanda", "jenny" })
				{
					yield return Scan(Payload(id));
					yield return Collect(0f, null);
				}

				yield return Shot("persist1_before_quit");
				Application.Quit();
				yield break;
			}

			if (_phase.StartsWith("online"))
			{
				yield return Online();
				Application.Quit();
				yield break;
			}

			if (_phase == "persist2")
			{
				yield return Shot("persist2_lobby_after_restart");
				Bar().TabSelected?.Invoke(MainTab.Results);
				yield return new WaitForSecondsRealtime(1.8f);
				yield return Shot("persist2_results_after_restart");
				yield return Scan(Payload("pip"));
				yield return new WaitForSecondsRealtime(0.8f);
				yield return Shot("persist2_duplicate_after_restart");
				Application.Quit();
				yield break;
			}

			typeof(CollectionConfig).GetField("_recatchCooldownSeconds", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(config, 600);

			// Nickname editing
			FindObjectsByType<TapArea>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Nickname")?.Tapped?.Invoke();
			yield return new WaitForSecondsRealtime(0.8f);
			FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Dice")?.Clicked?.Invoke();
			yield return new WaitForSecondsRealtime(0.5f);
			yield return Shot("nickname_popup");
			FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Button1")?.Clicked?.Invoke();
			yield return new WaitForSecondsRealtime(0.6f);

			Bar().TabSelected?.Invoke(MainTab.Scanner);
			yield return new WaitForSecondsRealtime(0.8f);
			yield return Shot("scanner");

			// Common, plain float
			_Main.Scripts.Services.Floats.FloatRoller.ForcedNext = 0.30f;
			yield return Scan(Payload("pip"));
			yield return Capture("common", 0.9f, 2.4f, 3.9f);
			yield return Collect(0.35f, "common_fly");

			// Re-scan straight away: resting
			yield return Scan(Payload("pip"));
			yield return new WaitForSecondsRealtime(0.8f);
			yield return Shot("resting");
			ClosePopups();
			typeof(CollectionConfig).GetField("_recatchCooldownSeconds", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(config, 0);
			yield return new WaitForSecondsRealtime(0.6f);

			// After the cooldown, a worse float
			_Main.Scripts.Services.Floats.FloatRoller.ForcedNext = 0.42f;
			yield return Scan(Payload("pip"));
			yield return new WaitForSecondsRealtime(2.6f);
			yield return Shot("no_improvement");
			ClosePopups();
			yield return new WaitForSecondsRealtime(0.6f);

			// ...and a Phantom Perfect upgrade
			_Main.Scripts.Services.Floats.FloatRoller.ForcedNext = 0.0042f;
			yield return Scan(Payload("pip"));
			yield return Capture("upgrade", 1.0f, 3.2f, 4.0f, 5.6f);
			yield return Collect(0.4f, "upgrade_fly");

			yield return Scan("https://example.com/not-a-spooky-code");
			yield return new WaitForSecondsRealtime(0.8f);
			yield return Shot("invalid");
			ClosePopups();
			yield return new WaitForSecondsRealtime(0.5f);

			// Rare, epic, legendary
			yield return Scan(Payload("wanda"));
			yield return WaitForCollect(14f);
			yield return Shot("rare_info");
			yield return Collect(0.4f, null);
			_Main.Scripts.Services.Floats.FloatRoller.ForcedNext = 0.12f;
			yield return Scan(Payload("fangsworth"));
			yield return Capture("epic", 2.0f, 4.6f);
			yield return Collect(0.4f, null);
			_Main.Scripts.Services.Floats.FloatRoller.ForcedNext = 0.05f;
			yield return Scan(Payload("jenny"));
			yield return Capture("legendary", 2.4f, 4.0f, 6.6f, 7.4f);
			yield return Collect(0.4f, "legendary_fly");

			// The old Halloween tags (SPOOKY:id:SECRET) keep working next to the new links.
			foreach (var id in new[] { "skelly", "batsy", "sheety", "mumbles", "howler" })
			{
				yield return Scan(id == "howler" ? LegacyPayload(id) : Payload(id));
				yield return WaitForCollect(14f);
				yield return Collect(0f, null);
			}

			yield return new WaitForSecondsRealtime(1.2f);
			yield return Shot("lobby_almost");

			yield return Scan(Payload("gourdking"));
			yield return WaitForCollect(14f);
			yield return Collect(0f, null);
			yield return new WaitForSecondsRealtime(1.0f);
			yield return Shot("completion_start");
			yield return new WaitForSecondsRealtime(2.2f);
			yield return Shot("completion");

			var resultsButton = FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
				.FirstOrDefault(b => b.name == "Results" && b.transform.parent != null && b.transform.parent.name == "Stage");
			resultsButton?.Clicked?.Invoke();
			yield return new WaitForSecondsRealtime(2.0f);
			yield return Shot("results_top");
			var resultsScroll = FindAnyObjectByType<ResultsScreen>()?.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
			if (resultsScroll != null)
				resultsScroll.verticalNormalizedPosition = 0f;
			yield return new WaitForSecondsRealtime(0.5f);
			yield return Shot("results_bottom");

			Bar().TabSelected?.Invoke(MainTab.Collection);
			yield return new WaitForSecondsRealtime(1.2f);
			yield return Shot("lobby_full");

			var card = FindObjectsByType<CardView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(c => c.Character.Id == "pip");
			card?.GetComponent<TapArea>()?.Tapped?.Invoke();
			yield return new WaitForSecondsRealtime(1.2f);
			yield return Shot("card_detail");
			FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Info")?.Clicked?.Invoke();
			yield return new WaitForSecondsRealtime(0.8f);
			yield return Shot("card_detail_back");
			ClosePopups();
			yield return new WaitForSecondsRealtime(0.5f);

			card = FindObjectsByType<CardView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(c => c.Character.Id == "jenny");
			card?.GetComponent<TapArea>()?.Tapped?.Invoke();
			yield return new WaitForSecondsRealtime(1.4f);
			yield return Shot("card_detail_jenny");
			ClosePopups();
			yield return new WaitForSecondsRealtime(0.5f);

			FindAnyObjectByType<LobbyScreen>()?.HostToolsRequested?.Invoke();
			yield return new WaitForSecondsRealtime(1.5f);
			yield return Shot("host_top");
			var host = FindAnyObjectByType<HostScreen>();
			var hostScroll = host != null ? host.GetComponentInChildren<UnityEngine.UI.ScrollRect>() : null;
			if (hostScroll != null)
				hostScroll.verticalNormalizedPosition = 0f;
			yield return new WaitForSecondsRealtime(0.6f);
			yield return Shot("host_bottom");

			FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Reset")?.Clicked?.Invoke();
			yield return new WaitForSecondsRealtime(0.8f);
			yield return Shot("reset_confirm");
			ClosePopups();
			host?.CloseRequested?.Invoke();
			yield return new WaitForSecondsRealtime(0.6f);

			// Album shelf, then any other shipped collection (switch by tapping its book, then by scanning its code).
			FindAnyObjectByType<LobbyScreen>()?.AlbumsRequested?.Invoke();
			yield return new WaitForSecondsRealtime(1.6f);
			yield return Shot("albums");
			var other = FindAnyObjectByType<AlbumScreen>() != null ? FindOtherPack(halloween) : null;
			if (other != null)
			{
				FindAnyObjectByType<AlbumScreen>()!.PackSelected?.Invoke(other);
				yield return new WaitForSecondsRealtime(1.5f);
				yield return Shot($"lobby_{other.Id}");
				var first = other.Characters.OrderByDescending(c => c.Rarity).First();
				if (DevCodeBook.TryGetPayload(other.Id, first.Id, out var otherPayload))
				{
					FindAnyObjectByType<LobbyScreen>()?.AlbumsRequested?.Invoke();
					yield return new WaitForSecondsRealtime(0.8f);
					FindAnyObjectByType<AlbumScreen>()!.PackSelected?.Invoke(FindOtherPack(other.Id)!);
					yield return new WaitForSecondsRealtime(1.0f);
					yield return Shot("lobby_back_to_first");
					yield return Scan(otherPayload);
					yield return Capture($"{other.Id}_reveal", 2.4f, 5.0f);
					yield return Collect(0.4f, $"{other.Id}_after_scan");
				}

				Bar().TabSelected?.Invoke(MainTab.Results);
				yield return new WaitForSecondsRealtime(1.8f);
				yield return Shot($"results_{other.Id}");
				Bar().TabSelected?.Invoke(MainTab.Collection);
				yield return new WaitForSecondsRealtime(0.8f);
				FindAnyObjectByType<LobbyScreen>()?.AlbumsRequested?.Invoke();
				yield return new WaitForSecondsRealtime(1.6f);
				yield return Shot("albums_two_packs");
				FindAnyObjectByType<AlbumScreen>()?.CloseRequested?.Invoke();
				yield return new WaitForSecondsRealtime(0.6f);
			}
			else
			{
				FindAnyObjectByType<AlbumScreen>()?.CloseRequested?.Invoke();
				yield return new WaitForSecondsRealtime(0.6f);
			}

			yield return Scan("future-pack.someone.ABCDEF");
			yield return new WaitForSecondsRealtime(0.8f);
			yield return Shot("unknown_pack");
			ClosePopups();
			yield return new WaitForSecondsRealtime(0.6f);

			FindAnyObjectByType<LobbyScreen>()?.HostToolsRequested?.Invoke();
			yield return new WaitForSecondsRealtime(1.2f);
			host = FindAnyObjectByType<HostScreen>();
			host?.ResetCollectionConfirmed?.Invoke();
			ClosePopups();
			yield return new WaitForSecondsRealtime(2.0f);
			yield return Shot("after_reset");

			Debug.Log("[Autopilot] Finished.");
			Application.Quit();
		}

		/// <summary>
		/// Downloaded collections, against a local test website (PEEKABOO_REMOTE_PACKS). The site has a new "test-winter"
		/// pack, a "test-future" pack that needs app 9.9 and announces a newer app.
		/// online: first launch downloads; online2: relaunch with the site down (the download stays);
		/// online3: fresh phone, site down at launch and up ~8 s later, then a winter code is scanned.
		/// </summary>
		private IEnumerator Online()
		{
			const string winter = "test-winter";
			string WinterPayload(string id) => DevCodeBook.TryGetPayload(winter, id, out var p) ? p : $"{winter}.{id}.NOPE00";
			var library = Ctx().Library;

			if (_phase == "online3")
			{
				yield return WaitForCheck(library);
				Debug.Log($"[Autopilot] online3 boot check: {library.CheckStatus}, packs {library.Packs.Count}");
				yield return Shot("online3_lobby_offline");
				yield return new WaitForSecondsRealtime(10f);
				yield return Scan(WinterPayload("snowbo"));
				yield return new WaitForSecondsRealtime(0.5f);
				yield return Shot("online3_fetching");
				yield return WaitForCollect(25f);
				yield return Shot("online3_reveal");
				yield return Collect(0.8f, "online3_caught");
				Debug.Log($"[Autopilot] online3 done: {library.CheckStatus}, packs {string.Join(",", library.Packs.Select(p => p.Id))}");
				yield break;
			}

			yield return WaitForCheck(library);
			Debug.Log($"[Autopilot] {_phase} check: {library.CheckStatus}, packs {string.Join(",", library.Packs.Select(p => p.Id))}, locked {library.Locked.Count}, "
				+ $"updatesWaiting {library.UpdatesWaiting}, newerApp {library.NewerApp?.Version}");
			yield return new WaitForSecondsRealtime(1.4f);

			if (_phase == "online2")
			{
				yield return Shot("online2_lobby");
				FindAnyObjectByType<LobbyScreen>()?.AlbumsRequested?.Invoke();
				yield return new WaitForSecondsRealtime(1.6f);
				yield return Shot("online2_albums");
				yield break;
			}

			yield return Shot("online_new_collection");
			ClickButton("Open it");
			yield return new WaitForSecondsRealtime(1.4f);
			yield return Shot("online_app_update");
			ClosePopups();
			yield return new WaitForSecondsRealtime(1.2f);
			yield return Shot("online_lobby_winter");

			FindAnyObjectByType<LobbyScreen>()?.AlbumsRequested?.Invoke();
			yield return new WaitForSecondsRealtime(1.6f);
			yield return Shot("online_albums");
			var albumScroll = FindAnyObjectByType<AlbumScreen>()?.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
			if (albumScroll != null)
				albumScroll.verticalNormalizedPosition = 0f;
			yield return new WaitForSecondsRealtime(0.6f);
			yield return Shot("online_albums_bottom");
			FindAnyObjectByType<AlbumScreen>()?.CloseRequested?.Invoke();
			yield return new WaitForSecondsRealtime(0.6f);

			yield return Scan("test-future.robo.ABCDEF");
			yield return new WaitForSecondsRealtime(0.9f);
			yield return Shot("online_locked_scan");
			ClosePopups();
			yield return new WaitForSecondsRealtime(0.6f);

			yield return Scan(WinterPayload("mittens"));
			yield return Collect(0.8f, "online_winter_caught");
		}

		private static IEnumerator WaitForCheck(IPackLibrary library)
		{
			float t = 0f;
			while (t < 40f && (library.CheckStatus == PackCheckStatus.NotChecked || library.CheckStatus == PackCheckStatus.Checking))
			{
				t += Time.unscaledDeltaTime;
				yield return null;
			}
		}

		private static void ClickButton(string label)
		{
			var button = FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
				.FirstOrDefault(b => b.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text == label));
			if (button == null)
				Debug.LogWarning($"[Autopilot] No '{label}' button.");
			button?.Clicked?.Invoke();
		}

		private static Core.UiContext Ctx()
		{
			var root = FindAnyObjectByType<UiRoot>();
			var ctxField = typeof(UiRoot).GetField("_ctx", BindingFlags.NonPublic | BindingFlags.Instance);
			return (Core.UiContext)ctxField!.GetValue(root);
		}

		private static BottomBar Bar() => FindAnyObjectByType<BottomBar>();

		private static CollectionPack? FindOtherPack(string notId)
		{
			var root = FindAnyObjectByType<UiRoot>();
			var ctxField = typeof(UiRoot).GetField("_ctx", BindingFlags.NonPublic | BindingFlags.Instance);
			var ctx = (Core.UiContext)ctxField!.GetValue(root);
			return ctx.Library.Packs.FirstOrDefault(p => p.Id != notId);
		}

		private static CollectionConfig FindConfig()
		{
			var installer = FindAnyObjectByType<SpookySceneInstaller>();
			var field = typeof(SpookySceneInstaller).GetField("_collectionConfig", BindingFlags.NonPublic | BindingFlags.Instance);
			return (CollectionConfig)field!.GetValue(installer);
		}

		/// <summary>Waits until no reveal or popup is on screen, so scans don't pile up behind each other.</summary>
		private static IEnumerator WaitForIdle(float timeout = 25f)
		{
			float t = 0f;
			var layer = GameObject.Find("Popups");
			while (t < timeout && layer != null && layer.transform.childCount > 0)
			{
				t += Time.unscaledDeltaTime;
				yield return null;
			}
		}

		private IEnumerator Scan(string payload)
		{
			yield return WaitForIdle();
			var scanner = FindAnyObjectByType<ScannerScreen>(FindObjectsInactive.Include);
			scanner.PayloadScanned?.Invoke(payload);
			yield return null;
		}

		private IEnumerator Capture(string prefix, params float[] times)
		{
			float elapsed = 0f;
			for (int i = 0; i < times.Length; i++)
			{
				yield return new WaitForSecondsRealtime(times[i] - elapsed);
				elapsed = times[i];
				yield return Shot($"{prefix}_{i + 1}");
			}
		}

		private IEnumerator WaitForCollect(float timeout)
		{
			float t = 0f;
			while (t < timeout && FindCollect() == null)
			{
				t += Time.unscaledDeltaTime;
				yield return null;
			}

			yield return new WaitForSecondsRealtime(0.4f);
		}

		private static ChunkyButton? FindCollect()
			=> FindObjectsByType<ChunkyButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Collect");

		private IEnumerator Collect(float shotDelay, string? shotName)
		{
			yield return WaitForCollect(14f);
			FindCollect()?.Clicked?.Invoke();
			if (shotName != null)
			{
				yield return new WaitForSecondsRealtime(shotDelay);
				yield return Shot(shotName);
			}

			yield return new WaitForSecondsRealtime(1.6f);
		}

		private static void ClosePopups()
		{
			foreach (var popup in FindObjectsByType<Popup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
				popup.Close();
		}

		private IEnumerator Shot(string name)
		{
			yield return new WaitForEndOfFrame();
			var texture = ScreenCapture.CaptureScreenshotAsTexture();
			File.WriteAllBytes(Path.Combine(_outDir, $"{++_shot:00}_{name}.png"), texture.EncodeToPNG());
			Destroy(texture);
		}
	}
}
#endif
