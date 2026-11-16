using System;
using System.Collections;
using System.Collections.Generic;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts._ProjectAgnostic.Services.Input;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.Collection.Sys;
using _Main.Scripts.Ecs.SessionEnding.Cmp;
using _Main.Scripts.Fx;
using _Main.Scripts.Fx.Particles;
using _Main.Scripts.Fx.Tweening;
using _Main.Scripts.Services.Audio;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Services.Haptics;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Services.Permissions;
using _Main.Scripts.Services.Qr;
using _Main.Scripts.Services.Settings;
using _Main.Scripts.Services.Web;
using _Main.Scripts.Ui.Core;
using _Main.Scripts.Ui.Flow;
using _Main.Scripts.Ui.Navigation;
using _Main.Scripts.Ui.Popups;
using _Main.Scripts.Ui.Screens;
using _Main.Scripts.Ui.Widgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VContainer;

namespace _Main.Scripts.Ui
{
	/// <summary>
	/// The meta/UI layer. Builds the canvas (background, screens, bottom bar, popups, FX) once the collection packs are
	/// loaded, listens to the ECS through the event bus, and turns scan results into "moments" (reveal, duplicate,
	/// invalid, completion) that play one after another. The lobby/results/host screens show one collection at a time
	/// and are rebuilt when the player switches collections on the album shelf (or scans another collection's code).
	/// Gameplay input goes back to the ECS through <see cref="IGameplayInputService"/>.
	/// </summary>
	public sealed class UiRoot : MonoBehaviour
	{
		private const float ReferenceWidth = 1080f;
		private const float ReferenceHeight = 1920f;

		private IGameplayEventBusService _eventBus = null!;
		private IGameplayInputService _input = null!;
		private UiContext _ctx = null!;

		private readonly Dictionary<MainTab, ScreenBase> _screens = new();
		private LobbyScreen _lobby = null!;
		private ScannerScreen _scanner = null!;
		private ResultsScreen _results = null!;
		private HostScreen _host = null!;
		private AlbumScreen _album = null!;
		private AmbientBackground _background = null!;
		private RectTransform _screensRoot = null!;
		private RectTransform _hostLayer = null!;
		private BottomBar _bottomBar = null!;
		private CanvasScaler _scaler = null!;
		private RectTransform _overlayLayer = null!;
		private MainTab _currentTab = MainTab.Collection;

		private readonly Queue<Func<IEnumerator>> _moments = new();
		/// <summary>(pack, character) reveals queued but not landed yet: their cards keep the old state until then.</summary>
		private readonly List<(string PackId, string CharacterId)> _pendingReveals = new();
		private bool _momentRunning;
		private bool _hasSnapshot;
		private bool _resetRequested;
		private bool _splashDone;
		private string? _launchCode;
		private float _awaitingScanSince = -1f;
		private string? _lastPayload;
		/// <summary>The collection a scanned code is being downloaded for (its arrival plays the reveal, not a notice).</summary>
		private string? _fetchingPackId;
		private readonly List<CollectionPack> _arrivedPacks = new();
		private float _lastAutoCheck;

		/// <summary>An open app re-checks the website for new collections when it comes back to the foreground after this long.</summary>
		private const float AutoCheckIntervalSeconds = 300f;

		/// <summary>True once the canvas exists; the game manager starts the session only then.</summary>
		public bool IsBuilt { get; private set; }

		[Inject]
		public void Construct(
			IUiThemeConfig theme,
			IPackLibrary library,
			IAudioService audio,
			IHapticsService haptics,
			ISettingsService settings,
			IQrCodeImageGenerator qrGenerator,
			IQrCameraScanner scanner,
			ICameraPermissionService cameraPermission,
			IGameplayInputService input,
			IGameplayEventBusService eventBus)
		{
			_eventBus = eventBus;
			_input = input;
			_ctx = new UiContext(theme, library, audio, haptics, settings, qrGenerator, scanner, cameraPermission, input, this);
		}

