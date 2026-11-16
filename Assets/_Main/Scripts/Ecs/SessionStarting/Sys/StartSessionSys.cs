using System;
using System.Collections.Generic;
using _Main.Scripts._ProjectAgnostic.Ecs.BetterArchApi;
using _Main.Scripts._ProjectAgnostic.Ecs.UnityIntegration.Systems;
using _Main.Scripts._ProjectAgnostic.GameplaySessionLifetime.Service;
using _Main.Scripts._ProjectAgnostic.Providers.SaveLoading;
using _Main.Scripts._ProjectAgnostic.Providers.Serialization;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.Collection.Sys;
using _Main.Scripts.Ecs.GameSaving.Data;
using _Main.Scripts.Ecs.Scoring.Cmp;
using _Main.Scripts.Ecs.Time.Cmp;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Services.Packs;
using Arch.Core;
using UnityEngine;

namespace _Main.Scripts.Ecs.SessionStarting.Sys
{
	/// <summary>
	/// Starts the gameplay session on top of the singletons created by <see cref="InitializeSessionSys"/>:
	/// either loads the persisted save data into them, or initializes a fresh session. Extend
	/// <see cref="TryRestoreFromSaveData"/> alongside <c>SaveGameStateSys</c> when adding
	/// game-specific save sections.
	/// </summary>
	public sealed class StartSessionSys : IEcsInitSystem
	{
		private readonly World _world;
		private readonly IGameplaySessionLifetimeService _gameplaySessionLifetimeService;
		private readonly IGameplaySerializationProvider _serializationProvider;
		private readonly IPersistantStorageProvider _storageProvider;
		private readonly IGameplayEventBusService _eventBus;
		private readonly IPackLibrary _library;

		/// <summary>Saves made before collection packs held Halloween 2026 only.</summary>
		public const string PrePackSavePackId = "halloween-2026";

		public StartSessionSys(
			World world,
			IGameplaySessionLifetimeService gameplaySessionLifetimeService,
			IGameplaySerializationProvider serializationProvider,
			IPersistantStorageProvider storageProvider,
			IGameplayEventBusService eventBus,
			IPackLibrary library)
		{
			_world = world;
			_gameplaySessionLifetimeService = gameplaySessionLifetimeService;
			_serializationProvider = serializationProvider;
			_storageProvider = storageProvider;
			_eventBus = eventBus;
			_library = library;
		}

		public void OnInit()
		{
			bool wasRestored = TryRestoreFromSaveData();

			if (!wasRestored)
				_world.Create(new RecordSessionStartReq());

			_eventBus.Publish(new OnGameSessionStartedEvent(wasRestored));
		}

		/// <summary>
		/// Restores the session state from the save payload. The score/combo fields are written directly
		/// into the live <see cref="ScoreSinglCmp"/> here (rather than deferred via an <see cref="UpdateScoreReq"/>
		/// payload) so that <see cref="InitializeTimeAfterLoadedSaveReq"/> — processed by
		/// <c>UpdateGameplayTimeSys</c> ahead of <c>UpdateScoreSys</c> in the same tick — sees the restored combo
		/// state and can correctly shift/clear/restore it. The <see cref="UpdateScoreReq"/> raised afterwards only
		/// triggers a UI refresh; it carries no payload.
		/// </summary>
		private bool TryRestoreFromSaveData()
		{
			if (!_gameplaySessionLifetimeService.ShouldGameSessionBeRestored)
				return false;

			var storageReadResult = _storageProvider.Read();
			if (storageReadResult.IsT1)
				return false;

			var deserializationResult = _serializationProvider.Deserialize<GameSaveData>(storageReadResult.AsT0);
			if (deserializationResult.IsT1)
			{
				Debug.LogError($"[{nameof(StartSessionSys)}] Failed to deserialize save data: {deserializationResult.AsT1.Value}");
				return false;
			}

			var saveData = deserializationResult.AsT0.Value;

			if (saveData.Time.SessionStartTimeStamp <= 0)
			{
				Debug.LogError($"[{nameof(StartSessionSys)}] Save data carries no started session. Starting a new session instead.");
				return false;
			}

			RestoreTime(saveData.Time);
			RestoreScore(saveData.Score);
			RestoreCollections(saveData);

			_world.Create(new InitializeTimeAfterLoadedSaveReq());
			_world.Create(new UpdateScoreReq { Kind = ScoreUpdateRequestKind.Restore });

			return true;
		}

