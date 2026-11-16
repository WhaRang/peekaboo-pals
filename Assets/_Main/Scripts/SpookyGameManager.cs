using System.Collections;
using _Main.Scripts._ProjectAgnostic.Ecs.AppLifecycle.Cmp;
using _Main.Scripts._ProjectAgnostic.GameplaySessionLifetime.Service;
using _Main.Scripts._ProjectAgnostic.Providers.SaveLoading;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts._ProjectAgnostic.Services.Input;
using _Main.Scripts.Ecs;
using _Main.Scripts.Ecs.SessionEnding.Cmp;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Ui;
using UnityEngine;
using VContainer;

namespace _Main.Scripts
{
	/// <summary>
	/// The scene-level entry point that drives the ECS runtime: loads the collection packs, starts/stops the session,
	/// ticks the world every frame, and forwards OS app-lifecycle callbacks into the ECS as input events.
	/// <para>One endless session holds every collection. When the host erases everything (a forceful session end, which
	/// clears the save through the template path) a fresh session is started right after the old one is torn down.</para>
	/// Runs after the UI root (execution order) so the UI is subscribed before the first session events are published.
	/// </summary>
	[DefaultExecutionOrder(100)]
	public sealed class SpookyGameManager : MonoBehaviour
	{
		[Tooltip("Start a session automatically on scene start. Disable when a meta/menu layer decides when gameplay begins.")]
		[SerializeField] private bool _autoStartSessionOnStart = true;

		[Tooltip("When auto-starting, restore the previously saved session if one exists.")]
		[SerializeField] private bool _restoreSavedSessionIfAvailable = true;

		private EcsRuntime _ecsRuntime = null!;
		private IGameplayInputService _inputService = null!;
		private IGameplaySessionLifetimeService _gameplaySessionLifetimeService = null!;
		private IGameplayEventBusService _eventBus = null!;
		private IPersistantStorageProvider _storageProvider = null!;
		private IPackLibrary _packLibrary = null!;
		private UiRoot _uiRoot = null!;

		private bool _bootPending;
		private bool _stopRequested;
		private bool _restartAfterStop;

		[Inject]
		public void Construct(
			EcsRuntime ecsRuntime,
			IGameplayInputService inputService,
			IGameplaySessionLifetimeService gameplaySessionLifetimeService,
			IGameplayEventBusService eventBus,
			IPersistantStorageProvider storageProvider,
			IPackLibrary packLibrary,
			UiRoot uiRoot)
		{
			_packLibrary = packLibrary;
			_uiRoot = uiRoot;
			_ecsRuntime = ecsRuntime;
			_inputService = inputService;
			_gameplaySessionLifetimeService = gameplaySessionLifetimeService;
			_eventBus = eventBus;
			_storageProvider = storageProvider;
		}

		private void Awake()
		{
			// Android defaults to 30 fps; the game feel needs 60. Keep the screen awake while walking around hunting.
			QualitySettings.vSyncCount = 0;
#if UNITY_WEBGL && !UNITY_EDITOR
			// In browsers -1 means "every requestAnimationFrame", which is smoother than Unity's own 60 fps timer.
			Application.targetFrameRate = -1;
#else
			Application.targetFrameRate = 60;
#endif
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if !UNITY_WEBGL || UNITY_EDITOR
			// Browsers can't lock orientation (the WebGL page keeps the game portrait itself).
			Screen.orientation = ScreenOrientation.Portrait;
#endif
		}

		private void Start()
		{
			_eventBus.Subscribe<OnGameplaySceneCloseRequestedEvent>(OnGameplaySceneCloseRequested);
			_eventBus.Subscribe<OnGameSessionStartedEvent>(OnGameSessionStarted);
			_eventBus.Subscribe<OnGameSessionEndedEvent>(OnGameSessionEnded);

			// The hunt is untimed and the combo is disabled: the template still publishes these, nobody needs them.
			_eventBus.Subscribe<OnTimeUpdatedEvent>(IgnoreEvent);
			_eventBus.Subscribe<OnComboClearedEvent>(IgnoreEvent);
			_eventBus.Subscribe<OnComboRestoredEvent>(IgnoreEvent);

			// The collections load first (the session restore and the UI both need them), then the UI builds on top.
			_bootPending = _autoStartSessionOnStart;
			StartCoroutine(LoadPacks());
		}