		private IEnumerator Start()
		{
			while (!_ctx.Library.IsReady)
				yield return null;

			_ctx.Pack = _ctx.Library.TryGetPack(_ctx.Settings.ViewedPackId, out var lastViewed) ? lastViewed : _ctx.Library.DefaultPack;
			if (QrPayloadCodec.TryDecodeLaunchUrl(WebPlatform.LaunchUrl, out var launchCode))
				_launchCode = launchCode;

			BuildCanvas();

			_eventBus.Subscribe<OnCollectionStateChangedEvent>(OnCollectionStateChanged);
			_eventBus.Subscribe<OnQrScanResolvedEvent>(OnQrScanResolved);
			_eventBus.Subscribe<OnCollectionCompletedEvent>(OnCollectionCompleted);
			_eventBus.Subscribe<OnScoreUpdatedEvent>(OnScoreUpdated);

			_ctx.Settings.Changed += ApplySettings;
			ApplySettings();
			_ctx.Library.PackAdded += OnPackAdded;
			_ctx.Library.Changed += OnLibraryChanged;
			_lastAutoCheck = Time.realtimeSinceStartup;

			ShowTab(MainTab.Collection, animate: false);
			IsBuilt = true;
			StartCoroutine(SplashOverlay.Play(_ctx, _overlayLayer, OnSplashDone));
		}

		private void OnSplashDone()
		{
			_splashDone = true;
			TryCatchLaunchCode();
			ShowNotices();
		}

		/// <summary>The web build was opened by scanning a tag with the phone's own camera: catch it right away.</summary>
		private void TryCatchLaunchCode()
		{
			if (_launchCode == null || !_splashDone || !_hasSnapshot)
				return;

			var code = _launchCode;
			_launchCode = null;
			WebPlatform.ClearLaunchCode();
			OnPayloadScanned(code);
		}

		private void OnDestroy()
		{
			if (_eventBus == null)
				return;

			_eventBus.Unsubscribe<OnCollectionStateChangedEvent>(OnCollectionStateChanged);
			_eventBus.Unsubscribe<OnQrScanResolvedEvent>(OnQrScanResolved);
			_eventBus.Unsubscribe<OnCollectionCompletedEvent>(OnCollectionCompleted);
			_eventBus.Unsubscribe<OnScoreUpdatedEvent>(OnScoreUpdated);
			_ctx.Settings.Changed -= ApplySettings;
			_ctx.Library.PackAdded -= OnPackAdded;
			_ctx.Library.Changed -= OnLibraryChanged;
		}

		// ------------------------------------------------------------------ Canvas