		private void RestoreTime(TimeSaveData timeSaveData)
		{
			ref var timeCmp = ref _world.GetSinglCmp<GameplayTimeSinglCmp>();
			timeCmp.SessionStartTimeStamp = timeSaveData.SessionStartTimeStamp;
			timeCmp.LastRecordedServerTimeStamp = timeSaveData.LastRecordedServerTimeStamp;
			timeCmp.RemainingPauseDurationInSeconds = timeSaveData.RemainingPauseDurationInSeconds;
		}

		private void RestoreScore(ScoreSaveData scoreSaveData)
		{
			ref var scoreCmp = ref _world.GetSinglCmp<ScoreSinglCmp>();
			scoreCmp.CurrentScore = scoreSaveData.CurrentScore;
			scoreCmp.CurrentMultiplier = scoreSaveData.CurrentMultiplier;
			scoreCmp.ComboActivated = scoreSaveData.ComboActivated;
			scoreCmp.LastComboTriggerTimestamp = scoreSaveData.LastComboTriggerTimestampUnixSeconds.HasValue
				? DateTimeOffset.FromUnixTimeSeconds(scoreSaveData.LastComboTriggerTimestampUnixSeconds.Value).UtcDateTime
				: (DateTime?)null;
		}

		/// <summary>
		/// Restores every collection. A pre-pack save (one Halloween collection) becomes the Halloween 2026 pack. Characters
		/// that left a known pack are dropped; packs this build doesn't have are kept untouched. The total score is then
		/// recomputed from the packs, so a rebalance of tier points applies to existing progress too.
		/// </summary>
		private void RestoreCollections(GameSaveData saveData)
		{
			ref var collectionCmp = ref _world.GetSinglCmp<CollectionSinglCmp>();

			var packs = saveData.Packs;
			if (packs == null && saveData.Collection.Collected != null)
			{
				var legacy = saveData.Collection;
				legacy.PackId = PrePackSavePackId;
				packs = new List<CollectionSaveData> { legacy };
				Debug.Log($"[{nameof(StartSessionSys)}] Migrated the pre-pack save into '{PrePackSavePackId}'.");
			}

			foreach (var packSave in packs ?? new List<CollectionSaveData>())
			{
				if (string.IsNullOrEmpty(packSave.PackId) || packSave.Collected == null)
					continue;

				bool known = _library.TryGetPack(packSave.PackId!, out var pack);
				var progress = collectionCmp.GetOrCreate(known ? pack.Id : packSave.PackId!);

				foreach (var entry in packSave.Collected)
				{
					string id = entry.Id;
					if (string.IsNullOrEmpty(id))
						continue;

					if (known)
					{
						if (!pack.TryGetCharacter(id, out var character))
						{
							Debug.LogWarning($"[{nameof(StartSessionSys)}] Dropping saved character '{packSave.PackId}/{id}': not in the pack anymore.");
							continue;
						}

						id = character.Id;
					}

					progress.Caught[id] = new CaughtCreature
					{
						FirstCaughtAtUnixSeconds = entry.CollectedAtUnixSeconds,
						LastCaughtAtUnixSeconds = entry.LastCaughtAtUnixSeconds > 0 ? entry.LastCaughtAtUnixSeconds : entry.CollectedAtUnixSeconds,
						// Saves from before floats: treat the old catch as the plainest float (no bonus).
						BestFloat = Mathf.Clamp(entry.BestFloat ?? 0.999f, 0f, 0.99999f),
						CatchCount = Mathf.Max(1, entry.CatchCount)
					};
				}

				progress.FirstScanUnixSeconds = packSave.FirstScanUnixSeconds;
				bool isComplete = !known || CollectionRules.IsEveryCharacterCollected(pack, progress.Caught);
				progress.CompletedUnixSeconds = isComplete ? packSave.CompletedUnixSeconds : null;
			}

			collectionCmp.IsDirty = true;

			ref var scoreCmp = ref _world.GetSinglCmp<ScoreSinglCmp>();
			scoreCmp.CurrentScore = CollectionRules.CalculateTotalPoints(_library, collectionCmp.Packs);
		}
	}
}
