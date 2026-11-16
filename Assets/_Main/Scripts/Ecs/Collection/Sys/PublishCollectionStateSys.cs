using System;
using System.Collections.Generic;
using _Main.Scripts._ProjectAgnostic.Ecs.BetterArchApi;
using _Main.Scripts._ProjectAgnostic.Ecs.UnityIntegration.Systems;
using _Main.Scripts._ProjectAgnostic.Services.EventBus;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.Scoring.Cmp;
using _Main.Scripts.Services.Events;
using _Main.Scripts.Services.Packs;
using Arch.Core;

namespace _Main.Scripts.Ecs.Collection.Sys
{
	/// <summary>
	/// Publishes an <see cref="OnCollectionStateChangedEvent"/> snapshot of every collection whenever any is dirty.
	/// Registered after <c>UpdateScoreSys</c> so the snapshot carries the already-updated total score.
	/// </summary>
	public sealed class PublishCollectionStateSys : IEcsTickSystem
	{
		private readonly World _world;
		private readonly IPackLibrary _library;
		private readonly IGameplayEventBusService _eventBus;
		private int _publishedRevision;

		public PublishCollectionStateSys(World world, IPackLibrary library, IGameplayEventBusService eventBus)
		{
			_world = world;
			_library = library;
			_eventBus = eventBus;
		}

		public void OnTick()
		{
			ref var collection = ref _world.GetSinglCmp<CollectionSinglCmp>();
			// A collection downloaded mid-session also republishes, in case the player has progress in it.
			if (!collection.IsDirty && _publishedRevision == _library.Revision)
				return;

			collection.IsDirty = false;
			_publishedRevision = _library.Revision;

			var snapshots = new Dictionary<string, CollectionSnapshot>(StringComparer.OrdinalIgnoreCase);
			foreach (var pair in collection.Packs)
			{
				if (!_library.TryGetPack(pair.Key, out var pack))
					continue;

				var progress = pair.Value;
				snapshots[pack.Id] = new CollectionSnapshot(
					pack.Id,
					progress.Caught,
					progress.FirstScanUnixSeconds,
					progress.CompletedUnixSeconds,
					CollectionRules.CalculatePoints(pack, progress.Caught, progress.CompletedUnixSeconds.HasValue),
					pack.Characters.Count);
			}

			ref var score = ref _world.GetSinglCmp<ScoreSinglCmp>();
			_eventBus.Publish(new OnCollectionStateChangedEvent(new AlbumSnapshot(snapshots, score.CurrentScore)));
		}
	}
}