		private void BuildCanvas()
		{
			var canvasGo = new GameObject("PartyCanvas", typeof(RectTransform));
			canvasGo.transform.SetParent(transform, false);
			canvasGo.layer = 5;
			var canvas = canvasGo.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 0;
			_scaler = canvasGo.AddComponent<CanvasScaler>();
			_scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			_scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
			UpdateScalerMatch();
			canvasGo.AddComponent<GraphicRaycaster>();
			_ctx.Canvas = canvas;

			EnsureEventSystem();

			var canvasRect = (RectTransform)canvasGo.transform;

			// Every layer that animates gets its own nested canvas, so a moving particle or scan line only rebuilds
			// its own layer instead of the whole UI (the biggest per-frame UI cost on mid-range phones).
			_background = AmbientBackground.Create(canvasRect, _ctx);
			_background.ApplyTheme(_ctx.Pack.Theme);
			Isolate(_background, interactive: false);

			var shakeRoot = UiBuilder.Rect("ShakeRoot", canvasRect);
			shakeRoot.Stretch();
			var safeArea = UiBuilder.Rect("SafeArea", shakeRoot);
			safeArea.Stretch();
			safeArea.gameObject.AddComponent<SafeAreaFitter>();

			_screensRoot = UiBuilder.Rect("Screens", safeArea);
			_screensRoot.Stretch(0f, 0f, 0f, BottomBar.Height - 70f);

			Isolate(_screensRoot, interactive: true);
			_scanner = CreateScreen<ScannerScreen>(_screensRoot, "Scanner");
			_screens[MainTab.Scanner] = _scanner;

			_bottomBar = BottomBar.Create(safeArea, _ctx);
			Isolate(_bottomBar, interactive: true);
			_bottomBar.TabSelected = tab => ShowTab(tab, animate: true);

			_hostLayer = UiBuilder.Rect("HostLayer", safeArea);
			_hostLayer.Stretch();
			Isolate(_hostLayer, interactive: true);
			_album = CreateScreen<AlbumScreen>(_hostLayer, "Albums");
			_album.CloseRequested = CloseAlbum;
			_album.PackSelected = OnAlbumPackSelected;
			CreatePackScreens();

			_ctx.PopupLayer = UiBuilder.Rect("Popups", safeArea);
			_ctx.PopupLayer.Stretch();
			Isolate(_ctx.PopupLayer, interactive: true);

			var fxLayer = UiBuilder.Rect("Fx", canvasRect);
			fxLayer.Stretch();
			Isolate(fxLayer, interactive: false);
			_ctx.FxLayer = fxLayer;
			var particles = UiBuilder.Rect("Particles", fxLayer).gameObject.AddComponent<UiParticleGraphic>();
			particles.rectTransform.Stretch();
			particles.Setup(_ctx.Theme.ParticleAtlas, _ctx.Theme.ParticleAtlasColumns, _ctx.Theme.ParticleAtlasRows, 1400);
			_ctx.FxParticles = particles;

			var dim = UiBuilder.Image(canvasRect, "Dim", (Sprite?)null, Color.clear);
			dim.rectTransform.Stretch();
			var flash = UiBuilder.Image(canvasRect, "Flash", (Sprite?)null, Color.clear);
			flash.rectTransform.Stretch();
			var screenFx = canvasGo.AddComponent<ScreenEffects>();
			screenFx.Setup(shakeRoot, flash, dim);
			_ctx.ScreenFx = screenFx;

			_overlayLayer = UiBuilder.Rect("Overlay", canvasRect);
			_overlayLayer.Stretch();
			Isolate(_overlayLayer, interactive: true);

			_scanner.PayloadScanned = OnPayloadScanned;
		}

		/// <summary>The screens that show one collection: built for <see cref="UiContext.Pack"/>, rebuilt on a switch.</summary>
		private void CreatePackScreens()
		{
			_lobby = CreateScreen<LobbyScreen>(_screensRoot, "Lobby");
			_results = CreateScreen<ResultsScreen>(_screensRoot, "Results");
			_lobby.Rect.SetSiblingIndex(0);
			_results.Rect.SetSiblingIndex(1);
			_screens[MainTab.Collection] = _lobby;
			_screens[MainTab.Results] = _results;

			_host = CreateScreen<HostScreen>(_hostLayer, "Host");
			_host.gameObject.SetActive(false);

			_lobby.HostToolsRequested = OpenHost;
			_lobby.AlbumsRequested = OpenAlbum;
			_host.CloseRequested = CloseHost;
			_host.ResetCollectionConfirmed = ResetCollection;
			_host.EraseEverythingConfirmed = EraseEverything;
		}

		/// <summary>Shows another collection: new theme, freshly built lobby/results/host screens, same tab.</summary>
		private void SwitchPack(CollectionPack pack)
		{
			if (pack == _ctx.Pack)
				return;

			_ctx.Pack = pack;
			_ctx.Settings.ViewedPackId = pack.Id;

			bool hostShown = _host.IsShown;
			foreach (var screen in new ScreenBase[] { _lobby, _results, _host })
			{
				screen.SetShown(false);
				Destroy(screen.gameObject);
			}

			CreatePackScreens();
			_background.ApplyTheme(pack.Theme);
			_lobby.ApplySnapshot(_ctx.Snapshot, animate: false, pendingId: PendingIdFor(pack));

			var tab = _currentTab;
			foreach (var pair in _screens)
			{
				if (pair.Key != tab)
					pair.Value.gameObject.SetActive(false);
			}

			_screens[tab].gameObject.SetActive(false);
			ShowTab(tab, animate: false);
			if (hostShown)
				OpenHost();
		}

