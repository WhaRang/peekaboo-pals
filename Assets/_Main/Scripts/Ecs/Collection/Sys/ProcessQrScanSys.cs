using _Main.Scripts._ProjectAgnostic.Ecs.BetterArchApi;
using _Main.Scripts._ProjectAgnostic.Ecs.UnityIntegration.Systems;
using _Main.Scripts._ProjectAgnostic.Providers.Time;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts._ProjectAgnostic.Utils;
using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.GameSaving.Cmp;
using _Main.Scripts.Ecs.Scoring.Cmp;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Services.Floats;
using _Main.Scripts.Services.Packs;
using _Main.Scripts.Services.Qr;
using Arch.Core;
using UnityEngine;

namespace _Main.Scripts.Ecs.Collection.Sys
{
	/// <summary>
	/// The single writer of caught characters: consumes <see cref="ScanQrCodeReq"/>, resolves the payload to a pack
	/// and character and classifies the scan (new / resting / float upgrade / no improvement / invalid). Each catch
	/// rolls a float; a first catch awards tier points + float bonus through <see cref="UpdateScoreReq"/> and raises
	/// <see cref="OnCharacterCollectedEvt"/> for the completion check. Every change requests a save.
	/// </summary>
	public sealed class ProcessQrScanSys : IEcsTickSystem
	{
		private readonly World _world;
		private readonly IQrCodeResolver _resolver;
		private readonly IServerTimeProvider _timeProvider;
		private readonly IGameplayEventBusService _eventBus;
		private readonly IFloatRoller _floatRoller;
		private readonly Query _scanReqQuery;

		public ProcessQrScanSys(
			World world,
			IQrCodeResolver resolver,
			IServerTimeProvider timeProvider,
			IGameplayEventBusService eventBus,
			IFloatRoller floatRoller)
		{
			_world = world;
			_resolver = resolver;
			_timeProvider = timeProvider;
			_eventBus = eventBus;
			_floatRoller = floatRoller;
			_scanReqQuery = _world.Query(new QueryDescription().WithAll<ScanQrCodeReq>());
		}

		public void OnTick()
		{
			if (_scanReqQuery.IsEmpty())
				return;

			ref var collection = ref _world.GetSinglCmp<CollectionSinglCmp>();
			using var commandBuffer = new SCommandBuffer();

			foreach (var chunk in _scanReqQuery)
			{
				foreach (int i in chunk)
				{
					ref var entity = ref chunk.Entity(i);
					var request = _world.Get<ScanQrCodeReq>(entity);

					ProcessScan(ref collection, request.RawPayload);
					commandBuffer.Remove<ScanQrCodeReq>(entity);
				}
			}

			commandBuffer.Playback(_world);
		}

		private void ProcessScan(ref CollectionSinglCmp collection, string rawPayload)
		{
			var resolution = _resolver.Resolve(rawPayload);

			if (resolution.IsT1)
			{
				Debug.Log($"[{nameof(ProcessQrScanSys)}] Rejected QR payload ({resolution.AsT1}): '{rawPayload}'");
				_eventBus.Publish(new OnQrScanResolvedEvent(QrScanOutcome.Invalid, string.Empty, string.Empty, rejectionReason: resolution.AsT1));
				return;
			}

			var pack = resolution.AsT0.Pack;
			var character = resolution.AsT0.Character;
			var progress = collection.GetOrCreate(pack.Id);
			long now = _timeProvider.GetCurrentTime().ToUnixTimeSeconds();

			if (progress.Caught.TryGetValue(character.Id, out var caught))
			{
				ProcessRecatch(ref collection, pack, progress, character, caught, now);
				return;
			}

			float value = _floatRoller.Roll();
			progress.Caught[character.Id] = new CaughtCreature
			{
				FirstCaughtAtUnixSeconds = now,
				LastCaughtAtUnixSeconds = now,
				BestFloat = value,
				CatchCount = 1
			};
			progress.FirstScanUnixSeconds ??= now;
			collection.IsDirty = true;

			int points = CollectionRules.CharacterPoints(pack, character, value);
			_world.Create(new UpdateScoreReq { Kind = ScoreUpdateRequestKind.Gain, Points = points });
			_world.Create(new OnCharacterCollectedEvt { PackId = pack.Id, CharacterId = character.Id });
			_world.Create(new OnGameSavingRequestedEvt());

			bool completes = CollectionRules.IsEveryCharacterCollected(pack, progress.Caught);
			_eventBus.Publish(new OnQrScanResolvedEvent(QrScanOutcome.NewCharacter, pack.Id, character.Id, points, value, 1f, completesCollection: completes));
		}

		/// <summary>
		/// A character already caught can be caught again once its cooldown passed. Every re-catch rolls a new float;
		/// only a rarer one replaces the best and pays the difference in float bonus, so the score can't be farmed.
		/// </summary>
		private void ProcessRecatch(ref CollectionSinglCmp collection, CollectionPack pack, PackProgress progress, CharacterDefinition character,
			CaughtCreature caught, long now)
		{
			long elapsed = now - caught.LastCaughtAtUnixSeconds;
			int cooldown = pack.RecatchCooldownSeconds;
			if (elapsed < cooldown)
			{
				_eventBus.Publish(new OnQrScanResolvedEvent(QrScanOutcome.OnCooldown, pack.Id, character.Id, value: caught.BestFloat, previousBest: caught.BestFloat,
					cooldownRemaining: (int)(cooldown - elapsed)));
				return;
			}

			float value = _floatRoller.Roll();
			float previousBest = caught.BestFloat;
			caught.LastCaughtAtUnixSeconds = now;
			caught.CatchCount++;

			int points = 0;
			bool upgraded = value < previousBest;
			if (upgraded)
			{
				points = Mathf.Max(0, CollectionRules.FloatBonus(pack, value) - CollectionRules.FloatBonus(pack, previousBest));
				caught.BestFloat = value;
				if (points > 0)
					_world.Create(new UpdateScoreReq { Kind = ScoreUpdateRequestKind.Gain, Points = points });
			}

			progress.Caught[character.Id] = caught;
			collection.IsDirty = true;
			_world.Create(new OnGameSavingRequestedEvt());

			_eventBus.Publish(new OnQrScanResolvedEvent(upgraded ? QrScanOutcome.FloatUpgrade : QrScanOutcome.NoImprovement,
				pack.Id, character.Id, points, value, previousBest));
		}
	}
}
