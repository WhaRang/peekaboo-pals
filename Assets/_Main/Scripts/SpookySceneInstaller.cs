using _Main.Scripts._ProjectAgnostic.Ecs.BetterArchApi;
using _Main.Scripts._ProjectAgnostic.Ecs.CommonSystems;
using _Main.Scripts._ProjectAgnostic.Ecs.AppLifecycle.Sys;
using _Main.Scripts._ProjectAgnostic.GameplaySessionLifetime.Factory;
using _Main.Scripts._ProjectAgnostic.GameplaySessionLifetime.Service;
using _Main.Scripts._ProjectAgnostic.Providers.SaveLoading;
using _Main.Scripts._ProjectAgnostic.Providers.Serialization;
using _Main.Scripts._ProjectAgnostic.Providers.Time;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts._ProjectAgnostic.Services.Input;
using _Main.Scripts._ProjectAgnostic.Services.Raycast;
using _Main.Scripts.Configs.Audio;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Configs.Scoring;
using _Main.Scripts.Configs.Time;
using _Main.Scripts.Configs.Ui;
using _Main.Scripts.Ecs;
using _Main.Scripts.Ecs.Collection.Sys;
using _Main.Scripts.Ecs.GameSaving.Sys;
using _Main.Scripts.Ecs.Scoring.Sys;
using _Main.Scripts.Ecs.SessionEnding.Sys;
using _Main.Scripts.Ecs.SessionStarting.Sys;
using _Main.Scripts.Ecs.Time.Sys;
using _Main.Scripts.Services.Audio;
using _Main.Scripts.Services.Floats;
using _Main.Scripts.Services.Haptics;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Services.Permissions;
using _Main.Scripts.Services.Qr;
using _Main.Scripts.Services.Settings;
using _Main.Scripts.Services.Time;
using _Main.Scripts.Ui;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Main.Scripts
{
	/// <summary>
	/// The scene composition root. Registers configs, providers, services, and the ECS pipeline.
	/// The registration order of ECS systems IS their execution order within a tick — keep the
	/// ordering comments honest when inserting game-specific systems.
	/// <para>Peekaboo Pals runs one endless session that holds every collection: no timeout, no combo (min = max = 1 in
	/// the ScoringConfig), completion is per-collection state, "Reset collection" wipes one collection, and "Erase
	/// everything" ends the session forcefully, which clears the save.</para>
	/// </summary>
	public class SpookySceneInstaller : LifetimeScope
	{
		[Header("Components")]
		[SerializeField] private SpookyGameManager _gameManager = null!;
		[SerializeField] private GameplaySessionObjectFactory _gameplaySessionObjectFactory = null!;
		[SerializeField] private UiRoot _uiRoot = null!;

		[Header("Configs")]
		[SerializeField] private TimeConfig _timeConfig = null!;
		[SerializeField] private ScoringConfig _scoringConfig = null!;
		[SerializeField] private CollectionConfig _collectionConfig = null!;
		[SerializeField] private AudioConfig _audioConfig = null!;
		[SerializeField] private UiThemeConfig _uiThemeConfig = null!;

		protected override void Configure(IContainerBuilder builder)
		{
			InstallComponents(builder);
			InstallConfigs(builder);
			InstallProviders(builder);
			InstallServices(builder);
			InstallEcs(builder);
		}

		private void InstallComponents(IContainerBuilder builder)
		{
			builder.RegisterComponent(_gameManager);
			builder.RegisterComponent(_gameplaySessionObjectFactory).AsImplementedInterfaces();
			builder.RegisterComponent(_uiRoot);
		}

		private void InstallConfigs(IContainerBuilder builder)
		{
			builder.RegisterInstance<ITimeConfig>(_timeConfig);
			builder.RegisterInstance<IScoringConfig>(_scoringConfig);
			builder.RegisterInstance<ICollectionRules>(_collectionConfig);
			builder.RegisterInstance<IAudioConfig>(_audioConfig);
			builder.RegisterInstance<IUiThemeConfig>(_uiThemeConfig);

			if (_scoringConfig.MaxComboMultiplier != 1 || _scoringConfig.MinComboMultiplier != 1)
				Debug.LogWarning($"[{nameof(SpookySceneInstaller)}] The hunt expects the combo disabled (Min = Max = 1 in the ScoringConfig); scans would be multiplied otherwise.");
		}

		private void InstallProviders(IContainerBuilder builder)
		{
			builder.Register<LocalServerTimeProvider>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<ConfigGameSessionPauseDurationProvider>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<PlayerPrefsStorageProvider>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<GameplaySerializationProvider>(Lifetime.Singleton).AsImplementedInterfaces();
		}

		private void InstallServices(IContainerBuilder builder)
		{
			builder.Register<RaycastService>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<GameplayInputService>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<GameplayEventBusService>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<GameplayTimeService>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<GameplaySessionLifetimeService>(Lifetime.Singleton).AsImplementedInterfaces();

			// Hunt services
			builder.Register<PackLibrary>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<QrCodeResolver>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<FloatRoller>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<QrCodeImageGenerator>(Lifetime.Singleton).AsImplementedInterfaces();
#if UNITY_WEBGL && !UNITY_EDITOR
			// Browsers: our own getUserMedia camera (WebCamTexture is black in iOS Safari) and jsQR in a Web Worker.
			builder.Register<WebGlQrCameraScanner>(Lifetime.Singleton).AsImplementedInterfaces();
#else
			builder.Register<QrCameraScanner>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<ThreadedZxingQrDecoder>(Lifetime.Singleton).AsImplementedInterfaces();
#endif
			builder.Register<CameraPermissionService>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<SettingsService>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<HapticsService>(Lifetime.Singleton).AsImplementedInterfaces();
			builder.Register<AudioService>(Lifetime.Singleton).AsImplementedInterfaces();
		}

		private void InstallEcs(IContainerBuilder builder)
		{
			builder.Register<EcsRuntime>(Lifetime.Singleton).AsSelf();

			builder.RegisterEcsWorld();
			builder.RegisterEcsBaking();

			// Session lifecycle (initialize singletons first, then load save data / start a new session)
			builder.RegisterEcsSystem<InitializeSessionSys>();
			builder.RegisterEcsSystem<StartSessionSys>();

			// <-- Insert game-specific board/content generation systems here -->

			// App lifecycle (must precede Time + GameSaving so fan-out events flush in one tick)
			builder.RegisterEcsSystem<ProcessAppLifecycleEventsSys>();

			// Time
			builder.RegisterEcsSystem<UpdateGameplayTimeSys>();

			// Collection: scan → new/duplicate/invalid, the completion check and per-collection resets (all feed UpdateScoreReq)
			builder.RegisterEcsSystem<ProcessQrScanSys>();
			builder.RegisterEcsSystem<CheckCollectionCompletionSys>();
			builder.RegisterEcsSystem<ResetCollectionSys>();

			// Scoring (consumes gain/penalty requests produced above)
			builder.RegisterEcsSystem<UpdateScoreSys>();

			// Collection snapshot for the UI (after scoring, so it carries the updated score)
			builder.RegisterEcsSystem<PublishCollectionStateSys>();

			// Session timeout: intentionally NOT registered — a scavenger hunt has no time limit.
			// builder.RegisterEcsSystem<CheckSessionTimeoutSys>();

			// Game Saving
			builder.RegisterEcsSystem<PeriodicSaveSys>();
			builder.RegisterEcsSystem<SaveGameStateSys>();

			// Session ending
			builder.RegisterEcsSystem<EndGameSessionSys>();
			builder.RegisterEcsSystem<CloseGameplaySceneSys>();

			// Cleanup
			builder.RegisterEcsSystem<DestroyEmptyEntitiesSys>();
		}
	}
}