		private string? PendingIdFor(ICollectionPack pack)
		{
			foreach (var (packId, characterId) in _pendingReveals)
			{
				if (string.Equals(packId, pack.Id, StringComparison.OrdinalIgnoreCase))
					return characterId;
			}

			return null;
		}

		private static void Isolate(Component layer, bool interactive)
		{
			if (layer.GetComponent<Canvas>() == null)
				layer.gameObject.AddComponent<Canvas>();
			if (interactive && layer.GetComponent<GraphicRaycaster>() == null)
				layer.gameObject.AddComponent<GraphicRaycaster>();
		}

		private T CreateScreen<T>(RectTransform parent, string name) where T : ScreenBase
		{
			var rect = UiBuilder.Rect(name, parent);
			rect.Stretch();
			// Each screen batches separately; the scanner's animated overlay no longer dirties the lobby and vice versa.
			Isolate(rect, interactive: true);
			var screen = rect.gameObject.AddComponent<T>();
			screen.Initialize(_ctx);
			screen.gameObject.SetActive(false);
			return screen;
		}

		private static void EnsureEventSystem()
		{
			if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
				return;

			var go = new GameObject("EventSystem", typeof(EventSystem));
			var module = go.AddComponent<InputSystemUIInputModule>();
			module.AssignDefaultActions();
		}

		private void UpdateScalerMatch()
		{
			// Phones (taller than 16:9) keep the full 1080 width; wider screens (tablets) fit the height instead.
			float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
			_scaler.matchWidthOrHeight = aspect > ReferenceWidth / ReferenceHeight + 0.01f ? 1f : 0f;
		}

		private void Update()
		{
			if (!IsBuilt)
				return;

			UpdateScalerMatch();
			HandleBackButton();

			// Safety net: if a scanned payload never produced a result (session restarting), resume scanning.
			if (_awaitingScanSince >= 0f && Time.unscaledTime - _awaitingScanSince > 2.5f && !_momentRunning && _moments.Count == 0)
			{
				_awaitingScanSince = -1f;
				_scanner.Suspended = false;
			}
		}

		/// <summary>Android back: close host tools/albums, then the top popup, then return to the collection. Never interrupts a reveal.</summary>
		private void HandleBackButton()
		{
			if (!Input.GetKeyDown(KeyCode.Escape))
				return;

			if (_host.IsShown)
			{
				CloseHost();
				return;
			}

			if (_album.IsShown)
			{
				CloseAlbum();
				return;
			}

			var popups = _ctx.PopupLayer.GetComponentsInChildren<Popup>();
			if (popups.Length > 0)
			{
				popups[popups.Length - 1].Close();
				return;
			}

			if (!_momentRunning && _currentTab != MainTab.Collection)
				ShowTab(MainTab.Collection, animate: true);
		}

		private void ApplySettings()
		{
			_ctx.ScreenFx.ReducedMotion = _ctx.ReducedMotion;
			_ctx.FxParticles.CountScale = _ctx.ReducedMotion ? 0.35f : 1f;
		}

		// ------------------------------------------------------------------ Navigation