		/// <summary>The packs on the phone first; then, in the background, a look at the website for new ones.</summary>
		private IEnumerator LoadPacks()
		{
			yield return _packLibrary.Load();
#if !UNITY_WEBGL || UNITY_EDITOR
			// The web build just loaded the website's own packs: nothing newer to look for.
			yield return _packLibrary.CheckForUpdates();
#endif
		}

		private void OnDestroy()
		{
			_eventBus.Unsubscribe<OnGameplaySceneCloseRequestedEvent>(OnGameplaySceneCloseRequested);
			_eventBus.Unsubscribe<OnGameSessionStartedEvent>(OnGameSessionStarted);
			_eventBus.Unsubscribe<OnGameSessionEndedEvent>(OnGameSessionEnded);
			_eventBus.Unsubscribe<OnTimeUpdatedEvent>(IgnoreEvent);
			_eventBus.Unsubscribe<OnComboClearedEvent>(IgnoreEvent);
			_eventBus.Unsubscribe<OnComboRestoredEvent>(IgnoreEvent);
		}

		private void Update()
		{
			if (_bootPending)
			{
				if (!_packLibrary.IsReady || !_uiRoot.IsBuilt)
					return;

				_bootPending = false;
				StartSession(restore: _restoreSavedSessionIfAvailable && _storageProvider.Read().IsT0);
				return;
			}

			if (_stopRequested)
			{
				_stopRequested = false;
				StopSession();

				if (_restartAfterStop)
				{
					_restartAfterStop = false;
					StartSession(restore: false);
				}

				return;
			}

#if UNITY_EDITOR
			// Editor convenience: simulate the app being backgrounded (pause + save flush).
			if (Input.GetKeyDown(KeyCode.P))
				_inputService.SendInput(new OnAppBackgroundedEvt());
#endif
			_ecsRuntime.Tick();
		}

		public void StartSession(bool restore = false)
		{
			if (_ecsRuntime.IsRunning)
			{
				Debug.LogError($"[{nameof(SpookyGameManager)}] Trying to start a session while one is already running.");
				return;
			}

			_gameplaySessionLifetimeService.ShouldGameSessionBeRestored = restore;
			_gameplaySessionLifetimeService.InstantiateGameplaySessionGameObjects();
			_ecsRuntime.Start();
			_inputService.UnlockInput();
		}

		private void StopSession()
		{
			_inputService.LockInput();
			_ecsRuntime.Stop();
			_gameplaySessionLifetimeService.DisposeGameplaySessionGameObjects();
		}

		private void OnApplicationPause(bool isPaused)
		{
			if (!_ecsRuntime.IsRunning)
				return;

			if (isPaused)
			{
				_inputService.SendInput(new OnAppBackgroundedEvt());
				// Flush synchronously: this may be the last tick before the process is suspended.
				_ecsRuntime.Tick();
				return;
			}

			_inputService.SendInput(new OnAppForegroundedEvt());
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (Application.isEditor)
				return;
			OnApplicationPause(!hasFocus);
		}

		private void OnGameplaySceneCloseRequested(OnGameplaySceneCloseRequestedEvent evt)
		{
			// Published mid-tick by CloseGameplaySceneSys; defer the teardown to the next Update
			// so the world is not cleared while systems are still iterating it.
			_stopRequested = true;
		}

		private void OnGameSessionStarted(OnGameSessionStartedEvent evt)
		{
			Debug.Log($"[{nameof(SpookyGameManager)}] Session started. Restored from save: {evt.WasRestoredFromSave}");
		}

		private void OnGameSessionEnded(OnGameSessionEndedEvent evt)
		{
			Debug.Log($"[{nameof(SpookyGameManager)}] Session ended. Reason: {evt.Reason}, final score: {evt.FinalScore}");

			// "Erase everything" from the host tools: the save is already cleared, start a brand-new session.
			if (evt.Reason == EndGameReason.ForcefullyEndedByUser)
				_restartAfterStop = true;
		}

		private static void IgnoreEvent<T>(T evt) where T : IEventBusEvent { }
	}
}
