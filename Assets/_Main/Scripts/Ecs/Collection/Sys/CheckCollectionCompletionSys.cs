using System.Collections.Generic;
using _Main.Scripts._ProjectAgnostic.Ecs.BetterArchApi;
using _Main.Scripts._ProjectAgnostic.Ecs.UnityIntegration.Systems;
using _Main.Scripts._ProjectAgnostic.Providers.Time;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts._ProjectAgnostic.Utils;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.Scoring.Cmp;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Services.Packs;
using Arch.Core;

namespace _Main.Scripts.Ecs.Collection.Sys
{
	/// <summary>
	/// Consumes <see cref="OnCharacterCollectedEvt"/>; when the last character of a pack is in, records that pack's
	/// completion time and awards its completion bonus. Completion is plain state: the session keeps running (and stays
	/// saved). Must run before <c>UpdateScoreSys</c> so the bonus lands in the same tick.
	/// </summary>
	public sealed class CheckCollectionCompletionSys : IEcsTickSystem
	{
		private readonly World _world;
		private readonly IPackLibrary _library;
		private readonly IServerTimeProvider _timeProvider;
		private readonly IGameplayEventBusService _eventBus;
		private readonly Query _collectedEvtQuery;
		private readonly HashSet<string> _touchedPacks = new(System.StringComparer.OrdinalIgnoreCase);

		public CheckCollectionCompletionSys(
			World world,
			IPackLibrary library,
			IServerTimeProvider timeProvider,
			IGameplayEventBusService eventBus)
		{
			_world = world;
			_library = library;
			_timeProvider = timeProvider;
			_eventBus = eventBus;
			_collectedEvtQuery = _world.Query(new QueryDescription().WithAll<OnCharacterCollectedEvt>());
		}

		public void OnTick()
		{
			if (_collectedEvtQuery.IsEmpty())
				return;

			_touchedPacks.Clear();
			foreach (var chunk in _collectedEvtQuery)
			{
				foreach (int i in chunk)
					_touchedPacks.Add(_world.Get<OnCharacterCollectedEvt>(chunk.Entity(i)).PackId);
			}

			_world.RemoveAllComponentsInQuery<OnCharacterCollectedEvt>(_collectedEvtQuery);

			ref var collection = ref _world.GetSinglCmp<CollectionSinglCmp>();
			foreach (var packId in _touchedPacks)
			{
				if (!_library.TryGetPack(packId, out var pack) || !collection.Packs.TryGetValue(packId, out var progress))
					continue;

				if (progress.CompletedUnixSeconds.HasValue || !CollectionRules.IsEveryCharacterCollected(pack, progress.Caught))
					continue;

				long now = _timeProvider.GetCurrentTime().ToUnixTimeSeconds();
				progress.CompletedUnixSeconds = now;
				collection.IsDirty = true;

				int bonus = pack.CompletionBonusPoints;
				if (bonus > 0)
					_world.Create(new UpdateScoreReq { Kind = ScoreUpdateRequestKind.Gain, Points = bonus });

				long duration = now - (progress.FirstScanUnixSeconds ?? now);
				_eventBus.Publish(new OnCollectionCompletedEvent(pack.Id, bonus, duration));
			}
		}
	}
}