		private void ShowTab(MainTab tab, bool animate)
		{
			var previous = _screens[_currentTab];
			var next = _screens[tab];
			bool changed = previous != next || !next.gameObject.activeSelf;
			int direction = Math.Sign((int)tab - (int)_currentTab);
			_currentTab = tab;
			_bottomBar.SetActive(tab, animate);

			if (!changed)
			{
				if (animate)
					next.Rect.PunchScale(0.03f, 0.3f, 2);
				return;
			}

			if (previous != next)
			{
				previous.SetShown(false);
				if (animate)
				{
					var prevRect = previous.Rect;
					var prevGroup = previous.Group;
					prevGroup.blocksRaycasts = false;
					if (!_ctx.ReducedMotion)
						prevRect.MoveTo(new Vector2(-direction * 260f, 0f), 0.25f, Ease.InCubic);
					prevGroup.FadeTo(0f, 0.22f).OnComplete(() =>
					{
						if (_screens[_currentTab] != previous)
							previous.gameObject.SetActive(false);
					});
				}
				else
				{
					previous.gameObject.SetActive(false);
				}
			}

			next.gameObject.SetActive(true);
			next.Rect.SetAsLastSibling();
			next.Group.blocksRaycasts = true;
			if (animate)
			{
				next.Group.alpha = 0f;
				next.Group.FadeTo(1f, 0.28f);
				if (!_ctx.ReducedMotion)
				{
					next.Rect.anchoredPosition = new Vector2(direction * 300f, 0f);
					next.Rect.MoveTo(Vector2.zero, 0.38f, Ease.OutBackStrong);
				}
				else
				{
					next.Rect.anchoredPosition = Vector2.zero;
				}
			}
			else
			{
				Tweens.Kill(next.Group);
				Tweens.Kill(next.Rect);
				next.Group.alpha = 1f;
				next.Rect.anchoredPosition = Vector2.zero;
			}

			next.SetShown(true);
		}

		private void OpenHost()
		{
			_host.gameObject.SetActive(true);
			_host.Group.alpha = 0f;
			_host.Group.FadeTo(1f, 0.25f);
			if (!_ctx.ReducedMotion)
				_host.Rect.ScaleFromTo(0.9f, 1f, 0.35f, Ease.OutBackStrong);
			_host.SetShown(true);
			_scanner.Suspended = true;
		}

		private void CloseHost()
		{
			var host = _host;
			host.SetShown(false);
			host.Group.FadeTo(0f, 0.2f).OnComplete(() =>
			{
				if (host != null)
					host.gameObject.SetActive(false);
			});
			_scanner.Suspended = _momentRunning || _album.IsShown;
		}

		private void OpenAlbum()
		{
			// Opening the shelf while offline is a good moment to try the website again.
			if (_ctx.Library.CheckStatus == PackCheckStatus.Offline && Time.realtimeSinceStartup - _ctx.Library.LastCheckTime > 20f)
				CheckForUpdatesNow();

			_album.gameObject.SetActive(true);
			_album.Rect.SetAsLastSibling();
			_album.Group.alpha = 0f;
			_album.Group.FadeTo(1f, 0.25f);
			if (!_ctx.ReducedMotion)
				_album.Rect.ScaleFromTo(0.92f, 1f, 0.35f, Ease.OutBackStrong);
			_album.SetShown(true);
			_scanner.Suspended = true;
		}

		private void CloseAlbum()
		{
			_album.SetShown(false);
			_album.Group.FadeTo(0f, 0.2f).OnComplete(() => _album.gameObject.SetActive(false));
			_scanner.Suspended = _momentRunning || _host.IsShown;
		}

		private void OnAlbumPackSelected(CollectionPack pack)
		{
			CloseAlbum();
			if (_momentRunning)
				return;

			SwitchPack(pack);
			if (_currentTab == MainTab.Scanner)
				ShowTab(MainTab.Collection, animate: true);
			else if (!_ctx.ReducedMotion)
				_screens[_currentTab].Rect.ScaleFromTo(0.94f, 1f, 0.35f, Ease.OutBackStrong);
		}

		/// <summary>Host tools: wipe the collection on screen (other collections stay).</summary>
		private void ResetCollection()
		{
			_resetRequested = true;
			_moments.Clear();
			_pendingReveals.Clear();
			_input.SendInput(new ResetCollectionReq { PackId = _ctx.Pack.Id });
			CloseHost();
			ShowTab(MainTab.Collection, animate: true);
		}

		/// <summary>Host tools: wipe every collection (a forceful session end clears the save; a new session starts).</summary>
		private void EraseEverything()
		{
			_resetRequested = true;
			_moments.Clear();
			_pendingReveals.Clear();
			_input.SendInput(new EndGameSessionReq { Reason = EndGameReason.ForcefullyEndedByUser });
			CloseHost();
			ShowTab(MainTab.Collection, animate: true);
		}

		// ------------------------------------------------------------------ ECS → UI

		private void OnCollectionStateChanged(OnCollectionStateChangedEvent evt)
		{
			_ctx.Album = evt.Album;
			var snapshot = _ctx.Snapshot;
			bool isReset = _resetRequested && snapshot.CollectedCount == 0;

			var pendingId = PendingIdFor(_ctx.Pack);
			if (pendingId == null)
				_lobby.ApplySnapshot(snapshot, animate: _hasSnapshot && !isReset);
			else
				_lobby.ApplySnapshot(snapshot, animate: false, pendingId: pendingId);

			if (_results.IsShown)
				_results.Refresh(snapshot, animate: false);
			if (_album.IsShown)
				_album.Refresh();

			_hasSnapshot = true;
			TryCatchLaunchCode();
			ShowNotices();

			if (isReset)
			{
				_resetRequested = false;
				var popup = Popup.Create(_ctx, new Vector2(860f, 640f), _ctx.Palette.Slime.Darken(0.2f));
				popup.AddTitle("Fresh start!", 40f, 90f);
				popup.AddBody("Progress on this phone was reset. Happy hunting!", 230f, 150f);
				popup.AddButton("Let's go!", _ctx.Palette.Purple, () => { });
			}
		}

		private void OnScoreUpdated(OnScoreUpdatedEvent evt)
		{
			// The header animates from collection snapshots (so it can wait for the reveal); nothing to do here.
		}

		private void OnPayloadScanned(string payload)
		{
			_lastPayload = payload;
			_scanner.Suspended = true;
			_awaitingScanSince = Time.unscaledTime;
			_input.SendInput(new ScanQrCodeReq { RawPayload = payload });
		}

		private void OnQrScanResolved(OnQrScanResolvedEvent evt)
		{
			_awaitingScanSince = -1f;

			// A code from a collection this phone doesn't have yet: ask the website for it, then catch the pal.
			if (evt.Outcome == QrScanOutcome.Invalid && evt.RejectionReason == QrRejectionReason.UnknownPack
				&& QrPayloadCodec.TryDecode(_lastPayload, out var decoded) && decoded.PackId.Length > 0)
			{
				var payload = _lastPayload!;
				Enqueue(() => FetchPackMoment(decoded.PackId, payload));
				return;
			}

			if (evt.Outcome == QrScanOutcome.Invalid || !_ctx.Library.TryGetPack(evt.PackId, out var pack) || !pack.TryGetCharacter(evt.CharacterId, out var character))
			{
				var reason = evt.RejectionReason;
				Enqueue(() => PopupMoment(done => MessagePopups.Invalid(_ctx, reason, done)));
				return;
			}

			// Another collection's code: show that collection (right away when idle, otherwise when its moment starts).
			if (pack != _ctx.Pack && !_momentRunning)
				SwitchPack(pack);

			switch (evt.Outcome)
			{
				case QrScanOutcome.NewCharacter:
				{
					int tierPoints = pack.GetPoints(character);
					var caught = new RevealSequence.Catch(evt.Float, tierPoints, evt.PointsAwarded - tierPoints, evt.PointsAwarded, null);
					QueueReveal(character, caught);
					break;
				}
				case QrScanOutcome.FloatUpgrade:
				{
					var caught = new RevealSequence.Catch(evt.Float, 0, CollectionRules.FloatBonus(pack, evt.Float), evt.PointsAwarded, evt.PreviousBestFloat);
					QueueReveal(character, caught);
					break;
				}
				case QrScanOutcome.NoImprovement:
					Enqueue(() => ShowPack(pack, () => PopupMoment(done => MessagePopups.NoImprovement(_ctx, character, evt.Float, evt.PreviousBestFloat, done))));
					break;
				case QrScanOutcome.OnCooldown:
					Enqueue(() => ShowPack(pack, () => PopupMoment(done => MessagePopups.Resting(_ctx, character, evt.PreviousBestFloat, evt.CooldownRemainingSeconds, done))));
					break;
			}
		}

		private void QueueReveal(CharacterDefinition character, RevealSequence.Catch caught)
		{
			_pendingReveals.Add((character.PackId, character.Id));
			if (character.PackId == _ctx.Pack.Id)
				_lobby.ApplySnapshot(_ctx.Snapshot, animate: false, pendingId: PendingIdFor(_ctx.Pack));
			Enqueue(() => RevealMoment(character, caught));
		}

		private void OnCollectionCompleted(OnCollectionCompletedEvent evt)
		{
			if (!_ctx.Library.TryGetPack(evt.PackId, out var pack))
				return;

			Enqueue(() => ShowPack(pack, () => CompletionCelebration.Play(_ctx, evt.BonusPoints, evt.CompletionDurationInSeconds, () => ShowTab(MainTab.Results, animate: true))));
		}

		/// <summary>Makes sure the moment plays over its own collection.</summary>
		private IEnumerator ShowPack(CollectionPack pack, Func<IEnumerator> moment)
		{
			SwitchPack(pack);
			yield return moment();
		}

		// ------------------------------------------------------------------ Collections from the website

		private void OnPackAdded(CollectionPack pack)
		{
			// The very first collection on a phone that had none: show it right away.
			if (_ctx.Pack.Characters.Count == 0 && !_momentRunning)
				SwitchPack(pack);
			if (_album.IsShown)
				_album.Refresh(animate: false);

			if (!string.Equals(pack.Id, _fetchingPackId, StringComparison.OrdinalIgnoreCase))
			{
				_arrivedPacks.Add(pack);
				ShowNotices();
			}
		}

		private void OnLibraryChanged()
		{
			if (_album.IsShown)
				_album.Refresh(animate: false);
			ShowNotices();
		}

		/// <summary>"A new collection!" and "A new version is out!" popups, once the splash is gone and the save is shown.</summary>
		private void ShowNotices()
		{
			// While a scanned code's collection downloads, its reveal goes first; the notices follow after the catch.
			if (!_splashDone || !_hasSnapshot || _fetchingPackId != null)
				return;

			foreach (var pack in _arrivedPacks)
			{
				Enqueue(() => PopupMoment(done => PackPopups.NewCollection(_ctx, pack, () =>
				{
					SwitchPack(pack);
					ShowTab(MainTab.Collection, animate: true);
				}, done)));
			}

			_arrivedPacks.Clear();

			var release = _ctx.Library.NewerApp;
			if (release != null && _ctx.Settings.SeenAppVersion != release.Version)
			{
				_ctx.Settings.SeenAppVersion = release.Version;
				Enqueue(() => PopupMoment(done => PackPopups.AppUpdate(_ctx, release, done)));
			}
		}

		private IEnumerator FetchPackMoment(string packId, string payload)
		{
			var library = _ctx.Library;
			if (!library.TryGetLocked(packId, out _) && library.CheckStatus != PackCheckStatus.Off)
			{
				bool cancelled = false;
				bool finished = false;
				_fetchingPackId = packId;
				var popup = PackPopups.Fetching(_ctx, () => cancelled = true);
				StartCoroutine(RunThen(library.CheckForUpdates(packId), () => finished = true));
				float shownAt = Time.unscaledTime;
				// Go on as soon as this collection is in (the rest of the check carries on in the background), but
				// linger a little even when it's instant, so the popup doesn't just flash.
				while (!cancelled && ((!finished && !library.TryGetPack(packId, out _)) || Time.unscaledTime - shownAt < 0.9f))
					yield return null;

				_fetchingPackId = null;
				if (cancelled)
				{
					ShowNotices();
					yield break;
				}

				popup.Close();
				yield return new WaitForSecondsRealtime(0.2f);
			}

			if (library.TryGetPack(packId, out _))
			{
				// Scan it again: its reveal is queued as the next moment.
				OnPayloadScanned(payload);
				float sentAt = Time.unscaledTime;
				while (_awaitingScanSince >= 0f && Time.unscaledTime - sentAt < 3f)
					yield return null;
				yield break;
			}

			if (library.TryGetLocked(packId, out var locked))
				yield return PopupMoment(done => PackPopups.NeedsAppUpdate(_ctx, locked, library.NewerApp, done));
			else
			{
				bool offline = library.CheckStatus == PackCheckStatus.Offline;
				yield return PopupMoment(done => MessagePopups.Invalid(_ctx, QrRejectionReason.UnknownPack, done, offline));
			}

			ShowNotices();
		}

		private void CheckForUpdatesNow()
		{
			_lastAutoCheck = Time.realtimeSinceStartup;
			StartCoroutine(_ctx.Library.CheckForUpdates(null));
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (!hasFocus || !IsBuilt || _ctx.Library.CheckStatus == PackCheckStatus.Off)
				return;

			float since = Time.realtimeSinceStartup - Mathf.Max(_lastAutoCheck, _ctx.Library.LastCheckTime);
			if (since > AutoCheckIntervalSeconds)
				CheckForUpdatesNow();
		}

		private static IEnumerator RunThen(IEnumerator routine, Action done)
		{
			yield return routine;
			done();
		}

		// ------------------------------------------------------------------ Moments

		private void Enqueue(Func<IEnumerator> moment)
		{
			_moments.Enqueue(moment);
			_scanner.Suspended = true;
			if (!_momentRunning)
				StartCoroutine(RunMoments());
		}

		private IEnumerator RunMoments()
		{
			_momentRunning = true;
			while (_moments.Count > 0)
			{
				var moment = _moments.Dequeue();
				yield return moment();
			}

			_momentRunning = false;
			_bottomBar.SetInteractable(true);
			_scanner.Suspended = _host.IsShown || _album.IsShown;
		}

		private IEnumerator RevealMoment(CharacterDefinition character, RevealSequence.Catch caught)
		{
			if (_ctx.Library.TryGetPack(character.PackId, out var pack))
				SwitchPack(pack);
			_bottomBar.SetInteractable(false);
			// Reveal over the collection, not the camera: the scanner's own texts would clash with the reveal's.
			ShowTab(MainTab.Collection, animate: false);
			_lobby.ScrollToCard(character.Id);

			yield return RevealSequence.Play(_ctx, character, caught,
				getSlot: () =>
				{
					ShowTab(MainTab.Collection, animate: false);
					_lobby.ScrollToCard(character.Id);
					return _lobby.GetCard(character.Id);
				},
				onLanded: () =>
				{
					_pendingReveals.Remove((character.PackId, character.Id));
					_lobby.ApplySnapshot(_ctx.Snapshot, animate: true, pendingId: PendingIdFor(_ctx.Pack));
				});
			_bottomBar.SetInteractable(true);
		}

		private static IEnumerator PopupMoment(Func<Action, Popup> show)
		{
			bool closed = false;
			show(() => closed = true);
			while (!closed)
				yield return null;
		}
	}

	/// <summary>Keeps its rect inside <see cref="Screen.safeArea"/> (notches, rounded corners, gesture bars).</summary>
	public sealed class SafeAreaFitter : MonoBehaviour
	{
		private Rect _applied;

		private void Update()
		{
			var safe = Screen.safeArea;
			if (safe == _applied || Screen.width <= 0 || Screen.height <= 0)
				return;

			_applied = safe;
			var rect = (RectTransform)transform;
			rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
			rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}
	}
}
